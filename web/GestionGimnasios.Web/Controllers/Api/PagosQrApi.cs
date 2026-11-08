using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers.Api;

/// <summary>Renovación de la membresía con QR desde la app del socio (RF-05, HU-50).</summary>
[Route("api/pagos-qr")]
public class PagosQrApi(PagosQrService pagos) : ApiBase
{
    [HttpPost]
    public Task<OrdenPagoDto> Crear(OrdenPagoForm f) => pagos.CrearAsync(f);

    [HttpGet("mias")]
    public Task<List<OrdenPagoDto>> Mias() => pagos.MisOrdenesAsync();

    [HttpGet("{id:int}")]
    public Task<OrdenPagoDto> Obtener(int id) => pagos.ObtenerPropiaAsync(id);

    [HttpPost("{id:int}/comprobante")]
    [Consumes("multipart/form-data")]
    public Task<OrdenPagoDto> Comprobante(int id, [FromForm] ComprobanteForm f) => pagos.RegistrarComprobanteAsync(id, f);

    [HttpGet("{id:int}/comprobante")]
    [Produces("image/jpeg", "image/png", "image/webp")]
    public async Task<IActionResult> VerComprobante(int id)
    {
        var a = await pagos.ComprobantePropioAsync(id);
        return PhysicalFile(a.Ruta, a.TipoContenido);
    }
}
