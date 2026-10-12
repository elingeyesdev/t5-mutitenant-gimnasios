using System.Data;
using System.Security.Cryptography;
using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record ReservaDto(
    int Id, int UsuarioId, string Usuario, string? Ci, int? HorarioActividadId, int? ActividadId, string Actividad,
    int? SucursalId, string Sucursal, string? Cadena, string? Direccion, string? Instructor, DateOnly Fecha, TimeOnly? Inicio, TimeOnly? Fin,
    string Estado, string? Codigo, DateTime CreadaEn, DateTime? CanceladaEn, bool EsPasada);

public record DisponibilidadDto(
    int HorarioId, int ActividadId, string Actividad, string? Descripcion, int SucursalId, string Sucursal, string DiaSemana, DateOnly Fecha,
    TimeOnly Inicio, TimeOnly Fin, string? Instructor, int CupoMaximo, int Reservados, int Disponibles, bool YaReservado, bool EnEspera);

public class ReservaForm
{
    /// <summary>Para personal administrativo: socio para quien se reserva (reserva presencial).</summary>
    public int? UsuarioId { get; set; }
    public int? HorarioActividadId { get; set; }
    /// <summary>Acceso libre: actividad sin horario fijo con rango de horas.</summary>
    public int? ActividadId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly? Inicio { get; set; }
    public TimeOnly? Fin { get; set; }
}

public record ResultadoIngreso(ReservaDto Reserva, string Mensaje);

