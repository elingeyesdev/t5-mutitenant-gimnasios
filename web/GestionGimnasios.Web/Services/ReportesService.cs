using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record Indicador(string Etiqueta, string Valor, string? Detalle = null);
public record Punto(string Etiqueta, double Valor);
public record Serie(string Titulo, IReadOnlyList<Punto> Puntos);
public record Tabla(string Titulo, IReadOnlyList<string> Columnas, IReadOnlyList<IReadOnlyList<string>> Filas);
public record Reporte(string Tipo, string Titulo, string Descripcion, DateOnly Desde, DateOnly Hasta, string Alcance,
    IReadOnlyList<Indicador> Indicadores, IReadOnlyList<Serie> Series, IReadOnlyList<Tabla> Tablas, IReadOnlyList<string> Observaciones);

public record TipoReporte(string Tipo, string Titulo, string Descripcion, string Grupo);

public class FiltroReporte
{
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public int? SucursalId { get; set; }
    public int TopN { get; set; } = 20;
}

public record ResumenPanel(int UsuariosActivos, int IngresosHoy, int ReservasHoy, int SociosConSuscripcion, int PersonasDentro,
    Serie Usuarios, Serie Ingresos, Serie Reservas);

/// <summary>
/// Reportes del panel (CU-32) y panel de resumen (CU-31). En el proyecto base se calculaban en
/// el navegador a partir de datos crudos; aquí se calculan en el servidor con el alcance del usuario.
/// </summary>
public class ReportesService(AppDbContext db, UsuarioActual actual, Alcances alcances, SedesService sedes)
{
    public static readonly TipoReporte[] Tipos =
    [
        new("asistencia", "Asistencia por sucursal", "Ingresos por día y hora, días y horas pico, y comparación con el período anterior.", "Operación"),
        new("aforo", "Ocupación y aforo", "Aforo actual frente al máximo por sucursal y capacidad de máquinas instaladas.", "Operación"),
        new("auditoria", "Auditoría de accesos", "Registro de ingresos y salidas por fecha, persona y sucursal.", "Operación"),
        new("actividades", "Reservas por actividad", "Reservas confirmadas, asistidas y canceladas por actividad.", "Operación"),
        new("cancelaciones", "Cancelaciones y no asistencias", "Tasa de cancelación por actividad y día de la semana.", "Operación"),
        new("maquinas", "Inventario de máquinas", "Estado de los equipos por categoría y sucursal.", "Operación"),
        new("frecuencia", "Frecuencia de entrenamiento", "Ingresos por socio: clasificación por frecuencia y socios más activos.", "Gestión"),
        new("ranking", "Ranking de sedes", "Puntaje por sucursal: 40 % ingresos, 30 % reservas, 20 % miembros y 10 % ocupación.", "Gestión"),
        new("churn", "Retención de socios", "Socios activos, en riesgo (31 a 90 días sin ingresar) e inactivos.", "Gestión"),
        new("consolidado", "Consolidado de sucursales", "Ingresos, reservas, miembros y equipos de cada sucursal, agrupados por cadena.", "Gestión"),
    ];

