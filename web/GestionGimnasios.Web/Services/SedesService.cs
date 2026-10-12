using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record UbicacionDto(string Direccion, string Ciudad, decimal Latitud, decimal Longitud);
public record HorarioSucursalDto(int Id, string DiaSemana, TimeOnly Abre, TimeOnly Cierra, bool EsFeriado);
public record ResumenMaquinas(int Total, int Disponibles, int EnMantenimiento);

public record SucursalDto(
    int Id, int CadenaId, string Cadena, string Nombre, string? Descripcion, int CapacidadMaxima,
    bool Activa, bool AbiertaAhora, int OcupacionActual, UbicacionDto? Ubicacion,
    IReadOnlyList<HorarioSucursalDto> Horarios, IReadOnlyList<string> Servicios, ResumenMaquinas Maquinas,
    double? DistanciaKm = null);

public record CadenaDto(int Id, string Nombre, string? Descripcion, bool Activa, int Sucursales);

public class SucursalForm
{
    public int CadenaId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int CapacidadMaxima { get; set; } = 100;
    public bool Activa { get; set; } = true;
    public UbicacionDto? Ubicacion { get; set; }
    public List<HorarioSucursalDto>? Horarios { get; set; }
}

public class CadenaForm
{
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activa { get; set; } = true;
}

public record EstadisticasSucursalHoy(int Capacidad, int Ocupacion, int ReservasHoy, int Pendientes, int Completadas);
public record GimnasioFrecuente(int SucursalId, string Nombre, string? Direccion, int Visitas, int AforoActual, int AforoMaximo, int Porcentaje, string Etiqueta);

public class SedesService(AppDbContext db, UsuarioActual usuario, Alcances alcances)
{
    // ── Cadenas ──────────────────────────────────────────────────────────

    public async Task<List<CadenaDto>> CadenasAsync()
    {
        var q = db.Cadenas.Where(c => c.Activa);
        if (!usuario.EsSuperAdmin && usuario.Autenticado && usuario.EsPersonal)
            q = q.Where(c => c.Id == usuario.CadenaId);
        return await q.OrderBy(c => c.Id)
            .Select(c => new CadenaDto(c.Id, c.Nombre, c.Descripcion, c.Activa, c.Sucursales.Count))
            .ToListAsync();
    }

    public async Task<CadenaDto> CrearCadenaAsync(CadenaForm f)
    {
        if (string.IsNullOrWhiteSpace(f.Nombre)) throw ErrorNegocio.Solicitud("El nombre de la cadena es obligatorio.");
        if (await db.Cadenas.AnyAsync(c => c.Nombre == f.Nombre.Trim()))
            throw ErrorNegocio.Conflicto("Ya existe una Marca registrada con ese nombre.");
        var c = new Cadena { Nombre = f.Nombre.Trim(), Descripcion = f.Descripcion, Activa = f.Activa };
        db.Cadenas.Add(c);
        await db.SaveChangesAsync();
        return new CadenaDto(c.Id, c.Nombre, c.Descripcion, c.Activa, 0);
    }

