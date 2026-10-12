using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Gestión de usuarios de su alcance y turnos del personal (HU-06, HU-07, HU-08).</summary>
[Authorize(Politicas.Admin)]
public class UsuariosController(UsuariosService usuarios, RolesService roles, SedesService sedes, PersonalService personal, UsuarioActual actual) : PanelController
{
    public async Task<IActionResult> Index([FromQuery] FiltroUsuarios f)
    {
        f.Limite = 20;
        ViewBag.Filtro = f;
        ViewBag.Roles = await roles.RolesAsync();
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        return View(await usuarios.ListarAsync(f));
    }

    public async Task<IActionResult> Crear(int? rolId)
    {
        await CargarListas();
        return View("Formulario", new UsuarioForm { RolId = rolId, SucursalId = actual.SucursalFiltro });
    }

    [HttpPost]
    public async Task<IActionResult> Crear(UsuarioForm f)
    {
        if (await GuardarAsync(() => usuarios.CrearAsync(f), $"Usuario {f.Email} creado.")) return RedirectToAction(nameof(Index));
        await CargarListas();
        return View("Formulario", f);
    }

    public async Task<IActionResult> Editar(int id)
    {
        var u = await usuarios.ObtenerVisibleAsync(id);
        await CargarListas();
        ViewBag.Usuario = u;
        var rol = u.Roles.FirstOrDefault();
        return View("Formulario", new UsuarioForm
        {
            Email = u.Email, Nombres = u.Perfil?.Nombres ?? "", Apellidos = u.Perfil?.Apellidos ?? "", Telefono = u.Perfil?.Telefono,
            Ci = u.Perfil?.Ci, Genero = u.Perfil?.Genero, FechaNacimiento = u.Perfil?.FechaNacimiento, Activo = u.Activo,
            RolId = rol?.RolId, CadenaId = rol?.CadenaId ?? u.CadenaId, SucursalId = rol?.SucursalId ?? u.SucursalId,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, UsuarioForm f)
    {
        if (await GuardarAsync(() => usuarios.ActualizarAsync(id, f), "Cambios guardados.")) return RedirectToAction(nameof(Index));
        await CargarListas();
        ViewBag.Usuario = await usuarios.ObtenerVisibleAsync(id);
        return View("Formulario", f);
    }

    [HttpPost]
    public Task<IActionResult> Eliminar(int id) =>
        IntentarAsync(() => usuarios.EliminarAsync(id), "Usuario eliminado.", () => RedirectToAction(nameof(Index)));

    public async Task<IActionResult> Turnos(int id)
    {
        ViewBag.Usuario = await usuarios.ObtenerVisibleAsync(id);
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        return View(await personal.TurnosAsync(id));
    }

    [HttpPost]
    public Task<IActionResult> Turnos(int id, int sucursalId, bool[] trabaja, TimeOnly?[] inicio, TimeOnly?[] fin)
    {
        var turnos = Enumerable.Range(0, 7)
            .Where(d => d < trabaja.Length && trabaja[d] && inicio.ElementAtOrDefault(d) is not null && fin.ElementAtOrDefault(d) is not null)
            .Select(d => new TurnoForm(d, inicio[d]!.Value, fin[d]!.Value)).ToList();
        return IntentarAsync(() => personal.AsignarTurnosAsync(id, sucursalId, turnos), "Turnos guardados.", () => RedirectToAction(nameof(Turnos), new { id }));
    }

    private async Task CargarListas()
    {
        ViewBag.Roles = await roles.RolesAsignablesAsync();
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        ViewBag.Cadenas = await sedes.CadenasAsync();
    }
}