    private static readonly string[] NombresDia = ["Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];
    private static readonly int[] OrdenDias = [1, 2, 3, 4, 5, 6, 0];

    public async Task<Reporte> GenerarAsync(string tipo, FiltroReporte f)
    {
        var info = Tipos.FirstOrDefault(t => t.Tipo == tipo) ?? throw ErrorNegocio.NoEncontrado("Reporte no encontrado.");
        var hasta = f.Hasta ?? Reloj.Hoy;
        var desde = f.Desde ?? hasta.AddDays(-29);
        if (desde > hasta) throw ErrorNegocio.Solicitud("La fecha inicial no puede ser posterior a la final.");
        if (f.SucursalId is int sid) await alcances.ExigirSucursalAsync(sid);

        var sucursales = await alcances.SucursalesGestionables().Include(s => s.Cadena)
            .Where(s => f.SucursalId == null || s.Id == f.SucursalId).OrderBy(s => s.Cadena!.Nombre).ThenBy(s => s.Nombre).ToListAsync();
        var ids = sucursales.Select(s => s.Id).ToList();
        var alcance = f.SucursalId is not null ? sucursales.FirstOrDefault()?.Nombre ?? "" : actual.EsSuperAdmin ? "Todas las cadenas" : sucursales.FirstOrDefault()?.Cadena?.Nombre ?? "";
        var ctx = new Contexto(info, desde, hasta, alcance, sucursales, ids, f.TopN);

        return tipo switch
        {
            "asistencia" => await AsistenciaAsync(ctx),
            "aforo" => await AforoAsync(ctx),
            "auditoria" => await AuditoriaAsync(ctx),
            "actividades" => await ActividadesAsync(ctx),
            "cancelaciones" => await CancelacionesAsync(ctx),
            "maquinas" => await MaquinasAsync(ctx),
            "frecuencia" => await FrecuenciaAsync(ctx),
            "ranking" => await RankingAsync(ctx),
            "churn" => await ChurnAsync(ctx),
            _ => await ConsolidadoAsync(ctx),
        };
    }

    // ── Panel de resumen ─────────────────────────────────────────────────

    public async Task<ResumenPanel> ResumenAsync(string periodo = "week")
    {
        var ids = await alcances.SucursalesGestionables().Select(s => s.Id).ToListAsync();
        var hoy = Reloj.Hoy;
        var inicioHoy = Reloj.InicioDelDiaUtc(hoy);
        var usuarios = UsuariosDelAlcance();
        var dias = periodo == "month" ? 30 : periodo == "year" ? 365 : 7;
        var desde = hoy.AddDays(-(dias - 1));
        var desdeUtc = Reloj.InicioDelDiaUtc(desde);

        var altas = await usuarios.Where(u => u.CreadoEn >= desdeUtc).Select(u => u.CreadoEn).ToListAsync();
        var ingresos = await db.Accesos.Where(a => ids.Contains(a.SucursalId) && a.Ingreso >= desdeUtc).Select(a => a.Ingreso).ToListAsync();
        var reservas = await db.Reservas.Where(r => r.SucursalId != null && ids.Contains(r.SucursalId.Value) && r.Fecha >= desde && r.Fecha <= hoy).Select(r => r.Fecha).ToListAsync();

        return new ResumenPanel(
            await usuarios.CountAsync(u => u.Activo),
            await db.Accesos.CountAsync(a => ids.Contains(a.SucursalId) && a.Ingreso >= inicioHoy),
            await db.Reservas.CountAsync(r => r.SucursalId != null && ids.Contains(r.SucursalId.Value) && r.Fecha == hoy),
            await db.Suscripciones.CountAsync(s => s.Estado == EstadosSuscripcion.Activa && (actual.EsSuperAdmin || s.CadenaId == actual.CadenaId)
                                                   && (actual.SucursalFiltro == null || s.SucursalPrincipalId == actual.SucursalFiltro)),
            (await sedes.OcupacionAsync(ids)).Values.Sum(),
            Historial("Usuarios nuevos", altas.Select(d => DateOnly.FromDateTime(Reloj.ALocal(d))), desde, hoy, periodo),
            Historial("Ingresos", ingresos.Select(d => DateOnly.FromDateTime(Reloj.ALocal(d))), desde, hoy, periodo),
            Historial("Reservas", reservas, desde, hoy, periodo));
    }

    // ── Reportes ─────────────────────────────────────────────────────────

    private record Contexto(TipoReporte Info, DateOnly Desde, DateOnly Hasta, string Alcance, List<Sucursal> Sucursales, List<int> Ids, int TopN);

    private Reporte Crear(Contexto c, IReadOnlyList<Indicador> ind, IReadOnlyList<Serie>? series = null, IReadOnlyList<Tabla>? tablas = null, IReadOnlyList<string>? obs = null) =>
        new(c.Info.Tipo, c.Info.Titulo, c.Info.Descripcion, c.Desde, c.Hasta, c.Alcance, ind, series ?? [], tablas ?? [], obs ?? []);

    private async Task<List<(int UsuarioId, int SucursalId, DateTime Ingreso, DateTime? Salida, string Metodo)>> AccesosAsync(Contexto c, DateOnly? desde = null, DateOnly? hasta = null)
    {
        var d = Reloj.InicioDelDiaUtc(desde ?? c.Desde);
        var h = Reloj.InicioDelDiaUtc((hasta ?? c.Hasta).AddDays(1));
        return (await db.Accesos.Where(a => c.Ids.Contains(a.SucursalId) && a.Ingreso >= d && a.Ingreso < h)
                .Select(a => new { a.UsuarioId, a.SucursalId, a.Ingreso, a.Salida, a.Metodo }).ToListAsync())
            .Select(a => (a.UsuarioId, a.SucursalId, Reloj.ALocal(a.Ingreso), a.Salida is null ? (DateTime?)null : Reloj.ALocal(a.Salida.Value), a.Metodo)).ToList();
    }

    private async Task<Reporte> AsistenciaAsync(Contexto c)
    {
        var accesos = await AccesosAsync(c);
        var dias = c.Hasta.DayNumber - c.Desde.DayNumber + 1;
        var previoHasta = c.Desde.AddDays(-1);
        var previo = (await AccesosAsync(c, previoHasta.AddDays(-(dias - 1)), previoHasta)).Count;
        var porDia = OrdenDias.Select(d => new Punto(NombresDia[d], accesos.Count(a => (int)a.Ingreso.DayOfWeek == d))).ToList();
        var porHora = Enumerable.Range(5, 18).Select(h => new Punto($"{h:00}:00", accesos.Count(a => a.Ingreso.Hour == h))).ToList();
        var diaPico = porDia.MaxBy(p => p.Valor);
        var horaPico = porHora.MaxBy(p => p.Valor);
        var variacion = previo == 0 ? (accesos.Count > 0 ? "nuevo" : "0 %") : $"{Math.Round((accesos.Count - previo) * 100.0 / previo):+0;-0;0} %";
        var porSucursal = c.Sucursales.Select(s => (IReadOnlyList<string>)[s.Nombre, accesos.Count(a => a.SucursalId == s.Id).ToString(), accesos.Where(a => a.SucursalId == s.Id).Select(a => a.UsuarioId).Distinct().Count().ToString()])
            .OrderByDescending(f => int.Parse(f[1])).ToList();
        return Crear(c,
            [
                new("Ingresos", accesos.Count.ToString()),
                new("Promedio diario", Math.Round(accesos.Count / (double)dias, 1).ToString("0.#")),
                new("Día pico", diaPico is { Valor: > 0 } ? diaPico.Etiqueta : "—"),
                new("Hora pico", horaPico is { Valor: > 0 } ? horaPico.Etiqueta : "—"),
                new("Período anterior", previo.ToString(), $"Variación {variacion}"),
            ],
            [new("Ingresos por día de la semana", porDia), new("Ingresos por hora", porHora)],
            [new("Ingresos por sucursal", ["Sucursal", "Ingresos", "Personas distintas"], porSucursal)]);
    }

    private async Task<Reporte> AforoAsync(Contexto c)
    {
        var ocupacion = await sedes.OcupacionAsync(c.Ids);
        var maquinas = await db.Maquinas.Where(m => c.Ids.Contains(m.SucursalId)).GroupBy(m => m.SucursalId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var filas = c.Sucursales.Select(s =>
        {
            var o = ocupacion.GetValueOrDefault(s.Id);
            var pct = s.CapacidadMaxima > 0 ? Math.Min(100, (int)Math.Round(o * 100.0 / s.CapacidadMaxima)) : 0;
            return (s, o, pct, maq: maquinas.GetValueOrDefault(s.Id));
        }).OrderByDescending(x => x.pct).ToList();
        return Crear(c,
            [
                new("Personas dentro", filas.Sum(f => f.o).ToString()),
                new("Capacidad total", filas.Sum(f => f.s.CapacidadMaxima).ToString()),
                new("Ocupación promedio", filas.Count == 0 ? "0 %" : $"{Math.Round(filas.Average(f => f.pct))} %"),
                new("Sucursales sobre 80 %", filas.Count(f => f.pct >= 80).ToString()),
            ],
            [new("Ocupación por sucursal (%)", filas.Select(f => new Punto(f.s.Nombre, f.pct)).ToList())],
            [new("Aforo por sucursal", ["Sucursal", "Cadena", "Dentro", "Capacidad", "Ocupación", "Máquinas"],
                filas.Select(f => (IReadOnlyList<string>)[f.s.Nombre, f.s.Cadena?.Nombre ?? "", f.o.ToString(), f.s.CapacidadMaxima.ToString(), $"{f.pct} %", f.maq.ToString()]).ToList())],
            ["Ocupación = personas con ingreso activo (sin salida) respecto de la capacidad máxima."]);
    }

    private async Task<Reporte> AuditoriaAsync(Contexto c)
    {
        var d = Reloj.InicioDelDiaUtc(c.Desde);
        var h = Reloj.InicioDelDiaUtc(c.Hasta.AddDays(1));
        var filas = await db.Accesos.Where(a => c.Ids.Contains(a.SucursalId) && a.Ingreso >= d && a.Ingreso < h).OrderByDescending(a => a.Ingreso).Take(500)
            .Select(a => new { a.Ingreso, a.Salida, Nombre = a.Usuario!.Perfil!.Nombres + " " + a.Usuario.Perfil.Apellidos, Sucursal = a.Sucursal!.Nombre, a.Metodo, a.Estado }).ToListAsync();
        var total = await db.Accesos.CountAsync(a => c.Ids.Contains(a.SucursalId) && a.Ingreso >= d && a.Ingreso < h);
        return Crear(c,
            [
                new("Registros", total.ToString()),
                new("Con QR", filas.Count(f => f.Metodo == "QR").ToString()),
                new("Manuales", filas.Count(f => f.Metodo == "MANUAL").ToString()),
                new("Sin salida", filas.Count(f => f.Salida is null).ToString()),
            ],
            tablas: [new("Registros de acceso", ["Ingreso", "Salida", "Persona", "Sucursal", "Método", "Estado"],
                filas.Select(f => (IReadOnlyList<string>)[Reloj.ALocal(f.Ingreso).ToString("dd/MM/yyyy HH:mm"), f.Salida is null ? "—" : Reloj.ALocal(f.Salida.Value).ToString("HH:mm"), f.Nombre, f.Sucursal, f.Metodo, f.Estado]).ToList())],
            obs: total > 500 ? ["Se muestran los 500 registros más recientes."] : null);
    }

    private async Task<List<(string Actividad, string Estado, DateOnly Fecha)>> ReservasAsync(Contexto c) =>
        (await db.Reservas.Where(r => r.SucursalId != null && c.Ids.Contains(r.SucursalId.Value) && r.Fecha >= c.Desde && r.Fecha <= c.Hasta)
            .Select(r => new { Actividad = r.Actividad != null ? r.Actividad.Nombre : "Sin actividad", r.Estado, r.Fecha }).ToListAsync())
        .Select(r => (r.Actividad, r.Estado, r.Fecha)).ToList();

    private async Task<Reporte> ActividadesAsync(Contexto c)
    {
        var reservas = await ReservasAsync(c);
        var filas = reservas.GroupBy(r => r.Actividad).Select(g => new
        {
            Actividad = g.Key, Total = g.Count(),
            Confirmadas = g.Count(r => r.Estado == EstadosReserva.Confirmada),
            Asistidas = g.Count(r => r.Estado == EstadosReserva.Asistida),
            Canceladas = g.Count(r => r.Estado == EstadosReserva.Cancelada),
        }).OrderByDescending(x => x.Total).ToList();
        return Crear(c,
            [
                new("Reservas", reservas.Count.ToString()),
                new("Actividades", filas.Count.ToString()),
                new("Más reservada", filas.FirstOrDefault()?.Actividad ?? "—"),
                new("Asistidas", reservas.Count(r => r.Estado == EstadosReserva.Asistida).ToString()),
            ],
            [new("Reservas por actividad", filas.Select(f => new Punto(f.Actividad, f.Total)).ToList())],
            [new("Detalle por actividad", ["Actividad", "Total", "Confirmadas", "Asistidas", "Canceladas"],
                filas.Select(f => (IReadOnlyList<string>)[f.Actividad, f.Total.ToString(), f.Confirmadas.ToString(), f.Asistidas.ToString(), f.Canceladas.ToString()]).ToList())]);
    }

    private async Task<Reporte> CancelacionesAsync(Contexto c)
    {
        var reservas = await ReservasAsync(c);
        int Pct(int a, int b) => b == 0 ? 0 : (int)Math.Round(a * 100.0 / b);
        var canceladas = reservas.Count(r => r.Estado == EstadosReserva.Cancelada);
        var noAsistio = reservas.Count(r => r.Estado == EstadosReserva.NoAsistio);
        var porActividad = reservas.GroupBy(r => r.Actividad)
            .Select(g => (g.Key, Total: g.Count(), Canc: g.Count(r => r.Estado == EstadosReserva.Cancelada), No: g.Count(r => r.Estado == EstadosReserva.NoAsistio)))
            .OrderByDescending(x => Pct(x.Canc, x.Total)).ToList();
        var porDia = OrdenDias.Select(d =>
        {
            var deDia = reservas.Where(r => (int)r.Fecha.DayOfWeek == d).ToList();
            return new Punto(NombresDia[d], Pct(deDia.Count(r => r.Estado == EstadosReserva.Cancelada), deDia.Count));
        }).ToList();
        return Crear(c,
            [
                new("Reservas", reservas.Count.ToString()),
                new("Canceladas", canceladas.ToString(), $"{Pct(canceladas, reservas.Count)} % del total"),
                new("No asistió", noAsistio.ToString(), $"{Pct(noAsistio, reservas.Count)} % del total"),
                new("Mayor cancelación", porActividad.FirstOrDefault(x => x.Canc > 0).Key ?? "—"),
            ],
            [new("Tasa de cancelación por día (%)", porDia)],
            [new("Cancelaciones por actividad", ["Actividad", "Reservas", "Canceladas", "Tasa", "No asistió"],
                porActividad.Select(x => (IReadOnlyList<string>)[x.Key, x.Total.ToString(), x.Canc.ToString(), $"{Pct(x.Canc, x.Total)} %", x.No.ToString()]).ToList())]);
    }

    private async Task<Reporte> MaquinasAsync(Contexto c)
    {
        var maquinas = await db.Maquinas.Where(m => c.Ids.Contains(m.SucursalId)).Select(m => new { m.Nombre, m.Categoria, m.Estado, Sucursal = m.Sucursal!.Nombre, m.ActualizadaEn }).ToListAsync();
        var porCategoria = CategoriasMaquina.Todas.Select(cat => new Punto(Etiqueta(cat), maquinas.Count(m => m.Categoria == cat))).ToList();
        var mantenimiento = maquinas.Count(m => m.Estado == EstadosMaquina.Mantenimiento);
        return Crear(c,
            [
                new("Máquinas", maquinas.Count.ToString()),
                new("Disponibles", (maquinas.Count - mantenimiento).ToString()),
                new("En mantenimiento", mantenimiento.ToString(), maquinas.Count == 0 ? null : $"{Math.Round(mantenimiento * 100.0 / maquinas.Count)} % del total"),
            ],
            [new("Máquinas por categoría", porCategoria)],
            [
                new("Resumen por sucursal", ["Sucursal", "Total", "Disponibles", "Mantenimiento"],
                    maquinas.GroupBy(m => m.Sucursal).OrderBy(g => g.Key).Select(g => (IReadOnlyList<string>)[g.Key, g.Count().ToString(), g.Count(m => m.Estado == EstadosMaquina.Disponible).ToString(), g.Count(m => m.Estado == EstadosMaquina.Mantenimiento).ToString()]).ToList()),
                new("Equipos en mantenimiento", ["Máquina", "Categoría", "Sucursal", "Actualizada"],
                    maquinas.Where(m => m.Estado == EstadosMaquina.Mantenimiento).OrderBy(m => m.Sucursal).Select(m => (IReadOnlyList<string>)[m.Nombre, Etiqueta(m.Categoria), m.Sucursal, Reloj.ALocal(m.ActualizadaEn).ToString("dd/MM/yyyy")]).ToList()),
            ]);
    }

    private async Task<Reporte> FrecuenciaAsync(Contexto c)
    {
        var accesos = await AccesosAsync(c);
        var socios = await SociosDelAlcance(c).Select(u => new { u.Id, Nombre = u.Perfil!.Nombres + " " + u.Perfil.Apellidos }).ToListAsync();
        var conteo = accesos.GroupBy(a => a.UsuarioId).ToDictionary(g => g.Key, g => g.Count());
        var porSocio = socios.Select(s => (s.Nombre, Total: conteo.GetValueOrDefault(s.Id))).OrderByDescending(x => x.Total).ToList();
        var activos = porSocio.Count(x => x.Total > 0);
        var porDia = OrdenDias.Select(d => new Punto(NombresDia[d], accesos.Count(a => (int)a.Ingreso.DayOfWeek == d))).ToList();
        var grupos = new List<Punto>
        {
            new("Sin actividad (0)", porSocio.Count(x => x.Total == 0)),
            new("Baja (1–2)", porSocio.Count(x => x.Total is >= 1 and <= 2)),
            new("Media (3–5)", porSocio.Count(x => x.Total is >= 3 and <= 5)),
            new("Alta (6–10)", porSocio.Count(x => x.Total is >= 6 and <= 10)),
            new("Muy alta (más de 10)", porSocio.Count(x => x.Total > 10)),
        };
        return Crear(c,
            [
                new("Socios con ingresos", activos.ToString(), $"de {socios.Count} socios"),
                new("Promedio por socio", activos == 0 ? "0" : Math.Round(porSocio.Where(x => x.Total > 0).Average(x => x.Total), 1).ToString("0.#")),
                new("Socio más activo", porSocio.FirstOrDefault(x => x.Total > 0).Nombre ?? "—"),
                new("Día más concurrido", porDia.MaxBy(p => p.Valor) is { Valor: > 0 } pico ? pico.Etiqueta : "—"),
            ],
            [new("Socios por frecuencia", grupos), new("Ingresos por día de la semana", porDia)],
            [new($"Top {c.TopN} socios más activos", ["#", "Socio", "Ingresos"],
                porSocio.Where(x => x.Total > 0).Take(c.TopN).Select((x, i) => (IReadOnlyList<string>)[(i + 1).ToString(), x.Nombre, x.Total.ToString()]).ToList())]);
    }

    private async Task<Reporte> RankingAsync(Contexto c)
    {
        var accesos = await AccesosAsync(c);
        var reservas = await db.Reservas.Where(r => r.SucursalId != null && c.Ids.Contains(r.SucursalId.Value) && r.Fecha >= c.Desde && r.Fecha <= c.Hasta && r.Estado == EstadosReserva.Asistida)
            .GroupBy(r => r.SucursalId!.Value).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var miembros = await MiembrosPorSucursal(c);
        var ocupacion = await sedes.OcupacionAsync(c.Ids);
        var ing = c.Sucursales.ToDictionary(s => s.Id, s => accesos.Count(a => a.SucursalId == s.Id));
        double MaxDe(Func<int, int> f) => Math.Max(1, c.Sucursales.Select(s => f(s.Id)).DefaultIfEmpty(0).Max());
        var maxIng = MaxDe(id => ing[id]);
        var maxRes = MaxDe(id => reservas.GetValueOrDefault(id));
        var maxMie = MaxDe(id => miembros.GetValueOrDefault(id));
        var ranking = c.Sucursales.Select(s =>
        {
            var pct = s.CapacidadMaxima > 0 ? ocupacion.GetValueOrDefault(s.Id) / (double)s.CapacidadMaxima : 0;
            var puntaje = (int)Math.Round(ing[s.Id] / maxIng * 40 + reservas.GetValueOrDefault(s.Id) / maxRes * 30 + miembros.GetValueOrDefault(s.Id) / maxMie * 20 + Math.Min(1, pct) * 10);
            return (s, puntaje, pct);
        }).OrderByDescending(x => x.puntaje).ToList();
        return Crear(c,
            [
                new("Sedes evaluadas", ranking.Count.ToString()),
                new("Sede líder", ranking.FirstOrDefault().s?.Nombre ?? "—"),
                new("Puntaje promedio", ranking.Count == 0 ? "0/100" : $"{Math.Round(ranking.Average(r => r.puntaje))}/100"),
            ],
            [new("Puntaje por sede", ranking.Select(r => new Punto(r.s.Nombre, r.puntaje)).ToList())],
            [new("Ranking", ["#", "Sucursal", "Cadena", "Ingresos", "Reservas asistidas", "Miembros", "Ocupación", "Puntaje"],
                ranking.Select((r, i) => (IReadOnlyList<string>)[(i + 1).ToString(), r.s.Nombre, r.s.Cadena?.Nombre ?? "", ing[r.s.Id].ToString(), reservas.GetValueOrDefault(r.s.Id).ToString(), miembros.GetValueOrDefault(r.s.Id).ToString(), $"{Math.Round(r.pct * 100)} %", r.puntaje.ToString()]).ToList())],
            ["Puntaje = ingresos 40 % + reservas asistidas 30 % + miembros 20 % + ocupación actual 10 %, cada uno relativo a la mejor sede."]);
    }

    private async Task<Reporte> ChurnAsync(Contexto c)
    {
        var socios = await SociosDelAlcance(c).Select(u => new { u.Id, Nombre = u.Perfil!.Nombres + " " + u.Perfil.Apellidos, u.Email, u.Activo }).ToListAsync();
        var ids = socios.Select(s => s.Id).ToList();
        var ultimos = await db.Accesos.IgnoreQueryFilters().Where(a => ids.Contains(a.UsuarioId)).GroupBy(a => a.UsuarioId)
            .Select(g => new { g.Key, Ultimo = g.Max(a => a.Ingreso) }).ToDictionaryAsync(x => x.Key, x => x.Ultimo);
        var ahora = DateTime.UtcNow;
        var clasificados = socios.Select(s =>
        {
            var ultimo = ultimos.TryGetValue(s.Id, out var u) ? u : (DateTime?)null;
            var dias = ultimo is null ? (int?)null : (int)(ahora - ultimo.Value).TotalDays;
            var estado = !s.Activo ? "Inactivo" : dias is null ? "Sin historial" : dias <= 30 ? "Activo" : dias <= 90 ? "En riesgo" : "Inactivo";
            return (s.Nombre, s.Email, Ultimo: ultimo, Dias: dias, Estado: estado);
        }).ToList();
        string[] estados = ["Activo", "En riesgo", "Inactivo", "Sin historial"];
        IReadOnlyList<string> Fila((string Nombre, string Email, DateTime? Ultimo, int? Dias, string Estado) x) =>
            [x.Nombre, x.Email, x.Ultimo is null ? "—" : Reloj.ALocal(x.Ultimo.Value).ToString("dd/MM/yyyy"), x.Dias?.ToString() ?? "—"];
        return Crear(c,
            estados.Select(e => new Indicador(e == "Sin historial" ? "Sin historial" : e + "s", clasificados.Count(x => x.Estado == e).ToString())).Prepend(new Indicador("Socios", clasificados.Count.ToString())).ToList(),
            [new("Socios por estado", estados.Select(e => new Punto(e, clasificados.Count(x => x.Estado == e))).ToList())],
            [
                new("Socios en riesgo (31 a 90 días sin ingresar)", ["Socio", "Correo", "Último ingreso", "Días"], clasificados.Where(x => x.Estado == "En riesgo").OrderByDescending(x => x.Dias).Select(Fila).ToList()),
                new("Socios inactivos (más de 90 días o cuenta desactivada)", ["Socio", "Correo", "Último ingreso", "Días"], clasificados.Where(x => x.Estado == "Inactivo").OrderByDescending(x => x.Dias).Select(Fila).ToList()),
            ],
            ["Activo: último ingreso en los últimos 30 días · En riesgo: entre 31 y 90 días · Inactivo: más de 90 días o cuenta desactivada."]);
    }

    private async Task<Reporte> ConsolidadoAsync(Contexto c)
    {
        var accesos = await AccesosAsync(c);
        var reservas = await db.Reservas.Where(r => r.SucursalId != null && c.Ids.Contains(r.SucursalId.Value) && r.Fecha >= c.Desde && r.Fecha <= c.Hasta)
            .GroupBy(r => r.SucursalId!.Value).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var miembros = await MiembrosPorSucursal(c);
        var maquinas = await db.Maquinas.Where(m => c.Ids.Contains(m.SucursalId)).GroupBy(m => m.SucursalId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var filas = c.Sucursales.Select(s => (IReadOnlyList<string>)[s.Cadena?.Nombre ?? "", s.Nombre, accesos.Count(a => a.SucursalId == s.Id).ToString(), reservas.GetValueOrDefault(s.Id).ToString(), miembros.GetValueOrDefault(s.Id).ToString(), maquinas.GetValueOrDefault(s.Id).ToString()]).ToList();
        var porCadena = c.Sucursales.GroupBy(s => s.Cadena?.Nombre ?? "").Select(g => new Punto(g.Key, accesos.Count(a => g.Any(s => s.Id == a.SucursalId)))).ToList();
        return Crear(c,
            [
                new("Sucursales", c.Sucursales.Count.ToString()),
                new("Ingresos", accesos.Count.ToString()),
                new("Reservas", reservas.Values.Sum().ToString()),
                new("Miembros", miembros.Values.Sum().ToString()),
                new("Máquinas", maquinas.Values.Sum().ToString()),
            ],
            [new("Ingresos por cadena", porCadena)],
            [new("Consolidado por sucursal", ["Cadena", "Sucursal", "Ingresos", "Reservas", "Miembros", "Máquinas"], filas)]);
    }

    // ── Apoyo ────────────────────────────────────────────────────────────

    private IQueryable<Usuario> UsuariosDelAlcance()
    {
        var q = db.Usuarios.AsQueryable();
        if (actual.EsSuperAdmin) return q;
        var cadena = actual.CadenaId;
        if (actual.SucursalFiltro is int s)
            return q.Where(u => u.Roles.Any(r => r.SucursalId == s) || u.Membresias.Any(m => m.SucursalId == s));
        return q.Where(u => u.Roles.Any(r => r.CadenaId == cadena) || u.Membresias.Any(m => m.CadenaId == cadena));
    }

    private IQueryable<Usuario> SociosDelAlcance(Contexto c) =>
        db.Usuarios.Where(u => u.Roles.Any(r => r.Rol!.Nombre == Roles.Cliente) && u.Membresias.Any(m => m.SucursalId != null && c.Ids.Contains(m.SucursalId.Value)));

    private async Task<Dictionary<int, int>> MiembrosPorSucursal(Contexto c) =>
        await db.Membresias.Where(m => m.Activa && m.SucursalId != null && c.Ids.Contains(m.SucursalId.Value) && m.Usuario!.Activo)
            .GroupBy(m => m.SucursalId!.Value).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);

    private static Serie Historial(string titulo, IEnumerable<DateOnly> fechas, DateOnly desde, DateOnly hasta, string periodo)
    {
        var lista = fechas.ToList();
        if (periodo == "year")
        {
            var meses = Enumerable.Range(0, 12).Select(i => new DateOnly(hasta.Year, hasta.Month, 1).AddMonths(i - 11)).ToList();
            return new Serie(titulo, meses.Select(m => new Punto(m.ToString("MMM yy"), lista.Count(f => f.Year == m.Year && f.Month == m.Month))).ToList());
        }
        var dias = Enumerable.Range(0, hasta.DayNumber - desde.DayNumber + 1).Select(i => desde.AddDays(i));
        return new Serie(titulo, dias.Select(d => new Punto(d.ToString("dd/MM"), lista.Count(f => f == d))).ToList());
    }

    public static string Etiqueta(string categoria) => categoria switch
    {
        CategoriasMaquina.Cardio => "Cardio",
        CategoriasMaquina.TrenSuperior => "Tren superior",
        CategoriasMaquina.TrenInferior => "Tren inferior",
        CategoriasMaquina.Espalda => "Espalda",
        _ => "Multiestación",
    };
}