    public async Task ActualizarCadenaAsync(int id, CadenaForm f)
    {
        var c = await db.Cadenas.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado($"Cadena {id} no encontrada");
        if (await db.Cadenas.AnyAsync(x => x.Nombre == f.Nombre.Trim() && x.Id != id))
            throw ErrorNegocio.Conflicto("Ya existe una Marca registrada con ese nombre.");
        c.Nombre = f.Nombre.Trim();
        c.Descripcion = f.Descripcion;
        c.Activa = f.Activa;
        c.ActualizadaEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task EliminarCadenaAsync(int id)
    {
        var c = await db.Cadenas.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado($"Cadena {id} no encontrada");
        if (await db.Sucursales.IgnoreQueryFilters().AnyAsync(s => s.CadenaId == id))
            throw ErrorNegocio.Conflicto("La cadena tiene sucursales. Elimínalas o desactiva la cadena.");
        db.Cadenas.Remove(c);
        await db.SaveChangesAsync();
    }

    // ── Sucursales ───────────────────────────────────────────────────────

    /// <summary>
    /// Personal administrativo: las sucursales de su alcance. Clientes, entrenadores e
    /// instructores: el catálogo de sucursales activas, opcionalmente dentro de un radio.
    /// </summary>
    public async Task<List<SucursalDto>> SucursalesAsync(double? lat = null, double? lng = null, double radioKm = 50, int? cadenaId = null)
    {
        var q = usuario.Nivel >= Niveles.Recepcionista ? alcances.SucursalesGestionables() : db.Sucursales.AsQueryable();
        q = q.Where(s => s.Activa);
        if (cadenaId is not null) q = q.Where(s => s.CadenaId == cadenaId);
        var lista = await Consulta(q).OrderBy(s => s.Id).ToListAsync();
        var ocupacion = await OcupacionAsync(lista.Select(s => s.Id));

        var dtos = lista.Select(s => ADto(s, ocupacion.GetValueOrDefault(s.Id))).ToList();
        if (usuario.Nivel < Niveles.Recepcionista && lat is not null && lng is not null)
        {
            dtos = dtos.Where(d => d.Ubicacion is not null)
                .Select(d => d with { DistanciaKm = Math.Round(DistanciaKm(lat.Value, lng.Value, (double)d.Ubicacion!.Latitud, (double)d.Ubicacion.Longitud), 2) })
                .Where(d => d.DistanciaKm <= radioKm)
                .OrderBy(d => d.DistanciaKm)
                .ToList();
        }
        return dtos;
    }

    public async Task<SucursalDto> SucursalAsync(int id)
    {
        var s = await Consulta(db.Sucursales).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw ErrorNegocio.NoEncontrado($"Gimnasio {id} no encontrado");
        var ocupacion = await OcupacionAsync([id]);
        return ADto(s, ocupacion.GetValueOrDefault(id));
    }

    public async Task<SucursalDto> CrearSucursalAsync(SucursalForm f)
    {
        ValidarHorarios(f.Horarios);
        if (!await db.Cadenas.AnyAsync(c => c.Id == f.CadenaId))
            throw ErrorNegocio.Solicitud("La cadena especificada no existe.");
        if (await db.Sucursales.IgnoreQueryFilters().AnyAsync(s => s.CadenaId == f.CadenaId && s.Nombre == f.Nombre.Trim()))
            throw ErrorNegocio.Conflicto("Ya existe una sucursal con ese nombre en la cadena.");
        if (f.Ubicacion is not null && await db.Ubicaciones.AnyAsync(u => u.Latitud == f.Ubicacion.Latitud && u.Longitud == f.Ubicacion.Longitud))
            throw ErrorNegocio.Conflicto("Ya existe una sucursal registrada exactamente en esa ubicación del mapa.");

        var s = new Sucursal
        {
            CadenaId = f.CadenaId,
            Nombre = f.Nombre.Trim(),
            Descripcion = f.Descripcion,
            CapacidadMaxima = f.CapacidadMaxima,
            Activa = f.Activa,
            Ubicacion = f.Ubicacion is null ? null : new UbicacionSucursal { Direccion = f.Ubicacion.Direccion, Ciudad = f.Ubicacion.Ciudad, Latitud = f.Ubicacion.Latitud, Longitud = f.Ubicacion.Longitud },
            Horarios = (f.Horarios ?? []).Select(h => new HorarioSucursal { DiaSemana = h.DiaSemana, Abre = h.Abre, Cierra = h.Cierra, EsFeriado = h.EsFeriado }).ToList(),
        };
        db.Sucursales.Add(s);
        await db.SaveChangesAsync();
        return await SucursalAsync(s.Id);
    }

    public async Task<SucursalDto> ActualizarSucursalAsync(int id, SucursalForm f)
    {
        await alcances.ExigirSucursalAsync(id);
        ValidarHorarios(f.Horarios);
        var s = await db.Sucursales.Include(x => x.Ubicacion).Include(x => x.Horarios).FirstAsync(x => x.Id == id);
        if (await db.Sucursales.IgnoreQueryFilters().AnyAsync(x => x.CadenaId == s.CadenaId && x.Nombre == f.Nombre.Trim() && x.Id != id))
            throw ErrorNegocio.Conflicto("Ya existe una sucursal con ese nombre en la cadena.");

        s.Nombre = f.Nombre.Trim();
        s.Descripcion = f.Descripcion;
        s.CapacidadMaxima = f.CapacidadMaxima;
        s.Activa = f.Activa;
        s.ActualizadaEn = DateTime.UtcNow;
        if (f.Ubicacion is not null)
        {
            s.Ubicacion ??= new UbicacionSucursal();
            s.Ubicacion.Direccion = f.Ubicacion.Direccion;
            s.Ubicacion.Ciudad = f.Ubicacion.Ciudad;
            s.Ubicacion.Latitud = f.Ubicacion.Latitud;
            s.Ubicacion.Longitud = f.Ubicacion.Longitud;
        }
        if (f.Horarios is not null)
        {
            db.HorariosSucursal.RemoveRange(s.Horarios);
            s.Horarios = f.Horarios.Select(h => new HorarioSucursal { DiaSemana = h.DiaSemana, Abre = h.Abre, Cierra = h.Cierra, EsFeriado = h.EsFeriado }).ToList();
        }
        await db.SaveChangesAsync();
        return await SucursalAsync(id);
    }

    public async Task EliminarSucursalAsync(int id)
    {
        var s = await db.Sucursales.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado($"Gimnasio {id} no encontrado");
        db.Sucursales.Remove(s);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw ErrorNegocio.Conflicto("La sucursal tiene datos asociados (personal, clases o reservas). Desactívala en lugar de eliminarla.");
        }
    }

