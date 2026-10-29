using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Perfiles portables que otros socios autorizaron a la cadena (RF-02, CU-36).</summary>
[Authorize(Politicas.Admin)]
public class PortabilidadController(PortabilidadService portabilidad) : PanelController
{
    public async Task<IActionResult> Index()
    {
        try
        {
            return View(await portabilidad.AutorizadosAsync());
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
            return View(new List<ConsentimientoDto>());
        }
    }

    public async Task<IActionResult> Perfil(int id)
    {
        try
        {
            return View(await portabilidad.PerfilAsync(id));
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
            return RedirectToAction(nameof(Index));
        }
    }
}
