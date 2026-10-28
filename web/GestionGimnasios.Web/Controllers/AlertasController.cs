using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Umbrales de inactividad de la cadena y detección manual (RF-04, CU-38).</summary>
[Authorize(Politicas.Gerente)]
public class AlertasController(AlertasService alertas) : PanelController
{
    public async Task<IActionResult> Configuracion()
    {
        try
        {
            ViewBag.Resumen = await alertas.ResumenAsync();
            return View(await alertas.ReglaAsync());
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
            return RedirectToAction("Index", "Resumen");
        }
    }

    [HttpPost]
    public Task<IActionResult> Configuracion(ReglaInactividadForm f) =>
        IntentarAsync(() => alertas.GuardarReglaAsync(f), "Umbrales guardados. Se aplican en la próxima revisión.", () => RedirectToAction(nameof(Configuracion)));

    [HttpPost]
    public async Task<IActionResult> Detectar()
    {
        try
        {
            var r = await alertas.DetectarAhoraAsync();
            Exito($"Revisión terminada: {r.Revisados} socios revisados, {r.Nuevas} alertas nuevas, {r.Actualizadas} actualizadas y {r.Cerradas} cerradas.");
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
        }
        return RedirectToAction(nameof(Configuracion));
    }
}
