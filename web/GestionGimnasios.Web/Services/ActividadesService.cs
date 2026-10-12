using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record HorarioActividadDto(
    int Id, int ActividadId, string Actividad, int SucursalId, string Sucursal, string DiaSemana,
    TimeOnly Inicio, TimeOnly Fin, int CupoMaximo, bool Recurrente, int? InstructorId, string? Instructor);

public record ActividadDto(
    int Id, int SucursalId, string Sucursal, string Nombre, string? Descripcion, int? DuracionMin,
    bool Activa, bool AccesoLibre, IReadOnlyList<HorarioActividadDto> Horarios);

public record InstructorDto(int Id, string NombreCompleto, int? SucursalId, string? Sucursal);

public record AsistenciaDto(int Id, int UsuarioId, string Usuario, DateTime Ingreso, string Estado);

public class ActividadForm
{
    public int SucursalId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int? DuracionMin { get; set; }
    public bool AccesoLibre { get; set; }
    public bool Activa { get; set; } = true;
}

public class HorarioActividadForm
{
    public int? InstructorId { get; set; }
    public string DiaSemana { get; set; } = "LUNES";
    public TimeOnly Inicio { get; set; }
    public TimeOnly Fin { get; set; }
    public int CupoMaximo { get; set; } = 15;
    public bool Recurrente { get; set; } = true;
}

/// <summary>Actividades (clases y servicios), sus horarios y la asistencia (CU-16, CU-17).</summary>
public class ActividadesService(AppDbContext db, UsuarioActual usuario, Alcances alcances)
{
    public async Task<List<ActividadDto>> ListarAsync(int? sucursalId = null, bool incluirInactivas = false)
    {
        IQueryable<Actividad> q = db.Actividades;
        if (usuario.Nivel >= Niveles.Recepcionista)
        {
            var visibles = alcances.SucursalesGestionables().Select(s => s.Id);
            q = q.Where(a => visibles.Contains(a.SucursalId));
        }
        if (sucursalId is not null) q = q.Where(a => a.SucursalId == sucursalId);
        if (!incluirInactivas) q = q.Where(a => a.Activa);
        var lista = await Consulta(q).OrderBy(a => a.Sucursal!.Nombre).ThenBy(a => a.Nombre).ToListAsync();
        return lista.Select(ADto).ToList();
    }

    public async Task<ActividadDto> ObtenerAsync(int id)
    {
        var a = await Consulta(db.Actividades).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw ErrorNegocio.NoEncontrado($"Actividad {id} no encontrada");
        if (usuario.Nivel >= Niveles.Recepcionista && !await alcances.SucursalesGestionables().AnyAsync(s => s.Id == a.SucursalId))
            throw ErrorNegocio.Prohibido("No tiene permisos para acceder a esta actividad");
        return ADto(a);
    }

    public async Task<ActividadDto> CrearAsync(ActividadForm f)
    {
        if (usuario.EsGerente && f.SucursalId == 0) throw ErrorNegocio.Solicitud("Debes seleccionar una sucursal para el servicio.");
        var s = await alcances.ExigirSucursalAsync(alcances.ForzarSucursal(f.SucursalId), "La sucursal seleccionada no pertenece a tu marca.");
        Validar(f);
        var a = new Actividad
        {
            CadenaId = s.CadenaId, SucursalId = s.Id, Nombre = f.Nombre.Trim(), Descripcion = f.Descripcion,
            DuracionMin = f.DuracionMin, AccesoLibre = f.AccesoLibre, Activa = f.Activa,
        };
        db.Actividades.Add(a);
        await db.SaveChangesAsync();
        return await ObtenerAsync(a.Id);
    }

    public async Task<ActividadDto> ActualizarAsync(int id, ActividadForm f)
    {
        var a = await EnAlcance(id, "No tiene permisos para gestionar esta actividad");
        Validar(f);
        if (f.SucursalId != 0 && f.SucursalId != a.SucursalId)
        {
            var s = await alcances.ExigirSucursalAsync(f.SucursalId, "No puede reasignar la actividad a una sucursal fuera de tu marca.");
            a.SucursalId = s.Id;
        }
        a.Nombre = f.Nombre.Trim();
        a.Descripcion = f.Descripcion;
        a.DuracionMin = f.DuracionMin;
        a.AccesoLibre = f.AccesoLibre;
        a.Activa = f.Activa;
        await db.SaveChangesAsync();
        return await ObtenerAsync(id);
    }

