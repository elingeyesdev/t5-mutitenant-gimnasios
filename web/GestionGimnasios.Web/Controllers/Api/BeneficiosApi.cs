using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers.Api;

public record ActivoRequest(bool Activo);

/// <summary>Beneficios, vales y cupones (RF-03, HU-48). La app usa las rutas del socio.</summary>
[Route("api/beneficios")]
public class BeneficiosApi(BeneficiosService beneficios) : ApiBase
{
    [HttpGet("disponibles")]
    public Task<List<BeneficioSocioDto>> Disponibles() => beneficios.DisponiblesAsync();

    [HttpPost("{id:int}/canjear")]
    public Task<CanjeDto> Canjear(int id) => beneficios.CanjearAsync(id);

    [HttpGet("canjes/mios")]
    public Task<List<CanjeDto>> MisCanjes() => beneficios.MisCanjesAsync();

    [HttpGet, Authorize(Politicas.Admin)]
    public Task<List<BeneficioDto>> Listar() => beneficios.ListarAsync();

    [HttpPost, Authorize(Politicas.Gerente)]
    public Task<BeneficioDto> Crear(BeneficioForm f) => beneficios.CrearAsync(f);

    [HttpPut("{id:int}"), Authorize(Politicas.Gerente)]
    public Task<BeneficioDto> Actualizar(int id, BeneficioForm f) => beneficios.ActualizarAsync(id, f);

    [HttpPut("{id:int}/activo"), Authorize(Politicas.Gerente)]
    public Task<BeneficioDto> CambiarActivo(int id, ActivoRequest r) => beneficios.CambiarActivoAsync(id, r.Activo);

    [HttpGet("canjes"), Authorize(Politicas.Admin)]
    public Task<List<CanjeDto>> Canjes(string? estado) => beneficios.CanjesAsync(estado);

    [HttpGet("canjes/{codigo}"), Authorize(Politicas.Admin)]
    public Task<CanjeDto> Canje(string codigo) => beneficios.CanjePorCodigoAsync(codigo);

    [HttpPut("canjes/{codigo}/entregar"), Authorize(Politicas.Admin)]
    public Task<CanjeDto> Entregar(string codigo) => beneficios.EntregarAsync(codigo);
}
