using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Conciliación de las renovaciones con QR en recepción (RF-05, CU-39).</summary>
[Authorize(Politicas.Admin)]
public class PagosQrController(PagosQrService pagos) : PanelController
{
    private const string Todas = "TODAS";

    /// <summary>Sin filtro muestra las pendientes, que son las que requieren una acción.</summary>
    public async Task<IActionResult> Index(string? estado)
    {
        estado ??= EstadosOrdenPago.Pendiente;
        ViewBag.Estado = estado;
        ViewBag.Resumen = await pagos.ResumenAsync();
        return View(await pagos.ListarAsync(estado == Todas ? null : estado));
    }

    public async Task<IActionResult> Detalle(int id)
    {
        try
        {
            return View(await pagos.ObtenerAsync(id));
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Comprobante(int id)
    {
        try
        {
            var a = await pagos.ComprobanteAsync(id);
            return PhysicalFile(a.Ruta, a.TipoContenido);
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
            return RedirectToAction(nameof(Detalle), new { id });
        }
    }

    [HttpPost]
    public Task<IActionResult> Confirmar(int id) =>
        IntentarAsync(() => pagos.ConfirmarAsync(id), "Pago confirmado: la suscripción quedó renovada.", () => RedirectToAction(nameof(Detalle), new { id }));

    [HttpPost]
    public Task<IActionResult> Rechazar(int id, string? motivo) =>
        IntentarAsync(() => pagos.RechazarAsync(id, motivo), "Orden rechazada: la suscripción no cambió.", () => RedirectToAction(nameof(Detalle), new { id }));
}