    /// <summary>Como el proyecto base: borra los horarios y desactiva la actividad.</summary>
    public async Task<string> EliminarAsync(int id)
    {
        var a = await EnAlcance(id, "No tiene permisos para gestionar esta actividad");
        var horarios = await db.HorariosActividad.Where(h => h.ActividadId == id).Select(h => h.Id).ToListAsync();
        var conReservas = await db.Reservas.AnyAsync(r => r.HorarioActividadId != null && horarios.Contains(r.HorarioActividadId.Value));
        if (!conReservas) await db.HorariosActividad.Where(h => h.ActividadId == id).ExecuteDeleteAsync();
        a.Activa = false;
        await db.SaveChangesAsync();
        return "Servicio y sus horarios asociados eliminados correctamente.";
    }

    /// <summary>Instructores activos (nivel 2) del alcance, para asignarlos a un horario.</summary>
    public async Task<List<InstructorDto>> InstructoresElegiblesAsync(int? sucursalId = null)
    {
        if (usuario.Nivel < Niveles.Recepcionista) throw ErrorNegocio.Prohibido("Sin permisos administrativos para ver instructores.");
        var visibles = alcances.SucursalesGestionables().Select(s => s.Id);
        var q = db.UsuarioRoles.Where(r => r.Rol!.Nivel == Niveles.Staff && r.Usuario!.Activo && r.SucursalId != null && visibles.Contains(r.SucursalId.Value));
        if (sucursalId is not null) q = q.Where(r => r.SucursalId == sucursalId);
        return await q.Select(r => new { r.UsuarioId, r.Usuario!.Perfil!.Nombres, r.Usuario.Perfil.Apellidos, r.SucursalId, Sucursal = r.Sucursal!.Nombre })
            .Distinct().OrderBy(x => x.Apellidos)
            .Select(x => new InstructorDto(x.UsuarioId, x.Nombres + " " + x.Apellidos, x.SucursalId, x.Sucursal))
            .ToListAsync();
    }

    // ── Horarios de clase ────────────────────────────────────────────────

    public async Task<HorarioActividadDto> CrearHorarioAsync(int actividadId, HorarioActividadForm f)
    {
        var a = await EnAlcance(actividadId, "No tiene permisos para gestionar esta actividad");
        if (!a.Activa) throw ErrorNegocio.Solicitud("La actividad no está activa");
        var dia = NormalizarDia(f.DiaSemana);
        if (f.Fin <= f.Inicio) throw ErrorNegocio.Solicitud("La hora de fin debe ser posterior a la de inicio.");
        if (f.CupoMaximo < 1) throw ErrorNegocio.Solicitud("El cupo máximo debe ser al menos 1.");

        if (f.InstructorId is int instructorId)
        {
            await ExigirInstructorAsync(instructorId, a.SucursalId);
            var choca = await db.HorariosActividad.IgnoreQueryFilters().AnyAsync(h =>
                h.InstructorId == instructorId && h.DiaSemana == dia && h.Inicio < f.Fin && h.Fin > f.Inicio);
            if (choca) throw ErrorNegocio.Conflicto("El instructor ya tiene una clase en este horario");
        }
        await ExigirDentroDeHorarioAsync(a.SucursalId, dia, f.Inicio, f.Fin);

        var h = new HorarioActividad
        {
            CadenaId = a.CadenaId, ActividadId = a.Id, InstructorId = f.InstructorId, DiaSemana = dia,
            Inicio = f.Inicio, Fin = f.Fin, CupoMaximo = f.CupoMaximo, Recurrente = f.Recurrente,
        };
        db.HorariosActividad.Add(h);
        await db.SaveChangesAsync();
        return (await HorariosAsync(actividadId)).First(x => x.Id == h.Id);
    }

    public async Task<List<HorarioActividadDto>> HorariosAsync(int actividadId) =>
        (await db.HorariosActividad.Where(h => h.ActividadId == actividadId)
            .Include(h => h.Actividad).ThenInclude(a => a!.Sucursal)
            .Include(h => h.Instructor).ThenInclude(i => i!.Perfil)
            .ToListAsync())
        .OrderBy(h => Array.IndexOf(Dias.Largos, h.DiaSemana)).ThenBy(h => h.Inicio)
        .Select(AHorario).ToList();