    public async Task<HorarioSucursalDto> AgregarHorarioAsync(int sucursalId, HorarioSucursalDto h)
    {
        await alcances.ExigirSucursalAsync(sucursalId, "No puede agregar horarios a otra sucursal");
        var existentes = await db.HorariosSucursal.Where(x => x.SucursalId == sucursalId)
            .Select(x => new HorarioSucursalDto(x.Id, x.DiaSemana, x.Abre, x.Cierra, x.EsFeriado)).ToListAsync();
        ValidarHorarios([.. existentes, h]);
        var nuevo = new HorarioSucursal { SucursalId = sucursalId, DiaSemana = h.DiaSemana.ToUpperInvariant(), Abre = h.Abre, Cierra = h.Cierra, EsFeriado = h.EsFeriado };
        db.HorariosSucursal.Add(nuevo);
        await db.SaveChangesAsync();
        return new HorarioSucursalDto(nuevo.Id, nuevo.DiaSemana, nuevo.Abre, nuevo.Cierra, nuevo.EsFeriado);
    }

    public Task<List<HorarioSucursalDto>> HorariosAsync(int sucursalId) =>
        db.HorariosSucursal.Where(x => x.SucursalId == sucursalId)
            .Select(x => new HorarioSucursalDto(x.Id, x.DiaSemana, x.Abre, x.Cierra, x.EsFeriado)).ToListAsync();

    public async Task EliminarHorarioAsync(int horarioId)
    {
        var h = await db.HorariosSucursal.FindAsync(horarioId) ?? throw ErrorNegocio.NoEncontrado($"Horario {horarioId} no encontrado");
        await alcances.ExigirSucursalAsync(h.SucursalId, "No puede eliminar horarios de otra sucursal");
        db.HorariosSucursal.Remove(h);
        await db.SaveChangesAsync();
    }

    // ── Ocupación y estadísticas ─────────────────────────────────────────

