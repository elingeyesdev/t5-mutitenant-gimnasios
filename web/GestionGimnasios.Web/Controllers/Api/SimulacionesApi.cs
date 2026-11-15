using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers.Api;

/// <summary>Simulación de un ejercicio con las pautas del plan nutricional del socio (RF-06, HU-51).</summary>
[Route("api/simulaciones")]
public class SimulacionesApi(SimulacionesService simulaciones) : ApiBase
{
    [HttpGet("ejercicios/{ejercicioId:int}")]
    public Task<SimulacionDto> Ejercicio(int ejercicioId) => simulaciones.SimulacionAsync(ejercicioId);
}
