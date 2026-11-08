using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Revisión de la técnica por video desde el panel del entrenador (RF-01, CU-35).</summary>
[Authorize(Politicas.Asesor)]
public class VideosTecnicaController(VideosTecnicaService videos) : PanelController
{
    public async Task<IActionResult> Index(string? estado)
    {
        ViewBag.Estado = estado;
        return View(await videos.RecibidosAsync(estado));
    }

    public async Task<IActionResult> Detalle(int id)
    {
        try
        {
            return View(await videos.ObtenerAsync(id));
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Archivo(int id)
    {
        try
        {
            var a = await videos.ArchivoAsync(id);
            return PhysicalFile(a.Ruta, a.TipoContenido, enableRangeProcessing: true);
        }
        catch (ErrorNegocio)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public Task<IActionResult> Revisar(int id, RevisionVideoForm f) =>
        IntentarAsync(() => videos.RevisarAsync(id, f), "Devolución enviada al socio.", () => RedirectToAction(nameof(Detalle), new { id }));
}
