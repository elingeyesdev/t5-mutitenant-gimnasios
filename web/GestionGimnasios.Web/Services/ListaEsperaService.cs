using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record EntradaEsperaDto(
    int Id, int UsuarioId, string Usuario, int HorarioActividadId, string Actividad, string Sucursal, string DiaSemana,
    TimeOnly Inicio, DateOnly? Fecha, int Posicion, string Estado, DateTime CreadaEn, DateTime? AsignadaEn, int? ReservaId);

/// <summary>Lista de espera por horario, en orden de llegada (CU-25). La asignación es manual, como en el proyecto base.</summary>
public class ListaEsperaService(AppDbContext db, UsuarioActual actual, Alcances alcances, ReservasService reservas, NotificacionesService notificaciones)
{
    public async Task<EntradaEsperaDto> InscribirseAsync(int horarioId, DateOnly fecha)
    {
        var h = await db.HorariosActividad.IgnoreQueryFilters().Include(x => x.Actividad).FirstOrDefaultAsync(x => x.Id == horarioId)
                ?? throw ErrorNegocio.NoEncontrado($"Horario {horarioId} no encontrado");
        if (fecha < Reloj.Hoy) throw ErrorNegocio.Solicitud("No puedes anotarte en una fecha pasada.");
        if (h.DiaSemana != Dias.Largo(fecha.DayOfWeek)) throw ErrorNegocio.Solicitud($"La clase se dicta los {h.DiaSemana}.");
        var ocupados = await db.Reservas.IgnoreQueryFilters().CountAsync(r => r.HorarioActividadId == horarioId && r.Fecha == fecha && EstadosReserva.Activas.Contains(r.Estado));
        if (ocupados < h.CupoMaximo) throw ErrorNegocio.Solicitud("Todavía hay cupo en este horario: puedes reservar directamente.");
        if (await db.ListaEspera.IgnoreQueryFilters().AnyAsync(e => e.UsuarioId == actual.Id && e.HorarioActividadId == horarioId && e.Fecha == fecha
                && (e.Estado == EstadosEspera.EnEspera || e.Estado == EstadosEspera.Notificada)))
            throw ErrorNegocio.Conflicto("Ya estás en la lista de espera de este horario.");

        var posicion = 1 + await db.ListaEspera.IgnoreQueryFilters().Where(e => e.HorarioActividadId == horarioId && e.Fecha == fecha).CountAsync();
        var e = new EntradaListaEspera { CadenaId = h.CadenaId, UsuarioId = actual.Id, HorarioActividadId = horarioId, Fecha = fecha, Posicion = posicion };
        db.ListaEspera.Add(e);
        await db.SaveChangesAsync();
        return (await Consulta(db.ListaEspera.IgnoreQueryFilters().Where(x => x.Id == e.Id))).First();
    }

    public async Task<List<EntradaEsperaDto>> MiasAsync() =>
        await Consulta(db.ListaEspera.IgnoreQueryFilters().Where(e => e.UsuarioId == actual.Id && (e.Fecha == null || e.Fecha >= Reloj.Hoy)));

    public async Task<List<EntradaEsperaDto>> DeHorarioAsync(int horarioId, DateOnly? fecha = null)
    {
        var h = await db.HorariosActividad.Include(x => x.Actividad).FirstOrDefaultAsync(x => x.Id == horarioId)
                ?? throw ErrorNegocio.NoEncontrado($"Horario {horarioId} no encontrado");
        await alcances.ExigirSucursalAsync(h.Actividad!.SucursalId);
        var q = db.ListaEspera.Where(e => e.HorarioActividadId == horarioId);
        if (fecha is not null) q = q.Where(e => e.Fecha == fecha);
        return await Consulta(q);
    }

