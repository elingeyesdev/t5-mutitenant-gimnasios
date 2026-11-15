using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>
/// Planes nutricionales desde el panel del asesor (RF-06, CU-40): el entrenador o el nutricionista
/// definen el plan y las pautas que el socio ve junto a la simulación de cada ejercicio en la app.
/// </summary>
[Authorize(Politicas.Asesor)]
public class NutricionController(PersonalService personal) : PanelController
{
    public async Task<IActionResult> Index() => View(await personal.PlanesDeMisAlumnosAsync());

    public async Task<IActionResult> Editar(int clienteId)
    {
        var alumno = await AlumnoAsync(clienteId);
        if (alumno is null) return SinAsesoria();
        return View(alumno);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int clienteId, PlanNutricionalForm f)
    {
        if (await GuardarAsync(() => personal.GuardarPlanAsync(clienteId, f), "Plan guardado. El socio ya lo ve en la app."))
            return RedirectToAction(nameof(Editar), new { clienteId });

        var alumno = await AlumnoAsync(clienteId);
        if (alumno is null) return SinAsesoria();
        // Vuelve a mostrar lo que escribió, con el mensaje de la regla que no se cumplió.
        var escrito = new PlanNutricionalDto(alumno.Plan?.Id ?? 0, 0, "", clienteId, alumno.Cliente, f.KcalDiarias, f.ProteinaG, f.CarbohidratosG, f.GrasaG,
            f.Notas, f.PlanComidas ?? [], alumno.Plan?.ActualizadoEn ?? DateTime.UtcNow, f.PautaAntesEntrenar, f.PautaDespuesEntrenar, f.Hidratacion);
        return View(alumno with { Plan = escrito });
    }

    private async Task<AlumnoPlanDto?> AlumnoAsync(int clienteId) =>
        (await personal.PlanesDeMisAlumnosAsync()).FirstOrDefault(a => a.ClienteId == clienteId);

    private RedirectToActionResult SinAsesoria()
    {
        Error("No tienes una asesoría activa con este socio.");
        return RedirectToAction(nameof(Index));
    }
}
