using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers.Api;

/// <summary>Perfil deportivo portable con consentimiento (RF-02, HU-47). La app usa las rutas del socio.</summary>
[Route("api/portabilidad")]
public class PortabilidadApi(PortabilidadService portabilidad) : ApiBase
{
    [HttpGet("cadenas")]
    public Task<List<CadenaDisponibleDto>> Cadenas() => portabilidad.CadenasDisponiblesAsync();

    [HttpGet("consentimientos")]
    public Task<List<ConsentimientoDto>> Mios() => portabilidad.MisConsentimientosAsync();

    [HttpPost("consentimientos")]
    public Task<ConsentimientoDto> Otorgar(ConsentimientoForm f) => portabilidad.OtorgarAsync(f);

    [HttpPut("consentimientos/{id:int}/revocar")]
    public Task<ConsentimientoDto> Revocar(int id) => portabilidad.RevocarAsync(id);

    [HttpGet("consentimientos/{id:int}/consultas")]
    public Task<List<ConsultaPerfilDto>> Consultas(int id) => portabilidad.ConsultasAsync(id);

    [HttpGet("autorizados"), Authorize(Politicas.Admin)]
    public Task<List<ConsentimientoDto>> Autorizados() => portabilidad.AutorizadosAsync();

    [HttpGet("socios/{usuarioId:int}"), Authorize(Politicas.Admin)]
    public Task<PerfilPortableDto> Perfil(int usuarioId) => portabilidad.PerfilAsync(usuarioId);
}