    /// <summary>Entradas pendientes del alcance del personal (vista de lista de espera del panel).</summary>
    public async Task<List<EntradaEsperaDto>> PendientesAsync()
    {
        var visibles = alcances.SucursalesGestionables().Select(s => s.Id);
        return await Consulta(db.ListaEspera.Where(e => (e.Estado == EstadosEspera.EnEspera || e.Estado == EstadosEspera.Notificada)
                                                       && visibles.Contains(e.HorarioActividad!.Actividad!.SucursalId) && (e.Fecha == null || e.Fecha >= Reloj.Hoy)));
    }

    /// <summary>El personal asigna el cupo liberado: crea la reserva y marca la entrada como asignada.</summary>
    public async Task<EntradaEsperaDto> AsignarAsync(int entradaId)
    {
        var e = await db.ListaEspera.Include(x => x.HorarioActividad).ThenInclude(h => h!.Actividad).FirstOrDefaultAsync(x => x.Id == entradaId)
                ?? throw ErrorNegocio.NoEncontrado("Entrada de lista de espera no encontrada.");
        await alcances.ExigirSucursalAsync(e.HorarioActividad!.Actividad!.SucursalId);
        if (e.Estado is not (EstadosEspera.EnEspera or EstadosEspera.Notificada)) throw ErrorNegocio.Solicitud($"La entrada ya está {e.Estado}.");
        var adelante = await db.ListaEspera.AnyAsync(x => x.HorarioActividadId == e.HorarioActividadId && x.Fecha == e.Fecha && x.Posicion < e.Posicion
                                                           && (x.Estado == EstadosEspera.EnEspera || x.Estado == EstadosEspera.Notificada));
        if (adelante) throw ErrorNegocio.Conflicto("Hay socios antes en la lista: se respeta el orden de llegada.");

        var reserva = await reservas.CrearAsync(new ReservaForm { UsuarioId = e.UsuarioId, HorarioActividadId = e.HorarioActividadId, Fecha = e.Fecha ?? Reloj.Hoy });
        e.Estado = EstadosEspera.Asignada;
        e.AsignadaEn = DateTime.UtcNow;
        e.ReservaId = reserva.Id;
        await db.SaveChangesAsync();
        await notificaciones.NotificarAsync(e.UsuarioId, TiposNotificacion.ReservaConfirmada, "Tienes cupo",
            $"Se te asignó un lugar en {e.HorarioActividad.Actividad.Nombre} del {reserva.Fecha:dd/MM} a las {reserva.Inicio:HH\\:mm}.");
        return (await Consulta(db.ListaEspera.Where(x => x.Id == e.Id))).First();
    }

    public async Task SalirAsync(int entradaId)
    {
        var e = await db.ListaEspera.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == entradaId)
                ?? throw ErrorNegocio.NoEncontrado("Entrada de lista de espera no encontrada.");
        if (e.UsuarioId != actual.Id)
        {
            var sucursal = await db.HorariosActividad.IgnoreQueryFilters().Where(h => h.Id == e.HorarioActividadId).Select(h => h.Actividad!.SucursalId).FirstAsync();
            await alcances.ExigirSucursalAsync(sucursal);
        }
        e.Estado = EstadosEspera.Cancelada;
        await db.SaveChangesAsync();
    }

    private static async Task<List<EntradaEsperaDto>> Consulta(IQueryable<EntradaListaEspera> q) =>
        await q.OrderBy(e => e.Fecha).ThenBy(e => e.Posicion)
            .Select(e => new EntradaEsperaDto(
                e.Id, e.UsuarioId, e.Usuario!.Perfil!.Nombres + " " + e.Usuario.Perfil.Apellidos, e.HorarioActividadId,
                e.HorarioActividad!.Actividad!.Nombre, e.HorarioActividad.Actividad.Sucursal!.Nombre, e.HorarioActividad.DiaSemana,
                e.HorarioActividad.Inicio, e.Fecha, e.Posicion, e.Estado, e.CreadaEn, e.AsignadaEn, e.ReservaId))
            .ToListAsync();
}
