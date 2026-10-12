using System.Text.Json;
using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record TurnoDto(int Id, int SucursalId, string Sucursal, int DiaSemana, string Dia, TimeOnly Inicio, TimeOnly Fin);
public record TurnoForm(int DiaSemana, TimeOnly Inicio, TimeOnly Fin);
public record ParticipanteDto(int ReservaId, int UsuarioId, string NombreCompleto, string? AvatarUrl, string Estado);
public record ClaseAgendaDto(int HorarioId, string Actividad, string Sucursal, string DiaSemana, TimeOnly Inicio, TimeOnly Fin, int CupoMaximo, int Inscritos, bool Terminada, IReadOnlyList<ParticipanteDto> Participantes);
public record EstadisticaClaseDto(int HorarioId, string Actividad, string DiaSemana, TimeOnly Inicio, int Asistencias30Dias);
public record AsesorDto(int Id, string NombreCompleto, string Rol, int? SucursalId, string? Sucursal, string? Cadena, string Especialidad, string? AvatarUrl);
public record AsesoriaDto(int Id, int ClienteId, string Cliente, string? TelefonoCliente, string? CiCliente, string? EmailCliente, int AsesorId, string Asesor, string RolAsesor, string? SucursalAsesor, string Estado, int? MetaSesionesSemana, DateTime CreadaEn);
public record ActividadAlumnoDto(int AsesoriaId, int ClienteId, string Cliente, DateTime? UltimaSesion, int Sesiones, int SesionesSemana, int? MetaSesionesSemana, bool Inactivo);
public record FichaClienteDto(int ClienteId, PerfilDto Perfil, MetricaDto? UltimaMetrica, RestriccionDto? Restriccion, PerfilEntrenamientoDto? Entrenamiento, IReadOnlyList<SesionResumenDto> UltimasSesiones);
public record SesionResumenDto(int Id, DateTime Inicio, string Actividad, int? DuracionSeg, int Series);
public record PlanNutricionalDto(int Id, int AsesorId, string Asesor, int ClienteId, string Cliente, int? KcalDiarias, decimal? ProteinaG, decimal? CarbohidratosG, decimal? GrasaG, string? Notas, Dictionary<string, Dictionary<string, string>> PlanComidas, DateTime ActualizadoEn);
public record CitaDto(int Id, int PacienteId, string Paciente, string? AvatarUrl, DateOnly Fecha, TimeOnly Hora, string Tipo, string Estado, string? Notas);

public class PlanNutricionalForm
{
    public int? KcalDiarias { get; set; }
    public decimal? ProteinaG { get; set; }
    public decimal? CarbohidratosG { get; set; }
    public decimal? GrasaG { get; set; }
    public string? Notas { get; set; }
    /// <summary>Día → comida → descripción (meal_plan del proyecto base).</summary>
    public Dictionary<string, Dictionary<string, string>>? PlanComidas { get; set; }
}

public class CitaForm
{
    public int PacienteId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public string Tipo { get; set; } = "CONSULTA";
    public string? Notas { get; set; }
}

/// <summary>
/// Personal y asesorías (CU-08, CU-09, CU-10, CU-17, CU-28, CU-29, CU-34): turnos, agenda del
/// instructor, catálogo de asesores, solicitudes de asesoría, ficha del cliente, plan
/// nutricional y citas.
/// </summary>
public class PersonalService(AppDbContext db, UsuarioActual actual, Alcances alcances, DatosPersonales datos, NotificacionesService notificaciones, EntrenamientoService entrenamiento)
{
    private static readonly string[] NombresDia = ["DOMINGO", "LUNES", "MARTES", "MIERCOLES", "JUEVES", "VIERNES", "SABADO"];
    public static readonly string[] EstadosCita = ["PENDIENTE", "CONFIRMADA", "ATENDIDA", "CANCELADA", "NO_ASISTIO"];

    // ── Turnos del personal (CU-08) ──────────────────────────────────────

    public async Task<List<TurnoDto>> TurnosAsync(int usuarioId)
    {
        if (usuarioId != actual.Id) await datos.ExigirAsync(usuarioId);
        return (await db.HorariosPersonal.IgnoreQueryFilters().Where(h => h.UsuarioId == usuarioId).Include(h => h.Sucursal)
                .OrderBy(h => h.SucursalId).ThenBy(h => h.DiaSemana).ToListAsync())
            .Select(h => new TurnoDto(h.Id, h.SucursalId, h.Sucursal!.Nombre, h.DiaSemana, NombresDia[h.DiaSemana], h.Inicio, h.Fin)).ToList();
    }

