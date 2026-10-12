using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers.Api;

public record EstadoRequest(string Estado, string? Notas = null);
public record PresencialRequest(int UsuarioId, int HorarioActividadId);
public record IngresoRequest(string Codigo, bool Forzar = false);
public record BloqueRequest(List<int> Ids, string Estado);
public record EsperaRequest(int HorarioActividadId, DateOnly Fecha);
public record CodigoRequest(string Codigo);

[Route("api/suscripciones")]
public class SuscripcionesApi(SuscripcionesService suscripciones, UsuarioActual actual) : ApiBase
{
    [HttpGet("planes")]
    public Task<List<PlanDto>> Planes(int? cadenaId, bool incluirInactivos = false) => suscripciones.PlanesAsync(cadenaId, incluirInactivos);

    /// <summary>Corrección RT-04: en el proyecto base cualquier usuario autenticado podía crear planes.</summary>
    [HttpPost("planes"), Authorize(Politicas.Gerente)]
    public Task<PlanDto> CrearPlan(PlanForm f) => suscripciones.CrearPlanAsync(f);

    [HttpPut("planes/{id:int}"), Authorize(Politicas.Gerente)]
    public Task<PlanDto> ActualizarPlan(int id, PlanForm f) => suscripciones.ActualizarPlanAsync(id, f);

    [HttpDelete("planes/{id:int}"), Authorize(Politicas.Gerente)]
    public async Task<NoContentResult> EliminarPlan(int id)
    {
        await suscripciones.EliminarPlanAsync(id);
        return NoContent();
    }

    [HttpGet, Authorize(Politicas.Admin)]
    public Task<List<SuscripcionDto>> Listar(string? estado, int? sucursalId) => suscripciones.ListarAsync(estado, sucursalId);

    [HttpGet("mias")]
    public Task<List<SuscripcionDto>> Mias() => suscripciones.DeUsuarioAsync(actual.Id);

    [HttpGet("activa")]
    public Task<SuscripcionDto?> Activa() => suscripciones.ActivaAsync(actual.Id);

    [HttpGet("usuario/{usuarioId:int}")]
    public Task<List<SuscripcionDto>> DeUsuario(int usuarioId) => suscripciones.DeUsuarioAsync(usuarioId);

    [HttpGet("{id:int}")]
    public Task<SuscripcionDto> Obtener(int id) => suscripciones.ObtenerAsync(id);

    [HttpPost, Authorize(Politicas.Admin)]
    public Task<SuscripcionDto> Crear(SuscripcionForm f) => suscripciones.CrearAsync(f);

    [HttpPut("{id:int}/estado"), Authorize(Politicas.Admin)]
    public Task<SuscripcionDto> CambiarEstado(int id, EstadoRequest r) => suscripciones.CambiarEstadoAsync(id, r.Estado);

    [HttpPost("{id:int}/pagos"), Authorize(Politicas.Admin)]
    public Task<PagoDto> RegistrarPago(int id, PagoForm f) => suscripciones.RegistrarPagoAsync(id, f);

    [HttpGet("{id:int}/pagos")]
    public Task<List<PagoDto>> Pagos(int id) => suscripciones.PagosAsync(id);
}

[Route("api/reservas")]
public class ReservasApi(ReservasService reservas) : ApiBase
{
    [HttpGet("disponibilidad")]
    public Task<List<DisponibilidadDto>> Disponibilidad(int sucursalId, DateOnly fecha, int? actividadId) => reservas.DisponibilidadAsync(sucursalId, fecha, actividadId);

    [HttpGet("mias")]
    public Task<List<ReservaDto>> Mias(DateOnly? fecha) => reservas.MiasAsync(fecha);

    [HttpGet, Authorize(Politicas.Admin)]
    public Task<Pagina<ReservaDto>> Listar(DateOnly? desde, DateOnly? hasta, string? estado, int? sucursalId, int? usuarioId, string? buscar, int limite = 50, int salto = 0) =>
        reservas.ListarAsync(desde, hasta, estado, sucursalId, usuarioId, buscar, limite, salto);

