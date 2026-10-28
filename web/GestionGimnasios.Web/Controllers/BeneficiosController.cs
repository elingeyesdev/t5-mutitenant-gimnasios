using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Beneficios de la cadena (gerente) y validación de canjes en recepción (RF-03, CU-37).</summary>
[Authorize(Politicas.Admin)]
public class BeneficiosController(BeneficiosService beneficios, SedesService sedes) : PanelController
{
    [Authorize(Politicas.Gerente)]
    public async Task<IActionResult> Index()
    {
        ViewBag.Cadenas = await sedes.CadenasAsync();
        return View(await beneficios.ListarAsync());
    }

    [HttpPost, Authorize(Politicas.Gerente)]
    public Task<IActionResult> Guardar(int? id, BeneficioForm f) =>
        IntentarAsync(() => id is int i ? beneficios.ActualizarAsync(i, f) : beneficios.CrearAsync(f), "Beneficio guardado.", () => RedirectToAction(nameof(Index)));

    [HttpPost, Authorize(Politicas.Gerente)]
    public Task<IActionResult> Activo(int id, bool activo) =>
        IntentarAsync(() => beneficios.CambiarActivoAsync(id, activo), activo ? "Beneficio activado." : "Beneficio desactivado.", () => RedirectToAction(nameof(Index)));

    /// <summary>Recepción busca el comprobante por su código y entrega el beneficio.</summary>
    public async Task<IActionResult> Canjes(string? codigo, string? estado)
    {
        ViewBag.Codigo = codigo;
        ViewBag.Estado = estado;
        if (!string.IsNullOrWhiteSpace(codigo))
        {
            try
            {
                ViewBag.Encontrado = await beneficios.CanjePorCodigoAsync(codigo);
            }
            catch (ErrorNegocio e)
            {
                Error(e.Message);
            }
        }
        return View(await beneficios.CanjesAsync(estado));
    }

    [HttpPost]
    public Task<IActionResult> Entregar(string codigo) =>
        IntentarAsync(() => beneficios.EntregarAsync(codigo), "Beneficio entregado: el comprobante ya no se puede volver a usar.",
            () => RedirectToAction(nameof(Canjes)), () => RedirectToAction(nameof(Canjes), new { codigo }));
}