    /// <summary>Reemplaza los turnos de un empleado en una sucursal, dentro del horario de atención.</summary>
    public async Task<List<TurnoDto>> AsignarTurnosAsync(int usuarioId, int sucursalId, IReadOnlyList<TurnoForm> turnos)
    {
        var s = await alcances.ExigirSucursalAsync(sucursalId, "Solo puedes asignar horarios a empleados que trabajan en tus propias sucursales.");
        var empleado = await db.UsuarioRoles.IgnoreQueryFilters().Include(r => r.Rol)
            .Where(r => r.UsuarioId == usuarioId && r.Rol!.Nivel > Niveles.Cliente).OrderByDescending(r => r.Rol!.Nivel).FirstOrDefaultAsync()
            ?? throw ErrorNegocio.NoEncontrado("El usuario no es parte del personal.");
        if (empleado.SucursalId != sucursalId && empleado.CadenaId != s.CadenaId)
            throw ErrorNegocio.Prohibido($"El empleado no trabaja en la sucursal \"{s.Nombre}\".");
        if (!actual.EsSuperAdmin && empleado.Rol!.Nivel >= actual.Nivel)
            throw ErrorNegocio.Prohibido("No puedes asignar turnos a personal de nivel igual o superior al tuyo.");

        var horarios = await db.HorariosSucursal.Where(h => h.SucursalId == sucursalId).ToListAsync();
        foreach (var t in turnos)
        {
            if (t.DiaSemana is < 0 or > 6) throw ErrorNegocio.Solicitud("Día de la semana no válido (0 = domingo … 6 = sábado).");
            if (t.Fin <= t.Inicio) throw ErrorNegocio.Solicitud($"Turno inválido el {NombresDia[t.DiaSemana]}: la hora de fin debe ser posterior al inicio.");
            if (horarios.Count == 0) continue;
            var corto = Dias.Cortos[(t.DiaSemana + 6) % 7];
            var dia = horarios.FirstOrDefault(h => h.DiaSemana == corto)
                      ?? throw ErrorNegocio.Solicitud($"La sucursal no tiene horario operativo registrado para {NombresDia[t.DiaSemana]}. Configura primero el horario de atención de la sucursal.");
            if (dia.EsFeriado) throw ErrorNegocio.Solicitud($"{NombresDia[t.DiaSemana]} está marcado como día no operativo en esta sucursal.");
            if (t.Inicio < dia.Abre || t.Fin > dia.Cierra)
                throw ErrorNegocio.Solicitud($"Turno inválido el {NombresDia[t.DiaSemana]}: {t.Inicio:HH\\:mm}–{t.Fin:HH\\:mm} está fuera del horario de atención ({dia.Abre:HH\\:mm}–{dia.Cierra:HH\\:mm}).");
        }
        if (turnos.GroupBy(t => t.DiaSemana).Any(g => g.Count() > 1)) throw ErrorNegocio.Solicitud("Solo se permite un turno por día en cada sucursal.");

        await db.HorariosPersonal.Where(h => h.UsuarioId == usuarioId && h.SucursalId == sucursalId).ExecuteDeleteAsync();
        db.HorariosPersonal.AddRange(turnos.Select(t => new HorarioPersonal { CadenaId = s.CadenaId, UsuarioId = usuarioId, SucursalId = sucursalId, DiaSemana = t.DiaSemana, Inicio = t.Inicio, Fin = t.Fin }));
        await db.SaveChangesAsync();
        return await TurnosAsync(usuarioId);
    }

    // ── Agenda del instructor (CU-17, CU-34) ─────────────────────────────

