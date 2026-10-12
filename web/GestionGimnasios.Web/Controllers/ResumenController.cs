using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Panel de resumen (HU-21).</summary>
[Authorize(Politicas.Admin)]
public class ResumenController(ReportesService reportes, SedesService sedes) : PanelController
{
    public async Task<IActionResult> Index(string periodo = "week")
    {
        ViewBag.Periodo = periodo;
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        return View(await reportes.ResumenAsync(periodo));
    }
}
