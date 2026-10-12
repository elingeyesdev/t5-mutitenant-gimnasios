using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers.Api;

public record TurnosRequest(int SucursalId, List<TurnoForm> Turnos);
public record SolicitudAsesorRequest(int AsesorId);
public record MetaRequest(int MetaSesionesSemana);
public record ConversacionRequest(int UsuarioId);
public record MensajeRequest(string Contenido);
public record VisitaRequest(int SucursalId, DateTime Entrada, DateTime? Salida);

[Route("api/personal")]
public class PersonalApi(PersonalService personal, UsuarioActual actual) : ApiBase
{
    [HttpGet("mis-turnos"), Authorize(Politicas.Staff)]
    public Task<List<TurnoDto>> MisTurnos() => personal.TurnosAsync(actual.Id);

    [HttpGet("{usuarioId:int}/turnos"), Authorize(Politicas.Admin)]
    public Task<List<TurnoDto>> Turnos(int usuarioId) => personal.TurnosAsync(usuarioId);

    [HttpPut("{usuarioId:int}/turnos"), Authorize(Politicas.Admin)]
    public Task<List<TurnoDto>> AsignarTurnos(int usuarioId, TurnosRequest r) => personal.AsignarTurnosAsync(usuarioId, r.SucursalId, r.Turnos);

    [HttpGet("agenda"), Authorize(Politicas.Staff)]
    public Task<List<ClaseAgendaDto>> Agenda(bool soloHoy = true) => personal.AgendaAsync(soloHoy);

    [HttpGet("estadisticas-asistencia"), Authorize(Politicas.Staff)]
    public Task<List<EstadisticaClaseDto>> EstadisticasAsistencia() => personal.EstadisticasAsistenciaAsync();

    [HttpGet("catalogo")]
    public Task<List<AsesorDto>> Catalogo(int? sucursalId) => personal.CatalogoAsync(sucursalId);

    [HttpPost("asesorias")]
    public Task<AsesoriaDto> Solicitar(SolicitudAsesorRequest r) => personal.SolicitarAsesorAsync(r.AsesorId);

    [HttpGet("asesorias/mias")]
    public Task<List<AsesoriaDto>> MisSolicitudes() => personal.MisSolicitudesAsync();

    [HttpGet("asesorias/pendientes"), Authorize(Politicas.Asesor)]
    public Task<List<AsesoriaDto>> Pendientes() => personal.PendientesAsync();

    [HttpGet("asesorias/activas"), Authorize(Politicas.Asesor)]
    public Task<List<AsesoriaDto>> Activas() => personal.ActivasAsync();

    [HttpGet("asesorias/actividad"), Authorize(Politicas.Asesor)]
    public Task<List<ActividadAlumnoDto>> Actividad() => personal.ActividadAlumnosAsync();

    [HttpPut("asesorias/{id:int}/aceptar"), Authorize(Politicas.Asesor)]
    public Task<AsesoriaDto> Aceptar(int id) => personal.AceptarAsync(id);

    [HttpPut("asesorias/{id:int}/rechazar"), Authorize(Politicas.Asesor)]
    public Task<AsesoriaDto> Rechazar(int id) => personal.RechazarAsync(id);

    [HttpPut("asesorias/{id:int}/cancelar")]
    public Task<AsesoriaDto> Cancelar(int id) => personal.CancelarAsync(id);

    [HttpPut("asesorias/{id:int}/meta"), Authorize(Politicas.Asesor)]
    public Task<AsesoriaDto> Meta(int id, MetaRequest r) => personal.FijarMetaAsync(id, r.MetaSesionesSemana);

    [HttpGet("panel"), Authorize(Politicas.Asesor)]
    public Task<object> Panel() => personal.PanelAsesorAsync();

    [HttpGet("clientes/{clienteId:int}/ficha"), Authorize(Politicas.Staff)]
    public Task<FichaClienteDto> Ficha(int clienteId) => personal.FichaClienteAsync(clienteId);

    [HttpGet("clientes/{clienteId:int}/plan"), Authorize(Politicas.Asesor)]
    public Task<PlanNutricionalDto?> Plan(int clienteId) => personal.PlanDeClienteAsync(clienteId);

    [HttpPut("clientes/{clienteId:int}/plan"), Authorize(Politicas.Asesor)]
    public Task<PlanNutricionalDto> GuardarPlan(int clienteId, PlanNutricionalForm f) => personal.GuardarPlanAsync(clienteId, f);

    [HttpGet("mi-plan")]
    public Task<PlanNutricionalDto?> MiPlan() => personal.MiPlanAsync();