    /// <summary>Clases que dicta el instructor: las de hoy o toda la semana, con sus inscritos de hoy.</summary>
    public async Task<List<ClaseAgendaDto>> AgendaAsync(bool soloHoy)
    {
        var hoy = Reloj.Hoy;
        var diaHoy = Dias.Largo(hoy.DayOfWeek);
        var q = db.HorariosActividad.IgnoreQueryFilters().Where(h => h.InstructorId == actual.Id);
        if (soloHoy) q = q.Where(h => h.DiaSemana == diaHoy);
        var horarios = await q.Include(h => h.Actividad).ThenInclude(a => a!.Sucursal).ToListAsync();
        var ids = horarios.Select(h => h.Id).ToList();
        var reservas = await db.Reservas.IgnoreQueryFilters()
            .Where(r => r.HorarioActividadId != null && ids.Contains(r.HorarioActividadId.Value) && r.Fecha == hoy && EstadosReserva.Activas.Contains(r.Estado))
            .Select(r => new { r.Id, HorarioId = r.HorarioActividadId!.Value, r.UsuarioId, Nombre = r.Usuario!.Perfil!.Nombres + " " + r.Usuario.Perfil.Apellidos, r.Usuario.Perfil.AvatarUrl, r.Estado })
            .ToListAsync();
        var ahora = TimeOnly.FromDateTime(Reloj.Ahora);
        return horarios.OrderBy(h => Array.IndexOf(Dias.Largos, h.DiaSemana)).ThenBy(h => h.Inicio).Select(h =>
        {
            var deHoy = h.DiaSemana == diaHoy ? reservas.Where(r => r.HorarioId == h.Id).ToList() : [];
            return new ClaseAgendaDto(h.Id, h.Actividad!.Nombre, h.Actividad.Sucursal!.Nombre, h.DiaSemana, h.Inicio, h.Fin, h.CupoMaximo, deHoy.Count,
                h.DiaSemana == diaHoy && h.Fin <= ahora,
                deHoy.Select(r => new ParticipanteDto(r.Id, r.UsuarioId, r.Nombre, r.AvatarUrl, r.Estado)).ToList());
        }).ToList();
    }

    /// <summary>Asistencias de los últimos 30 días por clase del instructor (getAttendanceStats).</summary>
    public async Task<List<EstadisticaClaseDto>> EstadisticasAsistenciaAsync()
    {
        var desde = Reloj.Hoy.AddDays(-30);
        var filas = await db.Reservas.IgnoreQueryFilters()
            .Where(r => r.HorarioActividad!.InstructorId == actual.Id && r.Estado == EstadosReserva.Asistida && r.Fecha >= desde)
            .GroupBy(r => new { r.HorarioActividadId, r.HorarioActividad!.Actividad!.Nombre, r.HorarioActividad.DiaSemana, r.HorarioActividad.Inicio })
            .Select(g => new { g.Key.HorarioActividadId, g.Key.Nombre, g.Key.DiaSemana, g.Key.Inicio, Total = g.Count() })
            .ToListAsync();
        return filas.OrderBy(f => f.Inicio).Select(f => new EstadisticaClaseDto(f.HorarioActividadId!.Value, f.Nombre, f.DiaSemana, f.Inicio, f.Total)).ToList();
    }

    // ── Asesores y asesorías (CU-09, CU-10) ──────────────────────────────

    /// <summary>Entrenadores y nutricionistas activos; por defecto, de las sucursales del cliente.</summary>
    public async Task<List<AsesorDto>> CatalogoAsync(int? sucursalId = null)
    {
        var q = db.UsuarioRoles.IgnoreQueryFilters().Where(r => r.Rol!.Nivel == Niveles.Asesor && r.Usuario!.Activo);
        if (sucursalId is not null) q = q.Where(r => r.SucursalId == sucursalId);
        else if (actual.EsCliente)
        {
            var cadenas = await db.Membresias.IgnoreQueryFilters().Where(m => m.UsuarioId == actual.Id && m.Activa).Select(m => m.CadenaId).ToListAsync();
            if (cadenas.Count > 0) q = q.Where(r => r.CadenaId != null && cadenas.Contains(r.CadenaId.Value));
        }
        return await q.OrderBy(r => r.Rol!.Nombre).ThenBy(r => r.Usuario!.Perfil!.Apellidos)
            .Select(r => new AsesorDto(r.UsuarioId, r.Usuario!.Perfil!.Nombres + " " + r.Usuario.Perfil.Apellidos, r.Rol!.Nombre, r.SucursalId,
                r.Sucursal!.Nombre, r.Sucursal.Cadena!.Nombre, r.Usuario.Perfil.NivelExperiencia ?? "Profesional", r.Usuario.Perfil.AvatarUrl))
            .ToListAsync();
    }

