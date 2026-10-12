using GestionGimnasios.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Base de las pantallas del panel: sesión por cookie y mensajes al usuario.</summary>
[Authorize(AuthenticationSchemes = EsquemasAuth.Cookie)]
[AutoValidateAntiforgeryToken]
public abstract class PanelController : Controller
{
    protected void Exito(string mensaje) => TempData["Exito"] = mensaje;
    protected void Error(string mensaje) => TempData["Error"] = mensaje;

    /// <summary>Ejecuta una acción de negocio; si incumple una regla muestra su mensaje y vuelve a la pantalla.</summary>
    protected async Task<IActionResult> IntentarAsync(Func<Task> accion, string exito, Func<IActionResult> siguiente, Func<IActionResult>? siFalla = null)
    {
        try
        {
            await accion();
            Exito(exito);
            return siguiente();
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
            return (siFalla ?? siguiente)();
        }
    }

    /// <summary>Para formularios que se vuelven a mostrar con sus datos cuando falla una regla.</summary>
    protected async Task<bool> GuardarAsync(Func<Task> accion, string exito)
    {
        try
        {
            await accion();
            Exito(exito);
            return true;
        }
        catch (ErrorNegocio e)
        {
            ModelState.AddModelError(string.Empty, e.Message);
            return false;
        }
    }
}

/// <summary>Opciones del menú lateral según el nivel del usuario (NAV_ROUTES del panel base).</summary>
public record OpcionMenu(string Titulo, string Icono, string Controlador, string Accion, int NivelMinimo, bool SoloSuperAdmin = false, string? Grupo = null);

public static class Menu
{
    public static readonly OpcionMenu[] Opciones =
    [
        new("Resumen", "ti-layout-dashboard", "Resumen", "Index", 4),
        new("Control de acceso", "ti-id-badge-2", "Accesos", "Index", 4, Grupo: "Operación"),
        new("Reservas", "ti-calendar-event", "Reservas", "Index", 4, Grupo: "Operación"),
        new("Lista de espera", "ti-list-numbers", "Reservas", "ListaEspera", 4, Grupo: "Operación"),
        new("Suscripciones", "ti-credit-card", "Suscripciones", "Index", 4, Grupo: "Operación"),
        new("Usuarios", "ti-users", "Usuarios", "Index", 4, Grupo: "Gestión"),
        new("Actividades", "ti-calendar-time", "Actividades", "Index", 4, Grupo: "Gestión"),
        new("Máquinas", "ti-barbell", "Maquinas", "Index", 4, Grupo: "Gestión"),
        new("Rutinas", "ti-list-check", "Rutinas", "Index", 3, Grupo: "Gestión"),
        new("Planes", "ti-receipt", "Suscripciones", "Planes", 5, Grupo: "Gestión"),
        new("Sucursales", "ti-building-store", "Sucursales", "Index", 4, Grupo: "Gestión"),
        new("Mapa de sucursales", "ti-map-pin", "Sucursales", "Mapa", 4, Grupo: "Gestión"),
        new("Avisos", "ti-speakerphone", "Notificaciones", "Avisos", 4, Grupo: "Gestión"),
        new("Reportes", "ti-report-analytics", "Reportes", "Index", 5, Grupo: "Análisis"),
        new("Cadenas", "ti-building-skyscraper", "Cadenas", "Index", 10, true, "Sistema"),
        new("Ejercicios", "ti-stretching", "Ejercicios", "Index", 10, true, "Sistema"),
        new("Roles y parámetros", "ti-shield-lock", "Roles", "Index", 10, true, "Sistema"),
    ];
}