    public async Task<string> EliminarHorarioAsync(int horarioId)
    {
        var h = await HorarioEnAlcance(horarioId);
        if (await db.Reservas.AnyAsync(r => r.HorarioActividadId == horarioId && EstadosReserva.Activas.Contains(r.Estado) && r.Fecha >= Reloj.Hoy))
            throw ErrorNegocio.Conflicto("El horario tiene reservas próximas. Cancélalas antes de eliminarlo.");
        db.HorariosActividad.Remove(h);
        await db.Reservas.Where(r => r.HorarioActividadId == horarioId).ExecuteUpdateAsync(s => s.SetProperty(r => r.HorarioActividadId, (int?)null));
        await db.SaveChangesAsync();
        return $"Horario {horarioId} eliminado";
    }

    /// <summary>Agenda semanal de un instructor (getInstructorSchedules).</summary>
    public async Task<List<object>> HorariosDeInstructorAsync(int instructorId) =>
        (await db.HorariosActividad.IgnoreQueryFilters().Where(h => h.InstructorId == instructorId)
            .Include(h => h.Actividad).ThenInclude(a => a!.Sucursal).ToListAsync())
        .OrderBy(h => Array.IndexOf(Dias.Largos, h.DiaSemana)).ThenBy(h => h.Inicio)
        .Select(h => (object)new { dia = h.DiaSemana, hora = $"{h.Inicio:HH\\:mm} - {h.Fin:HH\\:mm}", actividad = h.Actividad?.Nombre ?? "—", sucursal = h.Actividad?.Sucursal?.Nombre ?? "—", horarioId = h.Id })
        .ToList();

    // ── Asistencia (CU-17) ───────────────────────────────────────────────

