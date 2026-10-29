using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers.Api;

/// <summary>Alertas de inactividad o abandono (RF-04, HU-49).</summary>
[Route("api/alertas")]
[Authorize(Politicas.Asesor)]
public class AlertasApi(AlertasService alertas) : ApiBase
{
    [HttpGet]
    public Task<List<AlertaDto>> Listar(string? estado, string? nivel, int? sucursalId) => alertas.ListarAsync(estado, nivel, sucursalId);

    [HttpGet("resumen")]
    public Task<ResumenAlertasDto> Resumen() => alertas.ResumenAsync();

    [HttpGet("{id:int}")]
    public Task<AlertaDto> Obtener(int id) => alertas.ObtenerAsync(id);

    [HttpPost("{id:int}/intervenciones")]
    public Task<AlertaDto> Intervenir(int id, IntervencionForm f) => alertas.RegistrarIntervencionAsync(id, f);

    [HttpGet("regla"), Authorize(Politicas.Gerente)]
    public Task<ReglaInactividadDto> Regla() => alertas.ReglaAsync();

    [HttpPut("regla"), Authorize(Politicas.Gerente)]
    public Task<ReglaInactividadDto> GuardarRegla(ReglaInactividadForm f) => alertas.GuardarReglaAsync(f);

    [HttpPost("detectar"), Authorize(Politicas.Gerente)]
    public Task<ResultadoDeteccion> Detectar() => alertas.DetectarAhoraAsync();
}