    public async Task<AsesoriaDto> SolicitarAsesorAsync(int asesorId)
    {
        if (asesorId == actual.Id) throw ErrorNegocio.Solicitud("No puedes solicitarte a ti mismo como asesor.");
        var esAsesor = await db.UsuarioRoles.IgnoreQueryFilters().AnyAsync(r => r.UsuarioId == asesorId && r.Rol!.Nivel == Niveles.Asesor && r.Usuario!.Activo);
        if (!esAsesor) throw ErrorNegocio.NoEncontrado($"Asesor {asesorId} no encontrado.");
        if (await db.Asesorias.AnyAsync(a => a.ClienteId == actual.Id && (a.Estado == EstadosAsesoria.Pendiente || a.Estado == EstadosAsesoria.Activa)))
            throw ErrorNegocio.Conflicto("Ya tienes una solicitud pendiente o activa con otro asesor. Cancélala antes de solicitar uno nuevo.");

        var a = await db.Asesorias.FirstOrDefaultAsync(x => x.ClienteId == actual.Id && x.AsesorId == asesorId);
        if (a is null) db.Asesorias.Add(a = new Asesoria { ClienteId = actual.Id, AsesorId = asesorId });
        a.Estado = EstadosAsesoria.Pendiente;
        a.CreadaEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await notificaciones.NotificarAsync(asesorId, TiposNotificacion.Asesoria, "Nueva solicitud de asesoría", $"{actual.Nombre} quiere ser asesorado por ti.");
        return await AsesoriaAsync(a.Id);
    }

    public Task<List<AsesoriaDto>> MisSolicitudesAsync() => Asesorias(db.Asesorias.Where(a => a.ClienteId == actual.Id));
    public Task<List<AsesoriaDto>> PendientesAsync() => Asesorias(db.Asesorias.Where(a => a.AsesorId == actual.Id && a.Estado == EstadosAsesoria.Pendiente));
    public Task<List<AsesoriaDto>> ActivasAsync() => Asesorias(db.Asesorias.Where(a => a.AsesorId == actual.Id && a.Estado == EstadosAsesoria.Activa));

    public async Task<AsesoriaDto> AceptarAsync(int id)
    {
        var a = await db.Asesorias.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado($"Solicitud {id} no encontrada.");
        if (a.AsesorId != actual.Id) throw ErrorNegocio.Prohibido("Solo el asesor destinatario puede aceptar esta solicitud.");
        if (a.Estado != EstadosAsesoria.Pendiente) throw ErrorNegocio.Solicitud($"La solicitud ya está en estado {a.Estado}.");
        a.Estado = EstadosAsesoria.Activa;
        await db.SaveChangesAsync();
        await notificaciones.NotificarAsync(a.ClienteId, TiposNotificacion.Asesoria, "Solicitud aceptada", $"{actual.Nombre} aceptó tu solicitud de asesoría.");
        return await AsesoriaAsync(id);
    }

    public async Task<AsesoriaDto> RechazarAsync(int id)
    {
        var a = await db.Asesorias.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado($"Solicitud {id} no encontrada.");
        if (a.AsesorId != actual.Id) throw ErrorNegocio.Prohibido("No tienes permiso para rechazar esta solicitud.");
        if (a.Estado != EstadosAsesoria.Pendiente) throw ErrorNegocio.Solicitud($"La solicitud ya está en estado {a.Estado}.");
        a.Estado = EstadosAsesoria.Rechazada;
        await db.SaveChangesAsync();
        await notificaciones.NotificarAsync(a.ClienteId, TiposNotificacion.Asesoria, "Solicitud rechazada", $"{actual.Nombre} rechazó tu solicitud de asesoría.");
        return await AsesoriaAsync(id);
    }