    /// <summary>Personas dentro de cada sucursal: ingresos activos sin salida.</summary>
    public async Task<Dictionary<int, int>> OcupacionAsync(IEnumerable<int> sucursales)
    {
        var ids = sucursales.ToList();
        return await db.Accesos.IgnoreQueryFilters()
            .Where(a => ids.Contains(a.SucursalId) && a.Salida == null && a.Estado == EstadosAcceso.Activo)
            .GroupBy(a => a.SucursalId)
            .Select(g => new { g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Cantidad);
    }

    public async Task<object> EstadisticasOcupacionAsync(int sucursalId, string periodo = "day")
    {
        var s = await db.Sucursales.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == sucursalId)
                ?? throw ErrorNegocio.NoEncontrado($"Gym {sucursalId} no encontrado");
        var hoy = Reloj.Hoy;
        var desde = periodo switch
        {
            "year" => new DateOnly(hoy.Year, 1, 1),
            "month" => new DateOnly(hoy.Year, hoy.Month, 1),
            "week" => hoy.AddDays(-(int)hoy.DayOfWeek),
            _ => hoy,
        };
        var reservas = db.Reservas.IgnoreQueryFilters()
            .Where(r => r.SucursalId == sucursalId && r.Estado == EstadosReserva.Asistida && r.Fecha >= desde);
        var completadas = await reservas.CountAsync();
        var desglose = await reservas.GroupBy(r => r.Fecha).OrderBy(g => g.Key)
            .Select(g => new { fecha = g.Key, cantidad = g.Count() }).ToListAsync();
        var capacidad = Math.Max(1, s.CapacidadMaxima);
        return new
        {
            sucursalId,
            sucursal = s.Nombre,
            periodo,
            desde,
            reservasCompletadas = completadas,
            capacidadMaxima = capacidad,
            porcentajeOcupacion = periodo == "day" ? Math.Min(100, (int)Math.Round(completadas * 100.0 / capacidad)) : (int?)null,
            desglose,
        };
    }

    public async Task<EstadisticasSucursalHoy> EstadisticasHoyAsync(int? sucursalId)
    {
        var id = usuario.SucursalFiltro ?? sucursalId
                 ?? throw ErrorNegocio.Solicitud("Proporciona ?sucursalId para consultar una sucursal.");
        var s = await alcances.ExigirSucursalAsync(id);
        var hoy = Reloj.Hoy;
        var reservas = db.Reservas.Where(r => r.SucursalId == id && r.Fecha == hoy);
        var total = await reservas.CountAsync();
        var pendientes = await reservas.CountAsync(r => r.Estado == EstadosReserva.Confirmada);
        var completadas = await reservas.CountAsync(r => r.Estado == EstadosReserva.Asistida);
        return new EstadisticasSucursalHoy(s.CapacidadMaxima, completadas, total, pendientes, completadas);
    }

    public async Task<List<GimnasioFrecuente>> FrecuentesAsync(int usuarioId)
    {
        var top = await db.Accesos.IgnoreQueryFilters().Where(a => a.UsuarioId == usuarioId)
            .GroupBy(a => a.SucursalId).Select(g => new { SucursalId = g.Key, Visitas = g.Count() })
            .OrderByDescending(x => x.Visitas).Take(3).ToListAsync();
        if (top.Count == 0) return [];
        var ids = top.Select(t => t.SucursalId).ToList();
        var sucursales = await db.Sucursales.IgnoreQueryFilters().Include(s => s.Ubicacion).Where(s => ids.Contains(s.Id)).ToDictionaryAsync(s => s.Id);
        var ocupacion = await OcupacionAsync(ids);
        return top.Select(t =>
        {
            var s = sucursales[t.SucursalId];
            var max = s.CapacidadMaxima > 0 ? s.CapacidadMaxima : 100;
            var actual = ocupacion.GetValueOrDefault(t.SucursalId);
            var pct = (int)Math.Round(actual * 100.0 / max);
            var etiqueta = pct < 40 ? "Tranquilo" : pct <= 70 ? "Moderado" : "Concurrido";
            return new GimnasioFrecuente(s.Id, s.Nombre, s.Ubicacion?.Direccion, t.Visitas, actual, max, pct, etiqueta);
        }).ToList();
    }