    [HttpGet("citas"), Authorize(Politicas.Asesor)]
    public Task<List<CitaDto>> Citas(bool soloHoy = false) => personal.CitasAsync(soloHoy);

    [HttpPost("citas"), Authorize(Politicas.Asesor)]
    public Task<CitaDto> CrearCita(CitaForm f) => personal.CrearCitaAsync(f);

    [HttpPut("citas/{id:int}/estado"), Authorize(Politicas.Asesor)]
    public Task<CitaDto> EstadoCita(int id, EstadoRequest r) => personal.CambiarEstadoCitaAsync(id, r.Estado, r.Notas);
}

[Route("api/notificaciones")]
public class NotificacionesApi(NotificacionesService notificaciones) : ApiBase
{
    [HttpGet]
    public Task<List<NotificacionDto>> Mias(int limite = 50) => notificaciones.MiasAsync(limite);

    [HttpGet("sin-leer")]
    public async Task<object> SinLeer() => new { cantidad = await notificaciones.SinLeerAsync() };

    [HttpPut("{id:int}/leida")]
    public async Task<NoContentResult> Leida(int id)
    {
        await notificaciones.MarcarLeidaAsync(id);
        return NoContent();
    }

    [HttpPut("leidas")]
    public async Task<NoContentResult> TodasLeidas()
    {
        await notificaciones.MarcarTodasLeidasAsync();
        return NoContent();
    }

    [HttpGet("preferencias")]
    public Task<PreferenciasDto> Preferencias() => notificaciones.PreferenciasAsync();

    [HttpPut("preferencias")]
    public Task<PreferenciasDto> GuardarPreferencias(PreferenciasDto f) => notificaciones.GuardarPreferenciasAsync(f);

    [HttpGet("plantillas"), Authorize(Politicas.Admin)]
    public Task<List<PlantillaDto>> Plantillas() => notificaciones.PlantillasAsync();

    [HttpPost("plantillas"), Authorize(Politicas.SuperAdmin)]
    public Task<PlantillaDto> GuardarPlantilla(PlantillaDto f) => notificaciones.GuardarPlantillaAsync(f);

    [HttpPost("avisos"), Authorize(Politicas.Admin)]
    public async Task<object> EnviarAviso(AvisoForm f) => new { destinatarios = await notificaciones.EnviarAvisoAsync(f) };
}

[Route("api/reportes"), Authorize(Politicas.Admin)]
public class ReportesApi(ReportesService reportes) : ApiBase
{
    [HttpGet("resumen")]
    public Task<ResumenPanel> Resumen(string periodo = "week") => reportes.ResumenAsync(periodo);

    [HttpGet("tipos")]
    public TipoReporte[] Tipos() => ReportesService.Tipos;

    [HttpGet("{tipo}"), Authorize(Politicas.Gerente)]
    public Task<Reporte> Generar(string tipo, [FromQuery] FiltroReporte f) => reportes.GenerarAsync(tipo, f);
}

[Route("api/mensajes")]
public class MensajesApi(MensajesService mensajes) : ApiBase
{
    [HttpGet("conversaciones")]
    public Task<List<ConversacionDto>> Conversaciones() => mensajes.ConversacionesAsync();

    [HttpPost("conversaciones")]
    public Task<ConversacionDto> Abrir(ConversacionRequest r) => mensajes.AbrirAsync(r.UsuarioId);

    [HttpGet("conversaciones/{id:int}")]
    public Task<List<MensajeDto>> Historial(int id, int? despuesDe) => mensajes.HistorialAsync(id, despuesDe);

    [HttpPost("conversaciones/{id:int}/mensajes")]
    public Task<MensajeDto> Enviar(int id, MensajeRequest r) => mensajes.EnviarAsync(id, r.Contenido);

    [HttpPut("conversaciones/{id:int}/leida")]
    public async Task<NoContentResult> Leida(int id)
    {
        await mensajes.MarcarLeidaAsync(id);
        return NoContent();
    }

    [HttpDelete("conversaciones/{id:int}/mensajes")]
    public async Task<NoContentResult> Vaciar(int id)
    {
        await mensajes.VaciarAsync(id);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public Task<MensajeDto> EliminarMensaje(int id, bool paraTodos = false) => mensajes.EliminarMensajeAsync(id, paraTodos);
}

[Route("api/visitas")]
public class VisitasApi(VisitasService visitas) : ApiBase
{
    [HttpPost]
    public Task<VisitaDto> Registrar(VisitaRequest r) => visitas.RegistrarAsync(r.SucursalId, r.Entrada, r.Salida);

    [HttpGet("mias")]
    public Task<List<VisitaDto>> Mias() => visitas.MiasAsync();
}
