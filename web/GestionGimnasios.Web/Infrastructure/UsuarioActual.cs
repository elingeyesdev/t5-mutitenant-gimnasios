using System.Security.Claims;
using GestionGimnasios.Web.Domain;

namespace GestionGimnasios.Web.Infrastructure;

/// <summary>Nombres de los claims propios del sistema (sesión del panel y token JWT de la app).</summary>
public static class Claims
{
    public const string Nivel = "nivel";
    public const string Cadena = "cadena";
    public const string Sucursal = "sucursal";
    public const string Nombre = "nombre";
    public const string Alcance = "alcance";
}

/// <summary>Usuario autenticado de la petición en curso y su alcance (cadena y sucursal).</summary>
public class UsuarioActual(IHttpContextAccessor accessor)
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool Autenticado => Principal?.Identity?.IsAuthenticated == true;
    public int Id => Entero(ClaimTypes.NameIdentifier) ?? 0;
    public string Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? "";
    public string? Rol => Principal?.FindFirstValue(ClaimTypes.Role);
    public string Nombre => Principal?.FindFirstValue(Claims.Nombre) ?? Email;
    public int Nivel => Entero(Claims.Nivel) ?? 0;
    public int? CadenaId => Entero(Claims.Cadena);
    public int? SucursalId => Entero(Claims.Sucursal);

    public bool EsSuperAdmin => Nivel >= Niveles.SuperAdmin;
    public bool EsGerente => Nivel == Niveles.Gerente;
    public bool EsCliente => Rol == Roles.Cliente;
    public bool EsPersonal => Autenticado && !EsCliente && !EsSuperAdmin;

    /// <summary>
    /// Cadena por la que EF Core filtra los datos operativos: la del personal. El super
    /// administrador y los clientes no se filtran por cadena (el cliente consulta el catálogo
    /// de todas las sucursales y sus datos propios se filtran por usuario en los servicios).
    /// </summary>
    public int? CadenaFiltro => EsPersonal ? CadenaId ?? -1 : null;

    /// <summary>Sucursal a la que se limita el personal de sucursal (nivel menor a gerente).</summary>
    public int? SucursalFiltro => EsPersonal && Nivel < Niveles.Gerente ? SucursalId : EsGerente ? SucursalId : null;

    private int? Entero(string claim) =>
        int.TryParse(Principal?.FindFirstValue(claim), out var v) ? v : null;
}
