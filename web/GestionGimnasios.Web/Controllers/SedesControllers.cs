using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Cadenas (marcas): solo super administrador (HU-04).</summary>
[Authorize(Politicas.SuperAdmin)]
public class CadenasController(SedesService sedes) : PanelController
{
    public async Task<IActionResult> Index() => View(await sedes.CadenasAsync());

    [HttpPost]
    public Task<IActionResult> Crear(CadenaForm f) =>
        IntentarAsync(() => sedes.CrearCadenaAsync(f), $"Cadena {f.Nombre} creada.", () => RedirectToAction(nameof(Index)));

    [HttpPost]
    public Task<IActionResult> Editar(int id, CadenaForm f) =>
        IntentarAsync(() => sedes.ActualizarCadenaAsync(id, f), "Cadena actualizada.", () => RedirectToAction(nameof(Index)));

    [HttpPost]
    public Task<IActionResult> Eliminar(int id) =>
        IntentarAsync(() => sedes.EliminarCadenaAsync(id), "Cadena eliminada.", () => RedirectToAction(nameof(Index)));
}

/// <summary>Sucursales, su ubicación y horario de atención (HU-04, HU-05).</summary>
[Authorize(Politicas.Admin)]
public class SucursalesController(SedesService sedes) : PanelController
{
    public async Task<IActionResult> Index() => View(await sedes.SucursalesAsync());

    public async Task<IActionResult> Mapa() => View(await sedes.SucursalesAsync());

    [Authorize(Politicas.SuperAdmin)]
    public async Task<IActionResult> Crear(int? cadenaId)
    {
        ViewBag.Cadenas = await sedes.CadenasAsync();
        return View("Formulario", NuevoFormulario(cadenaId ?? 0));
    }

    [HttpPost, Authorize(Politicas.SuperAdmin)]
    public async Task<IActionResult> Crear(SucursalForm f, string[] dia, TimeOnly[] abre, TimeOnly[] cierra, bool[] feriado)
    {
        f.Horarios = Horarios(dia, abre, cierra, feriado);
        SucursalDto? creada = null;
        if (await GuardarAsync(async () => creada = await sedes.CrearSucursalAsync(f), $"Sucursal {f.Nombre} creada."))
            return RedirectToAction(nameof(Index));
        ViewBag.Cadenas = await sedes.CadenasAsync();
        return View("Formulario", f);
    }

    public async Task<IActionResult> Editar(int id)
    {
        var s = await sedes.SucursalAsync(id);
        ViewBag.Sucursal = s;
        ViewBag.Cadenas = await sedes.CadenasAsync();
        return View("Formulario", new SucursalForm
        {
            CadenaId = s.CadenaId, Nombre = s.Nombre, Descripcion = s.Descripcion, CapacidadMaxima = s.CapacidadMaxima,
            Activa = s.Activa, Ubicacion = s.Ubicacion, Horarios = s.Horarios.ToList(),
        });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, SucursalForm f, string[] dia, TimeOnly[] abre, TimeOnly[] cierra, bool[] feriado)
    {
        f.Horarios = Horarios(dia, abre, cierra, feriado);
        if (await GuardarAsync(() => sedes.ActualizarSucursalAsync(id, f), "Sucursal actualizada."))
            return RedirectToAction(nameof(Editar), new { id });
        ViewBag.Sucursal = await sedes.SucursalAsync(id);
        ViewBag.Cadenas = await sedes.CadenasAsync();
        return View("Formulario", f);
    }

    [HttpPost, Authorize(Politicas.SuperAdmin)]
    public Task<IActionResult> Eliminar(int id) =>
        IntentarAsync(() => sedes.EliminarSucursalAsync(id), "Sucursal eliminada.", () => RedirectToAction(nameof(Index)));

    private static SucursalForm NuevoFormulario(int cadenaId) => new()
    {
        CadenaId = cadenaId,
        Ubicacion = new UbicacionDto("", "Santa Cruz", -17.7833m, -63.1821m),
        Horarios = Domain.Dias.Cortos.Select(d => new HorarioSucursalDto(0, d,
            d == "DOM" ? new TimeOnly(8, 0) : d == "SAB" ? new TimeOnly(7, 0) : new TimeOnly(6, 0),
            d == "DOM" ? new TimeOnly(13, 0) : d == "SAB" ? new TimeOnly(18, 0) : new TimeOnly(22, 0), false)).ToList(),
    };

    private static List<HorarioSucursalDto> Horarios(string[] dia, TimeOnly[] abre, TimeOnly[] cierra, bool[] feriado) =>
        dia.Select((d, i) => new HorarioSucursalDto(0, d, abre[i], cierra[i], i < feriado.Length && feriado[i])).ToList();
}