/// <summary>Reservas de clases (CU-22, CU-23, CU-24).</summary>
public class ReservasService(
    AppDbContext db, UsuarioActual actual, Alcances alcances, SuscripcionesService suscripciones, NotificacionesService notificaciones)
{
    // ── Consultas ────────────────────────────────────────────────────────

    /// <summary>Horarios de una sucursal para una fecha con su cupo disponible (pantalla de reservar).</summary>
    public async Task<List<DisponibilidadDto>> DisponibilidadAsync(int sucursalId, DateOnly fecha, int? actividadId = null)
    {
        var dia = Dias.Largo(fecha.DayOfWeek);
        var q = db.HorariosActividad.IgnoreQueryFilters()
            .Where(h => h.DiaSemana == dia && h.Actividad!.SucursalId == sucursalId && h.Actividad.Activa && h.Actividad.Sucursal!.Activa);
        if (actividadId is not null) q = q.Where(h => h.ActividadId == actividadId);
        var horarios = await q.Include(h => h.Actividad).ThenInclude(a => a!.Sucursal).Include(h => h.Instructor).ThenInclude(i => i!.Perfil)
            .OrderBy(h => h.Inicio).ToListAsync();
        var ids = horarios.Select(h => h.Id).ToList();
        var ocupados = await db.Reservas.IgnoreQueryFilters()
            .Where(r => r.HorarioActividadId != null && ids.Contains(r.HorarioActividadId.Value) && r.Fecha == fecha && EstadosReserva.Activas.Contains(r.Estado))
            .GroupBy(r => r.HorarioActividadId!.Value).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var mias = await db.Reservas.IgnoreQueryFilters()
            .Where(r => r.UsuarioId == actual.Id && r.Fecha == fecha && r.HorarioActividadId != null && EstadosReserva.Activas.Contains(r.Estado))
            .Select(r => r.HorarioActividadId!.Value).ToListAsync();
        var espera = await db.ListaEspera.IgnoreQueryFilters()
            .Where(e => e.UsuarioId == actual.Id && ids.Contains(e.HorarioActividadId) && e.Estado == EstadosEspera.EnEspera && (e.Fecha == null || e.Fecha == fecha))
            .Select(e => e.HorarioActividadId).ToListAsync();
        return horarios.Select(h =>
        {
            var reservados = ocupados.GetValueOrDefault(h.Id);
            return new DisponibilidadDto(h.Id, h.ActividadId, h.Actividad!.Nombre, h.Actividad.Descripcion, h.Actividad.SucursalId, h.Actividad.Sucursal!.Nombre,
                h.DiaSemana, fecha, h.Inicio, h.Fin, h.Instructor?.Perfil?.NombreCompleto, h.CupoMaximo, reservados,
                Math.Max(0, h.CupoMaximo - reservados), mias.Contains(h.Id), espera.Contains(h.Id));
        }).ToList();
    }

    public async Task<List<ReservaDto>> MiasAsync(DateOnly? fecha = null)
    {
        var q = db.Reservas.IgnoreQueryFilters().Where(r => r.UsuarioId == actual.Id);
        if (fecha is not null) q = q.Where(r => r.Fecha == fecha);
        return (await Consulta(q).OrderByDescending(r => r.Fecha).ThenByDescending(r => r.Inicio).ToListAsync()).Select(ADto).ToList();
    }

    /// <summary>Reservas del alcance del personal administrativo (vista «Reservas» del panel).</summary>
    public async Task<Pagina<ReservaDto>> ListarAsync(DateOnly? desde = null, DateOnly? hasta = null, string? estado = null, int? sucursalId = null, int? usuarioId = null, string? buscar = null, int limite = 50, int salto = 0)
    {
        if (actual.Nivel == Niveles.Asesor) throw ErrorNegocio.Prohibido("Los entrenadores deben usar el endpoint de asistencia por clase.");
        var q = ReservasVisibles();
        if (desde is not null) q = q.Where(r => r.Fecha >= desde);
        if (hasta is not null) q = q.Where(r => r.Fecha <= hasta);
        if (!string.IsNullOrEmpty(estado)) q = q.Where(r => r.Estado == estado);
        if (sucursalId is not null) q = q.Where(r => r.SucursalId == sucursalId);
        if (usuarioId is not null) q = q.Where(r => r.UsuarioId == usuarioId);
        if (!string.IsNullOrWhiteSpace(buscar))
            q = q.Where(r => r.Usuario!.Email.Contains(buscar) || (r.Usuario.Perfil!.Nombres + " " + r.Usuario.Perfil.Apellidos).Contains(buscar) || r.Usuario.Perfil.Ci == buscar || r.CodigoQr == buscar.ToUpper());
        var total = await q.CountAsync();
        var lista = await Consulta(q).OrderByDescending(r => r.Fecha).ThenBy(r => r.Inicio).Skip(salto).Take(Math.Clamp(limite, 1, 500)).ToListAsync();
        return new Pagina<ReservaDto>(lista.Select(ADto).ToList(), total, limite, salto);
    }

    public async Task<ReservaDto> ObtenerAsync(int id)
    {
        var r = await Consulta(db.Reservas.IgnoreQueryFilters()).FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado($"Reserva {id} no encontrada");
        if (r.UsuarioId != actual.Id && !await EnAlcanceAsync(r)) throw ErrorNegocio.Prohibido("No tienes acceso a esta reserva.");
        return ADto(r);
    }

    /// <summary>Código que el socio muestra en recepción (equivale al QR de la reserva).</summary>
    public async Task<string> CodigoAsync(int id)
    {
        var r = await db.Reservas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado($"Reserva {id} no encontrada.");
        if (r.UsuarioId != actual.Id) throw ErrorNegocio.Prohibido("Solo puedes obtener el QR de tus propias reservas.");
        if (r.Estado != EstadosReserva.Confirmada) throw ErrorNegocio.Solicitud($"La reserva tiene estado '{r.Estado}'. Solo reservas CONFIRMADA generan QR.");
        return r.CodigoQr!;
    }

    // ── Crear (CU-22) ────────────────────────────────────────────────────

    public async Task<ReservaDto> CrearAsync(ReservaForm f)
    {
        if (actual.EsSuperAdmin || actual.Nivel == Niveles.Asesor) throw ErrorNegocio.Prohibido("Tu rol no permite crear reservas.");
        int usuarioId;
        if (actual.Nivel >= Niveles.Recepcionista)
        {
            usuarioId = f.UsuarioId ?? throw ErrorNegocio.Solicitud("Indica el socio para crear la reserva como personal.");
            if (!await db.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Activo)) throw ErrorNegocio.NoEncontrado($"Usuario {usuarioId} no encontrado.");
        }
        else
        {
            usuarioId = actual.Id;
        }
        if (f.Fecha < Reloj.Hoy) throw ErrorNegocio.Solicitud("No se puede reservar en una fecha pasada.");

        if (f.ActividadId is not null && f.HorarioActividadId is null)
            return await CrearAccesoLibreAsync(f, usuarioId);

        var horarioId = f.HorarioActividadId ?? throw ErrorNegocio.Solicitud("Proporciona el horario de la clase.");
        var horario = await db.HorariosActividad.IgnoreQueryFilters().Include(h => h.Actividad).FirstOrDefaultAsync(h => h.Id == horarioId)
                      ?? throw ErrorNegocio.NoEncontrado($"Horario {horarioId} no encontrado");
        if (!horario.Actividad!.Activa) throw ErrorNegocio.Solicitud("La actividad no está activa");
        if (horario.DiaSemana != Dias.Largo(f.Fecha.DayOfWeek)) throw ErrorNegocio.Solicitud($"La clase se dicta los {horario.DiaSemana}; elige una fecha de ese día.");
        if (f.Fecha == Reloj.Hoy && horario.Fin <= TimeOnly.FromDateTime(Reloj.Ahora)) throw ErrorNegocio.Solicitud("La clase de hoy ya terminó.");
        if (actual.Nivel >= Niveles.Recepcionista)
            await alcances.ExigirSucursalAsync(horario.Actividad.SucursalId, "No puede crear reservas en otra sucursal.");
        await ExigirSuscripcionAsync(usuarioId, horario.CadenaId);

        return await CrearConBloqueoAsync(horario, usuarioId, f.Fecha, EstadosReserva.Confirmada);
    }

    /// <summary>Reserva presencial del día ya marcada como asistida (walk-in del proyecto base).</summary>
    public async Task<ReservaDto> CrearPresencialAsync(int usuarioId, int horarioId)
    {
        var horario = await db.HorariosActividad.Include(h => h.Actividad).FirstOrDefaultAsync(h => h.Id == horarioId)
                      ?? throw ErrorNegocio.NoEncontrado($"Horario {horarioId} no encontrado.");
        await alcances.ExigirSucursalAsync(horario.Actividad!.SucursalId, "El horario pertenece a otra sucursal.");
        if (!await db.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Activo)) throw ErrorNegocio.NoEncontrado($"Usuario {usuarioId} no encontrado.");
        return await CrearConBloqueoAsync(horario, usuarioId, Reloj.Hoy, EstadosReserva.Asistida);
    }

    /// <summary>Bloquea el horario dentro de una transacción para que nunca se supere el cupo.</summary>
    private async Task<ReservaDto> CrearConBloqueoAsync(HorarioActividad horario, int usuarioId, DateOnly fecha, string estado)
    {
        var estrategia = db.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM HorariosActividad WITH (UPDLOCK, HOLDLOCK) WHERE Id = {horario.Id}");

            var duplicada = await db.Reservas.IgnoreQueryFilters().AnyAsync(r =>
                r.UsuarioId == usuarioId && r.HorarioActividadId == horario.Id && r.Fecha == fecha && EstadosReserva.Activas.Contains(r.Estado));
            if (duplicada) throw ErrorNegocio.Conflicto("Ya tienes una reserva activa para este horario y fecha");
            var ocupados = await db.Reservas.IgnoreQueryFilters().CountAsync(r =>
                r.HorarioActividadId == horario.Id && r.Fecha == fecha && EstadosReserva.Activas.Contains(r.Estado));
            if (ocupados >= horario.CupoMaximo) throw ErrorNegocio.Conflicto("Cupo agotado. Intenta con otro horario.");

            var reserva = new Reserva
            {
                CadenaId = horario.CadenaId, UsuarioId = usuarioId, HorarioActividadId = horario.Id, ActividadId = horario.ActividadId,
                SucursalId = horario.Actividad!.SucursalId, Fecha = fecha, Inicio = horario.Inicio, Fin = horario.Fin,
                Estado = estado, CreadaPorId = actual.Id, CodigoQr = await NuevoCodigoAsync(),
            };
            db.Reservas.Add(reserva);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return await ObtenerInternoAsync(reserva.Id);
        });
    }

    private async Task<ReservaDto> CrearAccesoLibreAsync(ReservaForm f, int usuarioId)
    {
        var a = await db.Actividades.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == f.ActividadId && x.Activa)
                ?? throw ErrorNegocio.NoEncontrado($"Actividad {f.ActividadId} no encontrada.");
        if (!a.AccesoLibre) throw ErrorNegocio.Solicitud("Esta clase requiere un horario (no es de acceso libre).");
        if (actual.Nivel >= Niveles.Recepcionista) await alcances.ExigirSucursalAsync(a.SucursalId, "La actividad pertenece a otra sucursal.");
        if (f.Inicio is null || f.Fin is null) throw ErrorNegocio.Solicitud("Acceso libre requiere hora de inicio y fin.");
        if (f.Fin <= f.Inicio) throw ErrorNegocio.Solicitud("La hora de fin debe ser posterior a la hora de inicio.");
        if (a.DuracionMin is > 0 && (f.Fin.Value - f.Inicio.Value).TotalMinutes > a.DuracionMin)
            throw ErrorNegocio.Solicitud("El tiempo seleccionado supera la duración máxima permitida para este servicio.");
        await ExigirSuscripcionAsync(usuarioId, a.CadenaId);

        var corto = Dias.Cortos[((int)f.Fecha.DayOfWeek + 6) % 7];
        var franjas = await db.HorariosSucursal.Where(h => h.SucursalId == a.SucursalId && !h.EsFeriado && h.DiaSemana == corto).ToListAsync();
        if (franjas.Count > 0 && !franjas.Any(h => h.Abre <= f.Inicio && h.Cierra >= f.Fin))
            throw ErrorNegocio.Solicitud($"El bloque {f.Inicio:HH\\:mm}–{f.Fin:HH\\:mm} está fuera del horario de apertura de la sede");

        var solapa = await db.Reservas.IgnoreQueryFilters().AnyAsync(r => r.UsuarioId == usuarioId && r.ActividadId == a.Id && r.Fecha == f.Fecha
            && EstadosReserva.Activas.Contains(r.Estado) && r.Inicio < f.Fin && r.Fin > f.Inicio);
        if (solapa) throw ErrorNegocio.Conflicto("Ya tienes una reserva en ese horario para esta actividad.");

        var reserva = new Reserva
        {
            CadenaId = a.CadenaId, UsuarioId = usuarioId, ActividadId = a.Id, SucursalId = a.SucursalId, Fecha = f.Fecha,
            Inicio = f.Inicio, Fin = f.Fin, Estado = EstadosReserva.Confirmada, CreadaPorId = actual.Id, CodigoQr = await NuevoCodigoAsync(),
        };
        db.Reservas.Add(reserva);
        await db.SaveChangesAsync();
        return await ObtenerInternoAsync(reserva.Id);
    }

    // ── Cancelar, confirmar e ingreso (CU-23, CU-24) ─────────────────────

    public async Task<ReservaDto> CancelarAsync(int id)
    {
        var r = await db.Reservas.IgnoreQueryFilters().Include(x => x.Actividad).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw ErrorNegocio.NoEncontrado($"Reserva {id} no encontrada");
        if (actual.Nivel < Niveles.Recepcionista)
        {
            if (r.UsuarioId != actual.Id) throw ErrorNegocio.Prohibido("No puedes cancelar reservas ajenas.");
        }
        else if (!await EnAlcanceAsync(r))
        {
            throw ErrorNegocio.Prohibido(await MensajeOtraSucursalAsync(r));
        }
        if (r.Estado is EstadosReserva.Cancelada or EstadosReserva.Asistida)
            throw ErrorNegocio.Solicitud($"La reserva ya se encuentra {r.Estado}");

        r.Estado = EstadosReserva.Cancelada;
        r.CanceladaEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await notificaciones.NotificarAsync(r.UsuarioId, TiposNotificacion.ReservaCancelada, "Reserva cancelada",
            $"Tu reserva para {r.Actividad?.Nombre ?? "la actividad"} del {r.Fecha:dd/MM} ha sido cancelada.");
        await AvisarListaEsperaAsync(r);
        return await ObtenerInternoAsync(id);
    }

    /// <summary>Confirma la asistencia de una reserva (confirm del proyecto base).</summary>
    public async Task<ReservaDto> ConfirmarAsync(int id)
    {
        var r = await db.Reservas.IgnoreQueryFilters().Include(x => x.Actividad).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw ErrorNegocio.NoEncontrado($"Reserva {id} no encontrada");
        if (!await EnAlcanceAsync(r)) throw ErrorNegocio.Prohibido(await MensajeOtraSucursalAsync(r));
        if (r.Estado is EstadosReserva.Asistida or EstadosReserva.Cancelada) throw ErrorNegocio.Solicitud($"La reserva ya se encuentra {r.Estado}");
        await MarcarAsistidaAsync(r, "MANUAL");
        return await ObtenerInternoAsync(id);
    }

    /// <summary>Registra el ingreso a la clase con el código de la reserva (checkInByToken).</summary>
    public async Task<ResultadoIngreso> IngresoPorCodigoAsync(string codigo, bool forzar = false)
    {
        var limpio = (codigo ?? "").Trim().ToUpperInvariant();
        var r = await db.Reservas.IgnoreQueryFilters().Include(x => x.Actividad).Include(x => x.Usuario).ThenInclude(u => u!.Perfil)
                    .FirstOrDefaultAsync(x => x.CodigoQr == limpio)
                ?? throw ErrorNegocio.Solicitud("Este código QR no corresponde a una reserva.");
        if (!await EnAlcanceAsync(r)) throw ErrorNegocio.Prohibido(await MensajeOtraSucursalAsync(r));

        var cliente = r.Usuario?.Perfil?.NombreCompleto ?? $"Usuario #{r.UsuarioId}";
        var rango = r.Inicio is not null && r.Fin is not null ? $" ({r.Inicio:HH\\:mm} - {r.Fin:HH\\:mm})" : "";
        var hoy = Reloj.Hoy;
        if (r.Fecha < hoy) throw ErrorNegocio.Solicitud($"Esta reserva era para el {r.Fecha:dd/MM/yyyy}{rango} y ya expiró. No se puede hacer check-in de reservas pasadas.");
        if (r.Fecha > hoy && !forzar) throw ErrorNegocio.Conflicto($"Esta reserva es para el {r.Fecha:dd/MM/yyyy}{rango}.");
        if (r.Estado != EstadosReserva.Confirmada)
            throw ErrorNegocio.Solicitud(r.Estado switch
            {
                EstadosReserva.Asistida => $"El cliente {cliente} ya completó el check-in de esta reserva.",
                EstadosReserva.Cancelada => $"Esta reserva fue cancelada por {cliente}.",
                _ => $"Esta reserva tiene estado \"{r.Estado}\" y no se puede procesar.",
            });
        await MarcarAsistidaAsync(r, "QR");
        return new ResultadoIngreso(await ObtenerInternoAsync(r.Id), $"Ingreso registrado: {cliente} — {r.Actividad?.Nombre ?? "clase"}{rango}.");
    }

    /// <summary>Marca reservas como asistidas o no asistidas en bloque.</summary>
    public async Task<int> EstadoEnBloqueAsync(IReadOnlyList<int> ids, string estado)
    {
        if (estado is not (EstadosReserva.Asistida or EstadosReserva.NoAsistio)) throw ErrorNegocio.Solicitud("Estado no válido: usa ASISTIDA o NO_ASISTIO.");
        var visibles = ReservasVisibles().Where(r => ids.Contains(r.Id));
        return await visibles.ExecuteUpdateAsync(s => s.SetProperty(r => r.Estado, estado));
    }

    // ── Apoyo ────────────────────────────────────────────────────────────

    private async Task MarcarAsistidaAsync(Reserva r, string metodo)
    {
        r.Estado = EstadosReserva.Asistida;
        if (r.SucursalId is int sucursal && !await db.Accesos.IgnoreQueryFilters().AnyAsync(a => a.UsuarioId == r.UsuarioId && a.SucursalId == sucursal && a.Estado == EstadosAcceso.Activo))
            db.Accesos.Add(new Acceso { CadenaId = r.CadenaId, UsuarioId = r.UsuarioId, SucursalId = sucursal, Metodo = metodo, RegistradoPorId = actual.Id });
        await db.SaveChangesAsync();
        await notificaciones.NotificarAsync(r.UsuarioId, TiposNotificacion.IngresoRegistrado, "Ingreso registrado",
            $"Tu ingreso a {r.Actividad?.Nombre ?? "tu clase"} fue registrado. Disfruta tu entrenamiento.");
    }

    /// <summary>Avisa al primero de la lista de espera que se liberó un cupo (la asignación la hace el personal).</summary>
    private async Task AvisarListaEsperaAsync(Reserva r)
    {
        if (r.HorarioActividadId is null) return;
        var primero = await db.ListaEspera.IgnoreQueryFilters()
            .Where(e => e.HorarioActividadId == r.HorarioActividadId && e.Estado == EstadosEspera.EnEspera && (e.Fecha == null || e.Fecha == r.Fecha))
            .OrderBy(e => e.Posicion).FirstOrDefaultAsync();
        if (primero is null) return;
        primero.Estado = EstadosEspera.Notificada;
        primero.NotificadaEn = DateTime.UtcNow;
        await notificaciones.NotificarAsync(primero.UsuarioId, TiposNotificacion.CupoLiberado, "Se liberó un cupo",
            $"Se liberó un lugar en {r.Actividad?.Nombre ?? "la clase"} del {r.Fecha:dd/MM}. Acércate a recepción para confirmarlo.", guardar: false);
        await db.SaveChangesAsync();
    }

    private async Task ExigirSuscripcionAsync(int usuarioId, int cadenaId)
    {
        // Corrección RT-04: en el proyecto base esta regla solo la validaba la app.
        if (!await suscripciones.TieneActivaAsync(usuarioId, cadenaId))
            throw ErrorNegocio.Prohibido("Se requiere una suscripción activa en esta cadena para reservar.");
    }

    private IQueryable<Reserva> ReservasVisibles()
    {
        if (actual.EsSuperAdmin) return db.Reservas;
        if (actual.Nivel < Niveles.Recepcionista) return db.Reservas.IgnoreQueryFilters().Where(r => r.UsuarioId == actual.Id);
        var q = db.Reservas.AsQueryable();
        if (actual.SucursalFiltro is int sucursal) q = q.Where(r => r.SucursalId == sucursal);
        return q;
    }

    private Task<bool> EnAlcanceAsync(Reserva r) => Task.FromResult(EnAlcance(r));

    private bool EnAlcance(Reserva r)
    {
        if (actual.EsSuperAdmin) return true;
        if (actual.Nivel < Niveles.Recepcionista || r.CadenaId != actual.CadenaId) return false;
        return actual.SucursalFiltro is null || r.SucursalId == actual.SucursalFiltro;
    }

    private async Task<string> MensajeOtraSucursalAsync(Reserva r)
    {
        var sucursal = await db.Sucursales.IgnoreQueryFilters().Include(s => s.Cadena).FirstOrDefaultAsync(s => s.Id == r.SucursalId);
        var lugar = sucursal is null ? "otra sucursal" : $"\"{sucursal.Nombre}\" (marca {sucursal.Cadena?.Nombre})";
        return actual.SucursalFiltro is null
            ? $"Esta reserva pertenece a la sucursal {lugar} y solo puedes registrar ingresos de tus propias sucursales."
            : $"Esta reserva pertenece a la sucursal {lugar} y solo puedes registrar ingresos de tu propia sucursal.";
    }

    /// <summary>Código corto (8 caracteres sin letras ambiguas) para dictarlo o teclearlo en recepción.</summary>
    private async Task<string> NuevoCodigoAsync()
    {
        const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var codigo = string.Create(8, 0, (span, _) => { for (var i = 0; i < span.Length; i++) span[i] = alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)]; });
            if (!await db.Reservas.IgnoreQueryFilters().AnyAsync(r => r.CodigoQr == codigo)) return codigo;
        }
    }

    public async Task<ReservaDto> ObtenerInternoAsync(int id) =>
        ADto(await Consulta(db.Reservas.IgnoreQueryFilters()).FirstAsync(x => x.Id == id));

    private static IQueryable<Reserva> Consulta(IQueryable<Reserva> q) =>
        q.Include(r => r.Usuario).ThenInclude(u => u!.Perfil)
            .Include(r => r.Actividad)
            .Include(r => r.Sucursal).ThenInclude(s => s!.Cadena)
            .Include(r => r.Sucursal).ThenInclude(s => s!.Ubicacion)
            .Include(r => r.HorarioActividad).ThenInclude(h => h!.Instructor).ThenInclude(i => i!.Perfil)
            .AsSplitQuery()
            .IgnoreQueryFilters();

    public static ReservaDto ADto(Reserva r)
    {
        var pasada = r.Fecha < Reloj.Hoy || (r.Fecha == Reloj.Hoy && r.Fin is not null && r.Fin <= TimeOnly.FromDateTime(Reloj.Ahora));
        return new ReservaDto(
            r.Id, r.UsuarioId, r.Usuario?.Perfil?.NombreCompleto ?? r.Usuario?.Email ?? "", r.Usuario?.Perfil?.Ci, r.HorarioActividadId, r.ActividadId,
            r.Actividad?.Nombre ?? "Actividad", r.SucursalId, r.Sucursal?.Nombre ?? "", r.Sucursal?.Cadena?.Nombre, r.Sucursal?.Ubicacion?.Direccion,
            r.HorarioActividad?.Instructor?.Perfil?.NombreCompleto, r.Fecha, r.Inicio, r.Fin, r.Estado,
            r.Estado == EstadosReserva.Confirmada ? r.CodigoQr : null, r.CreadaEn, r.CanceladaEn, pasada);
    }
}