    public async Task<AsistenciaDto> RegistrarAsistenciaAsync(int horarioId, int usuarioId)
    {
        var h = await HorarioEnAlcance(horarioId);
        if (usuario.Nivel == Niveles.Staff && h.InstructorId != usuario.Id)
            throw ErrorNegocio.Prohibido("Solo el instructor de la clase puede registrar su asistencia.");
        var hoyUtc = Reloj.InicioDelDiaUtc(Reloj.Hoy);
        var existente = await db.Asistencias.Include(x => x.Usuario).ThenInclude(u => u!.Perfil)
            .FirstOrDefaultAsync(x => x.HorarioActividadId == horarioId && x.UsuarioId == usuarioId && x.Ingreso >= hoyUtc);
        if (existente is not null) return AAsistencia(existente);

        var asistencia = new AsistenciaActividad { HorarioActividadId = horarioId, UsuarioId = usuarioId };
        db.Asistencias.Add(asistencia);
        // La reserva del día pasa a asistida, como el check-in de reserva.
        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.HorarioActividadId == horarioId && r.UsuarioId == usuarioId && r.Fecha == Reloj.Hoy && r.Estado == EstadosReserva.Confirmada);
        if (reserva is not null) reserva.Estado = EstadosReserva.Asistida;
        await db.SaveChangesAsync();
        await db.Entry(asistencia).Reference(x => x.Usuario).Query().Include(u => u.Perfil).LoadAsync();
        return AAsistencia(asistencia);
    }

    public async Task<List<AsistenciaDto>> AsistenciasAsync(int horarioId, DateOnly? fecha = null)
    {
        await HorarioEnAlcance(horarioId);
        var q = db.Asistencias.Where(a => a.HorarioActividadId == horarioId);
        if (fecha is not null)
        {
            var desde = Reloj.InicioDelDiaUtc(fecha.Value);
            q = q.Where(a => a.Ingreso >= desde && a.Ingreso < desde.AddDays(1));
        }
        return (await q.Include(a => a.Usuario).ThenInclude(u => u!.Perfil).OrderBy(a => a.Ingreso).ToListAsync()).Select(AAsistencia).ToList();
    }

    // ── Apoyo ────────────────────────────────────────────────────────────

    private async Task<Actividad> EnAlcance(int id, string mensaje)
    {
        var a = await db.Actividades.FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado($"Actividad {id} no encontrada");
        if (!await alcances.SucursalesGestionables().AnyAsync(s => s.Id == a.SucursalId)) throw ErrorNegocio.Prohibido(mensaje);
        return a;
    }

    private async Task<HorarioActividad> HorarioEnAlcance(int horarioId)
    {
        var h = await db.HorariosActividad.Include(x => x.Actividad).FirstOrDefaultAsync(x => x.Id == horarioId)
                ?? throw ErrorNegocio.NoEncontrado($"Horario {horarioId} no encontrado");
        if (usuario.Nivel >= Niveles.Recepcionista && !await alcances.SucursalesGestionables().AnyAsync(s => s.Id == h.Actividad!.SucursalId))
            throw ErrorNegocio.Prohibido("No tiene permisos para gestionar este horario");
        return h;
    }

    private async Task ExigirInstructorAsync(int instructorId, int sucursalId)
    {
        var u = await db.Usuarios.Include(x => x.Roles).ThenInclude(r => r.Rol).FirstOrDefaultAsync(x => x.Id == instructorId)
                ?? throw ErrorNegocio.NoEncontrado($"Usuario instructor {instructorId} no encontrado");
        if (!u.Activo) throw ErrorNegocio.Prohibido("El instructor indicado no está activo");
        if (!u.Roles.Any(r => r.Rol!.Nivel == Niveles.Staff))
            throw ErrorNegocio.Prohibido("El usuario indicado no tiene rol de instructor (nivel 2)");
        if (!u.Roles.Any(r => r.SucursalId == sucursalId))
            throw ErrorNegocio.Prohibido("El instructor debe pertenecer a la misma sucursal de la actividad.");
    }

    /// <summary>La clase debe caer dentro del horario de apertura de la sede (si está configurado).</summary>
    private async Task ExigirDentroDeHorarioAsync(int sucursalId, string dia, TimeOnly inicio, TimeOnly fin)
    {
        var corto = Dias.Cortos[Array.IndexOf(Dias.Largos, dia)];
        var franjas = await db.HorariosSucursal.Where(h => h.SucursalId == sucursalId && !h.EsFeriado && h.DiaSemana == corto).ToListAsync();
        if (franjas.Count == 0) return;
        if (!franjas.Any(h => h.Abre <= inicio && h.Cierra >= fin))
            throw ErrorNegocio.Conflicto("El horario de la clase queda fuera del horario de apertura configurado para la sede");
    }

    public static string NormalizarDia(string dia)
    {
        var d = dia.Trim().ToUpperInvariant().Replace("É", "E").Replace("Á", "A");
        var i = Array.IndexOf(Dias.Largos, d);
        if (i < 0) i = Array.IndexOf(Dias.Cortos, d);
        if (i < 0) throw ErrorNegocio.Solicitud($"Día de la semana no válido: {dia}.");
        return Dias.Largos[i];
    }

    private static IQueryable<Actividad> Consulta(IQueryable<Actividad> q) =>
        q.Include(a => a.Sucursal).Include(a => a.Horarios).ThenInclude(h => h.Instructor).ThenInclude(i => i!.Perfil).AsSplitQuery();

    private static ActividadDto ADto(Actividad a) => new(
        a.Id, a.SucursalId, a.Sucursal?.Nombre ?? "", a.Nombre, a.Descripcion, a.DuracionMin, a.Activa, a.AccesoLibre,
        a.Horarios.OrderBy(h => Array.IndexOf(Dias.Largos, h.DiaSemana)).ThenBy(h => h.Inicio)
            .Select(h => { h.Actividad = a; return AHorario(h); }).ToList());

    public static HorarioActividadDto AHorario(HorarioActividad h) => new(
        h.Id, h.ActividadId, h.Actividad?.Nombre ?? "", h.Actividad?.SucursalId ?? 0, h.Actividad?.Sucursal?.Nombre ?? "",
        h.DiaSemana, h.Inicio, h.Fin, h.CupoMaximo, h.Recurrente, h.InstructorId,
        h.Instructor?.Perfil is { } p ? $"{p.Nombres} {p.Apellidos}" : null);

    private static AsistenciaDto AAsistencia(AsistenciaActividad a) =>
        new(a.Id, a.UsuarioId, a.Usuario?.Perfil?.NombreCompleto ?? a.Usuario?.Email ?? "", a.Ingreso, a.Estado);

    private static void Validar(ActividadForm f)
    {
        if (string.IsNullOrWhiteSpace(f.Nombre)) throw ErrorNegocio.Solicitud("El nombre de la actividad es obligatorio.");
        if (f.DuracionMin is < 0) throw ErrorNegocio.Solicitud("La duración no puede ser negativa.");
    }
}
