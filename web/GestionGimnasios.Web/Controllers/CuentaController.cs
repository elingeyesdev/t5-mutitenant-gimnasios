using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestionGimnasios.Web.Controllers;

public class LoginForm
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool Recordar { get; set; }
}

/// <summary>Inicio y cierre de sesión del panel, y registro público de clientes (HU-01, HU-02).</summary>
[AutoValidateAntiforgeryToken]
public class CuentaController(ServicioAutenticacion auth, UsuariosService usuarios) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginForm());
    }

    [HttpPost, AllowAnonymous, EnableRateLimiting(Politicas.LimiteLogin)]
    public async Task<IActionResult> Login(LoginForm f, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        try
        {
            var u = await auth.ValidarAsync(f.Email, f.Password);
            var principal = await auth.PrincipalAsync(u, EsquemasAuth.Cookie);
            await HttpContext.SignInAsync(EsquemasAuth.Cookie, principal, new AuthenticationProperties { IsPersistent = f.Recordar });
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
        }
        catch (ErrorNegocio e)
        {
            ModelState.AddModelError(string.Empty, e.Message);
            return View(f);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(EsquemasAuth.Cookie);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Registro() => View(new UsuarioForm());

    [HttpPost, AllowAnonymous, EnableRateLimiting(Politicas.LimiteLogin)]
    public async Task<IActionResult> Registro(UsuarioForm f)
    {
        try
        {
            await usuarios.CrearAsync(f, registroPublico: true);
            TempData["Exito"] = "Tu cuenta de cliente fue creada. Ya puedes iniciar sesión en la app.";
            return RedirectToAction(nameof(Login));
        }
        catch (ErrorNegocio e)
        {
            ModelState.AddModelError(string.Empty, e.Message);
            return View(f);
        }
    }

    [HttpGet, AllowAnonymous]
    public IActionResult AccesoDenegado() => View();
}

/// <summary>Entrada del panel: el personal administrativo va al resumen; los demás roles usan la app.</summary>
[Authorize(AuthenticationSchemes = EsquemasAuth.Cookie)]
public class HomeController(UsuarioActual usuario) : Controller
{
    public IActionResult Index()
    {
        if (usuario.Nivel >= Niveles.Recepcionista) return RedirectToAction("Index", "Resumen");
        if (usuario.Nivel >= Niveles.Asesor) return RedirectToAction("Index", "Rutinas");
        return View("UsarApp");
    }

    [AllowAnonymous, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new Models.ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
