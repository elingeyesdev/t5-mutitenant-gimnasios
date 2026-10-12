using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Roles, permisos y parámetros del sistema (HU-03).</summary>
[Authorize(Politicas.SuperAdmin)]
public class RolesController(RolesService roles) : PanelController
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Permisos = await roles.PermisosAsync();
        ViewBag.Parametros = await roles.ParametrosAsync();
        return View(await roles.RolesAsync());
    }

    [HttpPost]
    public Task<IActionResult> GuardarRol(int? id, RolForm f) =>
        IntentarAsync(() => id is int i ? roles.ActualizarRolAsync(i, f) : roles.CrearRolAsync(f), "Rol guardado.", () => RedirectToAction(nameof(Index)));

    [HttpPost]
    public Task<IActionResult> EliminarRol(int id) =>
        IntentarAsync(() => roles.EliminarRolAsync(id), "Rol eliminado.", () => RedirectToAction(nameof(Index)));

    [HttpPost]
    public Task<IActionResult> CrearPermiso(PermisoForm f) =>
        IntentarAsync(() => roles.CrearPermisoAsync(f), "Permiso creado.", () => RedirectToAction(nameof(Index)));

    [HttpPost]
    public Task<IActionResult> AlternarPermiso(int rolId, int permisoId, bool asignar) =>
        IntentarAsync(() => asignar ? roles.AsignarPermisoAsync(rolId, permisoId) : roles.QuitarPermisoAsync(rolId, permisoId),
            asignar ? "Permiso asignado." : "Permiso quitado.", () => RedirectToAction(nameof(Index)));

    [HttpPost]
    public Task<IActionResult> GuardarParametro(ParametroForm f) =>
        IntentarAsync(() => roles.GuardarParametroAsync(f), "Parámetro guardado.", () => RedirectToAction(nameof(Index)));

    [HttpPost]
    public Task<IActionResult> EliminarParametro(int id) =>
        IntentarAsync(() => roles.EliminarParametroAsync(id), "Parámetro eliminado.", () => RedirectToAction(nameof(Index)));
}

/// <summary>Bandeja de notificaciones y avisos a usuarios (HU-20).</summary>
public class NotificacionesController(NotificacionesService notificaciones, SedesService sedes) : PanelController
{
    public async Task<IActionResult> Index() => View(await notificaciones.MiasAsync(100));

    [HttpPost]
    public Task<IActionResult> MarcarTodas() =>
        IntentarAsync(() => notificaciones.MarcarTodasLeidasAsync(), "Notificaciones marcadas como leídas.", () => RedirectToAction(nameof(Index)));

    [Authorize(Politicas.Admin)]
    public async Task<IActionResult> Avisos()
    {
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        ViewBag.Plantillas = await notificaciones.PlantillasAsync();
        return View(new AvisoForm());
    }

    [HttpPost, Authorize(Politicas.Admin)]
    public async Task<IActionResult> Avisos(AvisoForm f)
    {
        try
        {
            var n = await notificaciones.EnviarAvisoAsync(f);
            Exito($"Aviso enviado a {n} usuario(s).");
            return RedirectToAction(nameof(Avisos));
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
            ViewBag.Sucursales = await sedes.SucursalesAsync();
            ViewBag.Plantillas = await notificaciones.PlantillasAsync();
            return View(f);
        }
    }

    [HttpPost, Authorize(Politicas.SuperAdmin)]
    public Task<IActionResult> GuardarPlantilla(PlantillaDto f) =>
        IntentarAsync(() => notificaciones.GuardarPlantillaAsync(f), "Plantilla guardada.", () => RedirectToAction(nameof(Avisos)));
}

/// <summary>Reportes de operación y de gestión con vista imprimible (HU-22, HU-23).</summary>
[Authorize(Politicas.Gerente)]
public class ReportesController(ReportesService reportes, SedesService sedes) : PanelController
{
    public IActionResult Index() => View(ReportesService.Tipos);

    public async Task<IActionResult> Ver(string id, [FromQuery] FiltroReporte f)
    {
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        ViewBag.Filtro = f;
        try
        {
            return View(await reportes.GenerarAsync(id, f));
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
            return RedirectToAction(nameof(Index));
        }
    }
}

public class PasswordForm
{
    public string Actual { get; set; } = "";
    public string Nueva { get; set; } = "";
}

/// <summary>Perfil propio del usuario del panel.</summary>
public class PerfilController(UsuariosService usuarios, UsuarioActual actual, Data.AppDbContext db) : PanelController
{
    public async Task<IActionResult> Index() => View(await usuarios.ObtenerAsync(actual.Id));

    [HttpPost]
    public Task<IActionResult> Guardar(PerfilForm f) =>
        IntentarAsync(() => usuarios.ActualizarMiPerfilAsync(f), "Perfil actualizado.", () => RedirectToAction(nameof(Index)));

    [HttpPost]
    public Task<IActionResult> Password(PasswordForm f) =>
        IntentarAsync(async () =>
        {
            var u = await db.Usuarios.FindAsync(actual.Id) ?? throw ErrorNegocio.NoEncontrado("Usuario no encontrado.");
            if (!ServicioAutenticacion.Verificar(u, f.Actual)) throw ErrorNegocio.Solicitud("La contraseña actual no es correcta.");
            if (f.Nueva.Length < 6) throw ErrorNegocio.Solicitud("La nueva contraseña debe tener al menos 6 caracteres.");
            u.PasswordHash = ServicioAutenticacion.Hashear(f.Nueva);
            await db.SaveChangesAsync();
        }, "Contraseña actualizada.", () => RedirectToAction(nameof(Index)));
}
