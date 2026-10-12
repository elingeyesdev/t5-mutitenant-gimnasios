using System.Text.Json;
using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record SerieDto(
    int Id, int NumeroSerie, int? EjercicioId, string? Ejercicio, string? GrupoMuscular, int? RutinaEjercicioId,
    int? Repeticiones, decimal? PesoKg, int? DuracionSeg, int? DistanciaM, int? DescansoSeg, int? EsfuerzoPercibido, DateTime CompletadaEn);

public record SesionDto(
    int Id, int UsuarioId, int? SucursalId, string Lugar, string Actividad, int? RutinaId, string? Rutina, int? EntrenadorId,
    string? TipoDeporte, string Estado, DateTime Inicio, DateTime? Fin, int? DuracionSeg, int? Calorias, string? Notas,
    IReadOnlyList<SerieDto> Series);

public record MetricaDto(
    int Id, DateTime RegistradaEn, decimal? PesoKg, decimal? GrasaPct, decimal? MasaMuscularKg, decimal? CinturaCm,
    decimal? PechoCm, decimal? CaderaCm, decimal? BrazoCm, decimal? MusloCm, decimal? PantorrillaCm, string? Notas);

public record RecordFuerza(int EjercicioId, string Ejercicio, string GrupoMuscular, IReadOnlyList<PuntoRecord> Historial);
public record PuntoRecord(DateOnly Fecha, decimal PesoMaxKg, int Series);
public record ContactoDto(int Id, string NombreCompleto, string Telefono, string Relacion, bool EsPrincipal);

public record PerfilEntrenamientoDto(
    string? ObjetivoPrincipal, string? NivelExperiencia, IReadOnlyList<string> TiposPreferidos,
    IReadOnlyList<string> AreasPrioritarias, int? DiasDisponibles);

public record RestriccionDto(
    string Tipo, string? Descripcion, IReadOnlyList<string> AreasAfectadas, IReadOnlyList<string> MovimientosEvitar,
    bool RequiereAprobacion, bool Activa);

public class SerieForm
{
    public int NumeroSerie { get; set; }
    public int? EjercicioId { get; set; }
    public int? RutinaEjercicioId { get; set; }
    public int? Repeticiones { get; set; }
    public decimal? PesoKg { get; set; }
    public int? DuracionSeg { get; set; }
    public int? DistanciaM { get; set; }
    public int? DescansoSeg { get; set; }
    public int? EsfuerzoPercibido { get; set; }
}

public class SesionForm
{
    public int? RutinaId { get; set; }
    public int? SucursalId { get; set; }
    public string? TipoDeporte { get; set; }
    public int? DuracionSeg { get; set; }
    public int? Calorias { get; set; }
    public string? Notas { get; set; }
    public string? Estado { get; set; }
    public List<SerieForm> Series { get; set; } = [];
}

public class MetricaForm
{
    public int? UsuarioId { get; set; }
    public decimal? PesoKg { get; set; }
    public decimal? AlturaCm { get; set; }
    public int? Edad { get; set; }
    public decimal? GrasaPct { get; set; }
    public decimal? MasaMuscularKg { get; set; }
    public decimal? CinturaCm { get; set; }
    public decimal? PechoCm { get; set; }
    public decimal? CaderaCm { get; set; }
    public decimal? BrazoCm { get; set; }
    public decimal? MusloCm { get; set; }
    public decimal? PantorrillaCm { get; set; }
    public string? Notas { get; set; }
}

/// <summary>
/// Entrenamiento y seguimiento (CU-13, CU-14, CU-15, CU-07): perfil de entrenamiento,
/// restricciones, contactos de emergencia, sesiones con sus series, récords y métricas.
/// Estos datos pertenecen a la persona, no a la cadena.
/// </summary>
public class EntrenamientoService(AppDbContext db, UsuarioActual actual, DatosPersonales datos)
{
    // ── Perfil de entrenamiento, restricciones y contactos ───────────────

