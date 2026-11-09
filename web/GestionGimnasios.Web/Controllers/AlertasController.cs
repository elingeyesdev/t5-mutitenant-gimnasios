using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Bandeja de alertas de inactividad, umbrales de la cadena y detección manual (RF-04, CU-38).</summary>
[Authorize(Politicas.Asesor)]
public class AlertasController(AlertasService alertas, SedesService sedes) : PanelController
{
    /// <summary>Alertas del alcance del personal; sin filtro de estado muestra las que siguen abiertas o en seguimiento.</summary>
    public async Task<IActionResult> Index(string? estado, string? nivel, int? sucursalId)
    {
        ViewBag.Estado = estado;
        ViewBag.Nivel = nivel;
        ViewBag.SucursalId = sucursalId;
        ViewBag.Resumen = await alertas.ResumenAsync();
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        return View(await alertas.ListarAsync(estado, nivel, sucursalId));
    }

    public async Task<IActionResult> Detalle(int id)
    {
        try
        {
            return View(await alertas.ObtenerAsync(id));
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public Task<IActionResult> Intervenir(int id, IntervencionForm f) =>
        IntentarAsync(() => alertas.RegistrarIntervencionAsync(id, f), f.Cerrar ? "Contacto registrado y alerta cerrada." : "Contacto registrado: la alerta queda en seguimiento.",
            () => RedirectToAction(nameof(Detalle), new { id }));

    [Authorize(Politicas.Gerente)]
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

    [HttpPost, Authorize(Politicas.Gerente)]
    public Task<IActionResult> Configuracion(ReglaInactividadForm f) =>
        IntentarAsync(() => alertas.GuardarReglaAsync(f), "Umbrales guardados. Se aplican en la próxima revisión.", () => RedirectToAction(nameof(Configuracion)));

    [HttpPost, Authorize(Politicas.Gerente)]
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