    public Task<List<object>> UbicacionesAsync() =>
        db.Ubicaciones.Join(db.Sucursales.Where(s => s.Activa), u => u.SucursalId, s => s.Id,
                (u, s) => (object)new { sucursalId = s.Id, sucursal = s.Nombre, cadena = s.Cadena!.Nombre, latitud = u.Latitud, longitud = u.Longitud })
            .ToListAsync();

    // ── Apoyo ────────────────────────────────────────────────────────────

    private static IQueryable<Sucursal> Consulta(IQueryable<Sucursal> q) =>
        q.Include(s => s.Cadena).Include(s => s.Ubicacion).Include(s => s.Horarios)
            .Include(s => s.Actividades).Include(s => s.Maquinas).AsSplitQuery();

    private static SucursalDto ADto(Sucursal s, int ocupacion) => new(
        s.Id, s.CadenaId, s.Cadena?.Nombre ?? "", s.Nombre, s.Descripcion, s.CapacidadMaxima, s.Activa,
        AbiertaAhora(s.Horarios), ocupacion,
        s.Ubicacion is null ? null : new UbicacionDto(s.Ubicacion.Direccion, s.Ubicacion.Ciudad, s.Ubicacion.Latitud, s.Ubicacion.Longitud),
        s.Horarios.OrderBy(h => Array.IndexOf(Dias.Cortos, h.DiaSemana)).Select(h => new HorarioSucursalDto(h.Id, h.DiaSemana, h.Abre, h.Cierra, h.EsFeriado)).ToList(),
        s.Actividades.Where(a => a.Activa).Select(a => a.Nombre).ToList(),
        new ResumenMaquinas(s.Maquinas.Count, s.Maquinas.Count(m => m.Estado == EstadosMaquina.Disponible), s.Maquinas.Count(m => m.Estado == EstadosMaquina.Mantenimiento)));

    /// <summary>Igual que isGymOpenNow del proyecto base.</summary>
    public static bool AbiertaAhora(IEnumerable<HorarioSucursal> horarios)
    {
        var ahora = Reloj.Ahora;
        var codigo = Dias.Cortos[((int)ahora.DayOfWeek + 6) % 7];
        var hoy = horarios.FirstOrDefault(h => h.DiaSemana == codigo && !h.EsFeriado);
        if (hoy is null) return false;
        var hora = TimeOnly.FromDateTime(ahora);
        return hora >= hoy.Abre && hora < hoy.Cierra;
    }

    /// <summary>Igual que validateScheduleOverlaps: no se permiten rangos superpuestos el mismo día.</summary>
    private static void ValidarHorarios(IEnumerable<HorarioSucursalDto>? horarios)
    {
        if (horarios is null) return;
        foreach (var dia in horarios.Where(h => !h.EsFeriado).GroupBy(h => h.DiaSemana.Trim().ToUpperInvariant()))
        {
            var lista = dia.ToList();
            foreach (var h in lista.Where(h => h.Cierra <= h.Abre))
                throw ErrorNegocio.Solicitud($"En {dia.Key} la hora de cierre debe ser posterior a la de apertura.");
            for (var i = 0; i < lista.Count; i++)
                for (var j = i + 1; j < lista.Count; j++)
                    if (lista[i].Abre < lista[j].Cierra && lista[i].Cierra > lista[j].Abre)
                        throw ErrorNegocio.Solicitud($"Horarios superpuestos en {dia.Key}: el rango {lista[i].Abre:HH\\:mm}–{lista[i].Cierra:HH\\:mm} choca con {lista[j].Abre:HH\\:mm}–{lista[j].Cierra:HH\\:mm}.");
        }
    }

    public static double DistanciaKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371;
        double Rad(double g) => g * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLng = Rad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