    [HttpGet("{id:int}")]
    public Task<ReservaDto> Obtener(int id) => reservas.ObtenerAsync(id);

    [HttpGet("{id:int}/codigo")]
    public async Task<object> Codigo(int id) => new { codigo = await reservas.CodigoAsync(id) };

    [HttpPost]
    public Task<ReservaDto> Crear(ReservaForm f) => reservas.CrearAsync(f);

    [HttpPost("presencial"), Authorize(Politicas.Admin)]
    public Task<ReservaDto> Presencial(PresencialRequest r) => reservas.CrearPresencialAsync(r.UsuarioId, r.HorarioActividadId);

    [HttpPut("{id:int}/cancelar")]
    public Task<ReservaDto> Cancelar(int id) => reservas.CancelarAsync(id);

    [HttpPut("{id:int}/confirmar"), Authorize(Politicas.Escaner)]
    public Task<ReservaDto> Confirmar(int id) => reservas.ConfirmarAsync(id);

    [HttpPost("ingreso"), Authorize(Politicas.Escaner)]
    public Task<ResultadoIngreso> Ingreso(IngresoRequest r) => reservas.IngresoPorCodigoAsync(r.Codigo, r.Forzar);

    [HttpPatch("estado-bloque"), Authorize(Politicas.Admin)]
    public async Task<object> EstadoEnBloque(BloqueRequest r) => new { actualizadas = await reservas.EstadoEnBloqueAsync(r.Ids, r.Estado) };
}

[Route("api/lista-espera")]
public class ListaEsperaApi(ListaEsperaService espera) : ApiBase
{
    [HttpPost]
    public Task<EntradaEsperaDto> Inscribirse(EsperaRequest r) => espera.InscribirseAsync(r.HorarioActividadId, r.Fecha);

    [HttpGet("mias")]
    public Task<List<EntradaEsperaDto>> Mias() => espera.MiasAsync();

    [HttpGet("pendientes"), Authorize(Politicas.Admin)]
    public Task<List<EntradaEsperaDto>> Pendientes() => espera.PendientesAsync();

    [HttpGet("horario/{horarioId:int}"), Authorize(Politicas.Admin)]
    public Task<List<EntradaEsperaDto>> DeHorario(int horarioId, DateOnly? fecha) => espera.DeHorarioAsync(horarioId, fecha);

    [HttpPut("{id:int}/asignar"), Authorize(Politicas.Admin)]
    public Task<EntradaEsperaDto> Asignar(int id) => espera.AsignarAsync(id);

    [HttpDelete("{id:int}")]
    public async Task<NoContentResult> Salir(int id)
    {
        await espera.SalirAsync(id);
        return NoContent();
    }
}

[Route("api/accesos")]
public class AccesosApi(AccesosService accesos) : ApiBase
{
    [HttpPost("vista-previa"), Authorize(Politicas.Escaner)]
    public Task<VistaPreviaAcceso> VistaPrevia(CodigoRequest r) => accesos.VistaPreviaAsync(r.Codigo);

    [HttpPost("registrar"), Authorize(Politicas.Escaner)]
    public Task<AccesoDto> Registrar(CodigoRequest r) => accesos.RegistrarAsync(r.Codigo);

    [HttpPut("{id:int}/salida"), Authorize(Politicas.Escaner)]
    public Task<AccesoDto> Salida(int id) => accesos.RegistrarSalidaAsync(id);

    [HttpGet, Authorize(Politicas.Admin)]
    public Task<Pagina<AccesoDto>> Auditoria(string? estado, int? sucursalId, DateOnly? desde, DateOnly? hasta, string? buscar, string? tipo, int limite = 50, int salto = 0) =>
        accesos.AuditoriaAsync(estado, sucursalId, desde, hasta, buscar, tipo, limite, salto);

    [HttpGet("historial")]
    public Task<List<AccesoDto>> Historial(int limite = 50) => accesos.HistorialAsync(limite);
}