    public async Task<PerfilEntrenamientoDto?> PerfilAsync(int usuarioId)
    {
        await datos.ExigirAsync(usuarioId);
        var p = await db.PerfilesEntrenamiento.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);
        return p is null ? null : new PerfilEntrenamientoDto(p.ObjetivoPrincipal, p.NivelExperiencia, p.TiposPreferidos, p.AreasPrioritarias, p.DiasDisponibles);
    }

    public async Task<PerfilEntrenamientoDto> GuardarPerfilAsync(PerfilEntrenamientoDto f)
    {
        var p = await db.PerfilesEntrenamiento.FirstOrDefaultAsync(x => x.UsuarioId == actual.Id);
        if (p is null) db.PerfilesEntrenamiento.Add(p = new PerfilEntrenamiento { UsuarioId = actual.Id });
        p.ObjetivoPrincipal = f.ObjetivoPrincipal;
        p.NivelExperiencia = f.NivelExperiencia;
        p.TiposPreferidos = [.. f.TiposPreferidos];
        p.AreasPrioritarias = [.. f.AreasPrioritarias];
        p.DiasDisponibles = f.DiasDisponibles;
        await db.SaveChangesAsync();
        return f;
    }

    public async Task<RestriccionDto?> RestriccionAsync(int usuarioId)
    {
        await datos.ExigirAsync(usuarioId);
        var r = await db.Restricciones.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);
        return r is null ? null : new RestriccionDto(r.Tipo, r.Descripcion, r.AreasAfectadas, r.MovimientosEvitar, r.RequiereAprobacion, r.Activa);
    }

    public async Task<RestriccionDto> GuardarRestriccionAsync(RestriccionDto f)
    {
        if (string.IsNullOrWhiteSpace(f.Tipo)) throw ErrorNegocio.Solicitud("Indica el tipo de restricción.");
        var r = await db.Restricciones.FirstOrDefaultAsync(x => x.UsuarioId == actual.Id);
        if (r is null) db.Restricciones.Add(r = new RestriccionEntrenamiento { UsuarioId = actual.Id });
        r.Tipo = f.Tipo;
        r.Descripcion = f.Descripcion;
        r.AreasAfectadas = [.. f.AreasAfectadas];
        r.MovimientosEvitar = [.. f.MovimientosEvitar];
        r.RequiereAprobacion = f.RequiereAprobacion;
        r.Activa = f.Activa;
        r.ActualizadaEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return f;
    }

    public async Task<List<ContactoDto>> ContactosAsync(int usuarioId)
    {
        await datos.ExigirAsync(usuarioId);
        return await db.ContactosEmergencia.Where(c => c.UsuarioId == usuarioId).OrderByDescending(c => c.EsPrincipal)
            .Select(c => new ContactoDto(c.Id, c.NombreCompleto, c.Telefono, c.Relacion, c.EsPrincipal)).ToListAsync();
    }

    public async Task<ContactoDto> AgregarContactoAsync(ContactoDto f)
    {
        if (string.IsNullOrWhiteSpace(f.NombreCompleto) || string.IsNullOrWhiteSpace(f.Telefono))
            throw ErrorNegocio.Solicitud("Nombre y teléfono del contacto son obligatorios.");
        if (f.EsPrincipal)
            await db.ContactosEmergencia.Where(c => c.UsuarioId == actual.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.EsPrincipal, false));
        var c = new ContactoEmergencia { UsuarioId = actual.Id, NombreCompleto = f.NombreCompleto.Trim(), Telefono = f.Telefono.Trim(), Relacion = f.Relacion, EsPrincipal = f.EsPrincipal };
        db.ContactosEmergencia.Add(c);
        await db.SaveChangesAsync();
        return new ContactoDto(c.Id, c.NombreCompleto, c.Telefono, c.Relacion, c.EsPrincipal);
    }

    public async Task EliminarContactoAsync(int id)
    {
        var c = await db.ContactosEmergencia.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado("Contacto no encontrado.");
        if (c.UsuarioId != actual.Id && actual.Nivel < Niveles.Recepcionista) throw ErrorNegocio.Prohibido("No puedes eliminar contactos de otro usuario.");
        if (c.UsuarioId != actual.Id) await datos.ExigirAsync(c.UsuarioId);
        db.ContactosEmergencia.Remove(c);
        await db.SaveChangesAsync();
    }

    // ── Sesiones y series (CU-13) ────────────────────────────────────────

    /// <summary>Guarda una sesión terminada con sus series (saveCompletedSession del proyecto base).</summary>
    public async Task<SesionDto> GuardarSesionAsync(SesionForm f)
    {
        if (f.Series.Count == 0 && (f.DuracionSeg ?? 0) < 60)
            throw ErrorNegocio.Solicitud("No se puede guardar una sesión de menos de 1 minuto sin series registradas.");
        if (f.RutinaId is int rutinaId && !await db.Rutinas.AnyAsync(r => r.Id == rutinaId && (r.AsignadaAId == actual.Id || r.EsPlantilla || r.EntrenadorId == actual.Id)))
            throw ErrorNegocio.Prohibido("La rutina indicada no está asignada a tu cuenta.");

        var ahora = DateTime.UtcNow;
        var sesion = new SesionEntrenamiento
        {
            UsuarioId = actual.Id,
            SucursalId = await SucursalDeSesionAsync(f.SucursalId),
            RutinaId = f.RutinaId,
            TipoDeporte = f.TipoDeporte,
            DuracionSeg = f.DuracionSeg,
            Calorias = f.Calorias,
            Notas = f.Notas,
            Estado = string.IsNullOrEmpty(f.Estado) ? EstadosSesion.Completada : f.Estado,
            Inicio = f.DuracionSeg is > 0 ? ahora.AddSeconds(-f.DuracionSeg.Value) : ahora,
        };
        if (sesion.Estado == EstadosSesion.Completada) sesion.Fin = ahora;
        sesion.Series = await SeriesValidasAsync(f.Series);
        db.Sesiones.Add(sesion);
        await db.SaveChangesAsync();
        return await SesionAsync(sesion.Id);
    }

    public async Task<SesionDto> ActualizarSesionAsync(int id, SesionForm f)
    {
        var s = await db.Sesiones.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado($"Sesión {id} no encontrada");
        if (s.UsuarioId != actual.Id) throw ErrorNegocio.Prohibido("No tiene permisos para modificar esta sesión");
        if (!string.IsNullOrEmpty(f.Estado)) s.Estado = f.Estado;
        if (s.Estado == EstadosSesion.Completada && s.Fin is null) s.Fin = DateTime.UtcNow;
        s.DuracionSeg = f.DuracionSeg ?? s.DuracionSeg;
        s.Calorias = f.Calorias ?? s.Calorias;
        s.Notas = f.Notas ?? s.Notas;
        await db.SaveChangesAsync();
        return await SesionAsync(id);
    }

    public async Task<SerieDto> AgregarSerieAsync(int sesionId, SerieForm f)
    {
        var s = await db.Sesiones.FindAsync(sesionId) ?? throw ErrorNegocio.NoEncontrado($"Sesión {sesionId} no encontrada");
        if (s.UsuarioId != actual.Id) throw ErrorNegocio.Prohibido("No tiene permisos para modificar esta sesión");
        var serie = (await SeriesValidasAsync([f]))[0];
        serie.SesionId = sesionId;
        db.Series.Add(serie);
        await db.SaveChangesAsync();
        await db.Entry(serie).Reference(x => x.Ejercicio).LoadAsync();
        return ASerie(serie);
    }

    public async Task EliminarSesionAsync(int id)
    {
        var s = await db.Sesiones.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado($"Sesión {id} no encontrada");
        if (s.UsuarioId != actual.Id && !actual.EsSuperAdmin) throw ErrorNegocio.Prohibido("No tiene permisos para eliminar esta sesión");
        db.Sesiones.Remove(s);
        await db.SaveChangesAsync();
    }

    public async Task<SesionDto> SesionAsync(int id)
    {
        var s = await ConsultaSesiones().FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado($"Sesión {id} no encontrada");
        await datos.ExigirAsync(s.UsuarioId);
        return await ASesionAsync(s);
    }

    public async Task<List<SesionDto>> SesionesAsync(int usuarioId, int? rutinaId = null, int limite = 50, int desde = 0)
    {
        await datos.ExigirAsync(usuarioId);
        var q = ConsultaSesiones().Where(s => s.UsuarioId == usuarioId);
        if (rutinaId is not null) q = q.Where(s => s.RutinaId == rutinaId);
        var lista = await q.OrderByDescending(s => s.Inicio).Skip(desde).Take(Math.Clamp(limite, 1, 200)).ToListAsync();
        var resultado = new List<SesionDto>();
        foreach (var s in lista) resultado.Add(await ASesionAsync(s));
        return resultado;
    }

    /// <summary>Peso máximo por ejercicio y fecha (getStrengthRecords).</summary>
    public async Task<List<RecordFuerza>> RecordsAsync(int usuarioId)
    {
        await datos.ExigirAsync(usuarioId);
        var series = await db.Series.Where(x => x.Sesion!.UsuarioId == usuarioId && x.PesoKg != null && x.EjercicioId != null)
            .Select(x => new { x.EjercicioId, Ejercicio = x.Ejercicio!.Nombre, Grupo = x.Ejercicio.GrupoMuscular, x.PesoKg, x.Sesion!.Inicio })
            .ToListAsync();
        return series.GroupBy(x => new { x.EjercicioId, x.Ejercicio, x.Grupo })
            .Select(g => new RecordFuerza(g.Key.EjercicioId!.Value, g.Key.Ejercicio, g.Key.Grupo,
                g.GroupBy(x => DateOnly.FromDateTime(Reloj.ALocal(x.Inicio))).OrderBy(d => d.Key)
                    .Select(d => new PuntoRecord(d.Key, d.Max(x => x.PesoKg!.Value), d.Count())).ToList()))
            .OrderBy(r => r.Ejercicio).ToList();
    }

    // ── Métricas físicas (CU-14) ─────────────────────────────────────────

    /// <summary>Registra medidas de la persona (propias, o de un alumno si es su asesor o personal autorizado).</summary>
    public async Task<MetricaDto> RegistrarMetricaAsync(MetricaForm f)
    {
        var usuarioId = f.UsuarioId ?? actual.Id;
        if (usuarioId != actual.Id) await datos.ExigirAsync(usuarioId);
        if (usuarioId == actual.Id && actual.EsGerente) throw ErrorNegocio.Solicitud("Los gerentes no pueden registrar métricas físicas.");

        var m = new MetricaFisica
        {
            UsuarioId = usuarioId, SucursalId = actual.SucursalId, PesoKg = f.PesoKg, GrasaPct = f.GrasaPct, MasaMuscularKg = f.MasaMuscularKg,
            CinturaCm = f.CinturaCm, PechoCm = f.PechoCm, CaderaCm = f.CaderaCm, BrazoCm = f.BrazoCm, MusloCm = f.MusloCm,
            PantorrillaCm = f.PantorrillaCm, Notas = f.Notas,
        };
        db.Metricas.Add(m);
        if (f.AlturaCm is not null || f.Edad is > 0)
        {
            var p = await db.Perfiles.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);
            if (p is not null)
            {
                if (f.AlturaCm is not null) p.AlturaCm = f.AlturaCm;
                if (f.Edad is > 0) p.FechaNacimiento = new DateOnly(DateTime.Today.Year - f.Edad.Value, 1, 1);
            }
        }
        await db.SaveChangesAsync();
        return AMetrica(m);
    }

    /// <summary>Actualiza las circunferencias del último registro (saveMyCircumferences).</summary>
    public async Task<MetricaDto> GuardarCircunferenciasAsync(MetricaForm f)
    {
        var m = await db.Metricas.Where(x => x.UsuarioId == actual.Id).OrderByDescending(x => x.RegistradaEn).FirstOrDefaultAsync();
        if (m is null) db.Metricas.Add(m = new MetricaFisica { UsuarioId = actual.Id, SucursalId = actual.SucursalId });
        m.CinturaCm = f.CinturaCm ?? m.CinturaCm;
        m.CaderaCm = f.CaderaCm ?? m.CaderaCm;
        m.PechoCm = f.PechoCm ?? m.PechoCm;
        m.BrazoCm = f.BrazoCm ?? m.BrazoCm;
        m.MusloCm = f.MusloCm ?? m.MusloCm;
        m.PantorrillaCm = f.PantorrillaCm ?? m.PantorrillaCm;
        await db.SaveChangesAsync();
        return AMetrica(m);
    }

    public async Task<List<MetricaDto>> MetricasAsync(int usuarioId)
    {
        await datos.ExigirAsync(usuarioId);
        return (await db.Metricas.Where(m => m.UsuarioId == usuarioId).OrderByDescending(m => m.RegistradaEn).ToListAsync()).Select(AMetrica).ToList();
    }

    public async Task<MetricaDto?> UltimaMetricaAsync(int usuarioId)
    {
        await datos.ExigirAsync(usuarioId);
        var m = await db.Metricas.Where(x => x.UsuarioId == usuarioId).OrderByDescending(x => x.RegistradaEn).FirstOrDefaultAsync();
        return m is null ? null : AMetrica(m);
    }

    public async Task EliminarMetricaAsync(int id)
    {
        var m = await db.Metricas.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado("Métrica no encontrada.");
        if (m.UsuarioId != actual.Id && !actual.EsSuperAdmin) throw ErrorNegocio.Prohibido("Solo puedes eliminar tus propias métricas.");
        db.Metricas.Remove(m);
        await db.SaveChangesAsync();
    }

    // ── Apoyo ────────────────────────────────────────────────────────────

    private async Task<int?> SucursalDeSesionAsync(int? solicitada)
    {
        if (actual.SucursalFiltro is int propia && !actual.EsCliente) return propia;
        if (solicitada is not null && await db.Sucursales.IgnoreQueryFilters().AnyAsync(s => s.Id == solicitada)) return solicitada;
        return await db.Suscripciones.Where(s => s.UsuarioId == actual.Id && s.Estado == EstadosSuscripcion.Activa)
            .OrderByDescending(s => s.CreadaEn).Select(s => s.SucursalPrincipalId).FirstOrDefaultAsync();
    }

    private async Task<List<SerieEntrenamiento>> SeriesValidasAsync(IEnumerable<SerieForm> series)
    {
        var lista = series.ToList();
        var ids = lista.Where(s => s.EjercicioId is > 0).Select(s => s.EjercicioId!.Value).Distinct().ToList();
        var validos = (await db.Ejercicios.Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToListAsync()).ToHashSet();
        var rutinaEjercicios = lista.Where(s => s.RutinaEjercicioId is > 0).Select(s => s.RutinaEjercicioId!.Value).Distinct().ToList();
        var mapa = await db.RutinaEjercicios.Where(r => rutinaEjercicios.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.EjercicioId);
        return lista.Select(s => new SerieEntrenamiento
        {
            NumeroSerie = s.NumeroSerie,
            RutinaEjercicioId = s.RutinaEjercicioId is int re && mapa.ContainsKey(re) ? re : null,
            EjercicioId = s.EjercicioId is int e && validos.Contains(e) ? e : s.RutinaEjercicioId is int r && mapa.TryGetValue(r, out var desdeRutina) ? desdeRutina : null,
            Repeticiones = s.Repeticiones, PesoKg = s.PesoKg, DuracionSeg = s.DuracionSeg, DistanciaM = s.DistanciaM,
            DescansoSeg = s.DescansoSeg, EsfuerzoPercibido = s.EsfuerzoPercibido,
        }).ToList();
    }

    private IQueryable<SesionEntrenamiento> ConsultaSesiones() =>
        db.Sesiones.Include(s => s.Rutina).Include(s => s.Series).ThenInclude(x => x.Ejercicio).AsSplitQuery();

    private async Task<SesionDto> ASesionAsync(SesionEntrenamiento s)
    {
        string lugar = "Sede no registrada";
        if (s.SucursalId is int sid)
        {
            var suc = await db.Sucursales.IgnoreQueryFilters().Where(x => x.Id == sid).Select(x => new { x.Nombre, Cadena = x.Cadena!.Nombre }).FirstOrDefaultAsync();
            if (suc is not null) lugar = suc.Nombre.StartsWith(suc.Cadena) ? suc.Nombre : $"{suc.Cadena} - {suc.Nombre}";
        }
        var series = s.Series.OrderBy(x => x.CompletadaEn).ThenBy(x => x.NumeroSerie).Select(ASerie).ToList();
        var actividad = series.FirstOrDefault()?.Ejercicio ?? s.Rutina?.Nombre ?? s.TipoDeporte ?? "Actividad desconocida";
        return new SesionDto(s.Id, s.UsuarioId, s.SucursalId, lugar, actividad, s.RutinaId, s.Rutina?.Nombre, s.Rutina?.EntrenadorId,
            s.TipoDeporte, s.Estado, s.Inicio, s.Fin, s.DuracionSeg, s.Calorias, s.Notas, series);
    }

    private static SerieDto ASerie(SerieEntrenamiento x) => new(
        x.Id, x.NumeroSerie, x.EjercicioId, x.Ejercicio?.Nombre, x.Ejercicio?.GrupoMuscular, x.RutinaEjercicioId,
        x.Repeticiones, x.PesoKg, x.DuracionSeg, x.DistanciaM, x.DescansoSeg, x.EsfuerzoPercibido, x.CompletadaEn);

    public static MetricaDto AMetrica(MetricaFisica m) => new(
        m.Id, m.RegistradaEn, m.PesoKg, m.GrasaPct, m.MasaMuscularKg, m.CinturaCm, m.PechoCm, m.CaderaCm, m.BrazoCm, m.MusloCm, m.PantorrillaCm, m.Notas);

    /// <summary>IMC y su categoría, como healthMetrics de la app base.</summary>
    public static (decimal? Imc, string Categoria) Imc(decimal? pesoKg, decimal? alturaCm)
    {
        if (pesoKg is null or <= 0 || alturaCm is null or <= 0) return (null, "-");
        var m = alturaCm.Value / 100;
        var imc = Math.Round(pesoKg.Value / (m * m), 1);
        var categoria = imc < 18.5m ? "Bajo peso" : imc <= 24.9m ? "Peso normal" : imc <= 29.9m ? "Sobrepeso" : "Obesidad";
        return (imc, categoria);
    }

    internal static string Json(object o) => JsonSerializer.Serialize(o);
}