    /// <summary>Cancela una asesoría activa o pendiente; al cancelar una activa se desactivan las rutinas del asesor y se vacía el plan de comidas.</summary>
    public async Task<AsesoriaDto> CancelarAsync(int id)
    {
        var a = await db.Asesorias.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado($"Asesoría {id} no encontrada.");
        if (a.ClienteId != actual.Id && a.AsesorId != actual.Id) throw ErrorNegocio.Prohibido("No tienes permiso para cancelar esta asesoría.");
        if (a.Estado is not (EstadosAsesoria.Activa or EstadosAsesoria.Pendiente)) throw ErrorNegocio.Solicitud("Solo se pueden cancelar asesorías activas o pendientes.");
        var eraActiva = a.Estado == EstadosAsesoria.Activa;
        a.Estado = EstadosAsesoria.Cancelada;
        if (eraActiva)
        {
            await db.PlanesNutricionales.Where(p => p.AsesorId == a.AsesorId && p.ClienteId == a.ClienteId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.PlanComidas, (string?)null));
            await db.Rutinas.Where(r => r.EntrenadorId == a.AsesorId && r.AsignadaAId == a.ClienteId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Activa, false));
        }
        await db.SaveChangesAsync();
        var otro = actual.Id == a.ClienteId ? a.AsesorId : a.ClienteId;
        await notificaciones.NotificarAsync(otro, TiposNotificacion.Asesoria, eraActiva ? "Asesoría cancelada" : "Solicitud retirada",
            $"{actual.Nombre} {(eraActiva ? "canceló la asesoría" : "retiró la solicitud")}.");
        return await AsesoriaAsync(id);
    }

    public async Task<AsesoriaDto> FijarMetaAsync(int id, int metaSesionesSemana)
    {
        if (metaSesionesSemana is < 1 or > 14) throw ErrorNegocio.Solicitud("La meta debe estar entre 1 y 14 sesiones por semana.");
        var a = await db.Asesorias.FirstOrDefaultAsync(x => x.Id == id && x.AsesorId == actual.Id && x.Estado == EstadosAsesoria.Activa)
                ?? throw ErrorNegocio.Prohibido("Asesoría no encontrada o sin acceso");
        a.MetaSesionesSemana = metaSesionesSemana;
        await db.SaveChangesAsync();
        return await AsesoriaAsync(id);
    }

    /// <summary>Actividad reciente de los alumnos activos del asesor (getAdviseeRecentActivity).</summary>
    public async Task<List<ActividadAlumnoDto>> ActividadAlumnosAsync()
    {
        var alumnos = await db.Asesorias.Where(a => a.AsesorId == actual.Id && a.Estado == EstadosAsesoria.Activa)
            .Select(a => new { a.Id, a.ClienteId, Nombre = a.Cliente!.Perfil!.Nombres + " " + a.Cliente.Perfil.Apellidos, a.MetaSesionesSemana }).ToListAsync();
        if (alumnos.Count == 0) return [];
        var ids = alumnos.Select(a => a.ClienteId).ToList();
        var haceSemana = DateTime.UtcNow.AddDays(-7);
        var stats = await db.Sesiones.Where(s => ids.Contains(s.UsuarioId) && s.Estado == EstadosSesion.Completada)
            .GroupBy(s => s.UsuarioId)
            .Select(g => new { UsuarioId = g.Key, Ultima = g.Max(s => s.Inicio), Total = g.Count(), Semana = g.Count(s => s.Inicio >= haceSemana) })
            .ToDictionaryAsync(x => x.UsuarioId);
        return alumnos.Select(a =>
        {
            var s = stats.GetValueOrDefault(a.ClienteId);
            var dias = s is null ? 999 : (int)(DateTime.UtcNow - s.Ultima).TotalDays;
            return new ActividadAlumnoDto(a.Id, a.ClienteId, a.Nombre, s?.Ultima, s?.Total ?? 0, s?.Semana ?? 0, a.MetaSesionesSemana, dias >= UmbralInactividad(a.MetaSesionesSemana));
        }).OrderByDescending(x => x.UltimaSesion ?? DateTime.MinValue).ToList();
    }

    public async Task<object> PanelAsesorAsync() => new
    {
        clientesActivos = await db.Asesorias.CountAsync(a => a.AsesorId == actual.Id && a.Estado == EstadosAsesoria.Activa),
        solicitudesPendientes = await db.Asesorias.CountAsync(a => a.AsesorId == actual.Id && a.Estado == EstadosAsesoria.Pendiente),
        citasHoy = await db.Citas.CountAsync(c => c.NutricionistaId == actual.Id && c.Fecha == Reloj.Hoy),
    };

    /// <summary>Días sin entrenar a partir de los cuales el alumno se considera inactivo (staff-inactivity.scheduler).</summary>
    public static int UmbralInactividad(int? metaSemanal) => metaSemanal is > 0 ? (int)Math.Round(7.0 / metaSemanal.Value) + 1 : 7;

    // ── Ficha del cliente y plan nutricional (CU-28) ─────────────────────

    public async Task<FichaClienteDto> FichaClienteAsync(int clienteId)
    {
        await datos.ExigirAsync(clienteId);
        var perfil = await db.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == clienteId) ?? throw ErrorNegocio.NoEncontrado("Perfil no encontrado.");
        var sesiones = await db.Sesiones.Where(s => s.UsuarioId == clienteId).OrderByDescending(s => s.Inicio).Take(5)
            .Select(s => new SesionResumenDto(s.Id, s.Inicio, s.Rutina != null ? s.Rutina.Nombre : (s.TipoDeporte ?? "Entrenamiento libre"), s.DuracionSeg, s.Series.Count))
            .ToListAsync();
        return new FichaClienteDto(clienteId, UsuariosService.APerfil(perfil), await entrenamiento.UltimaMetricaAsync(clienteId),
            await entrenamiento.RestriccionAsync(clienteId), await entrenamiento.PerfilAsync(clienteId), sesiones);
    }

    public async Task<PlanNutricionalDto> GuardarPlanAsync(int clienteId, PlanNutricionalForm f)
    {
        await datos.ExigirAsesorAsync(clienteId);
        var p = await db.PlanesNutricionales.FirstOrDefaultAsync(x => x.AsesorId == actual.Id && x.ClienteId == clienteId);
        if (p is null) db.PlanesNutricionales.Add(p = new PlanNutricional { AsesorId = actual.Id, ClienteId = clienteId });
        p.KcalDiarias = f.KcalDiarias;
        p.ProteinaG = f.ProteinaG;
        p.CarbohidratosG = f.CarbohidratosG;
        p.GrasaG = f.GrasaG;
        p.Notas = f.Notas;
        if (f.PlanComidas is not null) p.PlanComidas = JsonSerializer.Serialize(f.PlanComidas);
        p.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await notificaciones.NotificarAsync(clienteId, TiposNotificacion.Asesoria, "Plan nutricional actualizado", $"{actual.Nombre} actualizó tu plan nutricional.");
        return (await Planes(db.PlanesNutricionales.Where(x => x.Id == p.Id))).First();
    }

    public async Task<PlanNutricionalDto?> PlanDeClienteAsync(int clienteId)
    {
        await datos.ExigirAsesorAsync(clienteId);
        return (await Planes(db.PlanesNutricionales.Where(x => x.AsesorId == actual.Id && x.ClienteId == clienteId))).FirstOrDefault();
    }

    /// <summary>Plan nutricional vigente del cliente: el de su asesoría activa más reciente (getMyPlan).</summary>
    public async Task<PlanNutricionalDto?> MiPlanAsync()
    {
        var asesor = await db.Asesorias.Where(a => a.ClienteId == actual.Id && a.Estado == EstadosAsesoria.Activa)
            .OrderByDescending(a => a.CreadaEn).Select(a => (int?)a.AsesorId).FirstOrDefaultAsync();
        if (asesor is null) return null;
        return (await Planes(db.PlanesNutricionales.Where(x => x.AsesorId == asesor && x.ClienteId == actual.Id))).FirstOrDefault();
    }

    // ── Citas nutricionales (CU-29) ──────────────────────────────────────

    public async Task<List<CitaDto>> CitasAsync(bool soloHoy)
    {
        var q = db.Citas.Where(c => c.NutricionistaId == actual.Id);
        if (soloHoy) q = q.Where(c => c.Fecha == Reloj.Hoy);
        return await q.OrderByDescending(c => c.Fecha).ThenBy(c => c.Hora)
            .Select(c => new CitaDto(c.Id, c.PacienteId, c.Paciente!.Perfil!.Nombres + " " + c.Paciente.Perfil.Apellidos, c.Paciente.Perfil.AvatarUrl, c.Fecha, c.Hora, c.Tipo, c.Estado, c.Notas))
            .ToListAsync();
    }

    /// <summary>
    /// Agenda una cita con un asesorado activo. El proyecto base listaba citas pero no tenía forma
    /// de crearlas; se agrega lo mínimo para que el módulo funcione.
    /// </summary>
    public async Task<CitaDto> CrearCitaAsync(CitaForm f)
    {
        if (actual.Rol != Roles.Nutricionista && !actual.EsSuperAdmin) throw ErrorNegocio.Prohibido("Solo un nutricionista puede agendar citas.");
        await datos.ExigirAsesorAsync(f.PacienteId);
        if (f.Fecha < Reloj.Hoy) throw ErrorNegocio.Solicitud("No se puede agendar una cita en una fecha pasada.");
        if (await db.Citas.AnyAsync(c => c.NutricionistaId == actual.Id && c.Fecha == f.Fecha && c.Hora == f.Hora && c.Estado != "CANCELADA"))
            throw ErrorNegocio.Conflicto("Ya tienes una cita a esa hora.");
        var c = new CitaNutricional { NutricionistaId = actual.Id, PacienteId = f.PacienteId, Fecha = f.Fecha, Hora = f.Hora, Tipo = string.IsNullOrWhiteSpace(f.Tipo) ? "CONSULTA" : f.Tipo, Notas = f.Notas };
        db.Citas.Add(c);
        await db.SaveChangesAsync();
        await notificaciones.NotificarAsync(f.PacienteId, TiposNotificacion.Asesoria, "Cita agendada", $"Tienes una cita nutricional el {f.Fecha:dd/MM} a las {f.Hora:HH\\:mm}.");
        return (await CitasAsync(false)).First(x => x.Id == c.Id);
    }

    public async Task<CitaDto> CambiarEstadoCitaAsync(int id, string estado, string? notas)
    {
        if (!EstadosCita.Contains(estado)) throw ErrorNegocio.Solicitud("Estado de cita no válido.");
        var c = await db.Citas.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado($"Cita {id} no encontrada.");
        if (c.NutricionistaId != actual.Id) throw ErrorNegocio.Prohibido("No tiene permisos para modificar esta cita.");
        c.Estado = estado;
        if (notas is not null) c.Notas = notas;
        await db.SaveChangesAsync();
        return (await CitasAsync(false)).First(x => x.Id == id);
    }

    // ── Apoyo ────────────────────────────────────────────────────────────

    private async Task<AsesoriaDto> AsesoriaAsync(int id) => (await Asesorias(db.Asesorias.Where(a => a.Id == id))).First();

    private Task<List<AsesoriaDto>> Asesorias(IQueryable<Asesoria> q) =>
        q.OrderByDescending(a => a.CreadaEn).Select(a => new AsesoriaDto(
            a.Id, a.ClienteId, a.Cliente!.Perfil!.Nombres + " " + a.Cliente.Perfil.Apellidos, a.Cliente.Perfil.Telefono, a.Cliente.Perfil.Ci, a.Cliente.Email,
            a.AsesorId, a.Asesor!.Perfil!.Nombres + " " + a.Asesor.Perfil.Apellidos,
            a.Asesor.Roles.OrderByDescending(r => r.Rol!.Nivel).Select(r => r.Rol!.Nombre).FirstOrDefault() ?? "",
            a.Asesor.Roles.Where(r => r.Sucursal != null).Select(r => r.Sucursal!.Nombre).FirstOrDefault(),
            a.Estado, a.MetaSesionesSemana, a.CreadaEn)).ToListAsync();

    private async Task<List<PlanNutricionalDto>> Planes(IQueryable<PlanNutricional> q)
    {
        var planes = await q.ToListAsync();
        var ids = planes.SelectMany(p => new[] { p.AsesorId, p.ClienteId }).Distinct().ToList();
        var nombres = await db.Perfiles.Where(p => ids.Contains(p.UsuarioId))
            .ToDictionaryAsync(p => p.UsuarioId, p => p.Nombres + " " + p.Apellidos);
        return planes.Select(p => new PlanNutricionalDto(
            p.Id, p.AsesorId, nombres.GetValueOrDefault(p.AsesorId, ""), p.ClienteId, nombres.GetValueOrDefault(p.ClienteId, ""),
            p.KcalDiarias, p.ProteinaG, p.CarbohidratosG, p.GrasaG, p.Notas,
            string.IsNullOrEmpty(p.PlanComidas) ? [] : JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(p.PlanComidas) ?? [],
            p.ActualizadoEn)).ToList();
    }
}
