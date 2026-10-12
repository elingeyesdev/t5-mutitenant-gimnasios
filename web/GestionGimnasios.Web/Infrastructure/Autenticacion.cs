using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GestionGimnasios.Web.Infrastructure;

public static class EsquemasAuth
{
    public const string Cookie = "Panel";
    public const string Jwt = "Api";
    public const string Mixto = "Mixto";
}

public class OpcionesJwt
{
    public string Clave { get; set; } = "";
    public string Emisor { get; set; } = "GestionGimnasios";
    public int HorasValidez { get; set; } = 168;
}

/// <summary>Alcance resuelto del usuario: su rol de mayor nivel, cadena y sucursal.</summary>
public record Alcance(string? Rol, int Nivel, int? CadenaId, int? SucursalId, string? Cadena, string? Sucursal);

/// <summary>Valida credenciales y emite la identidad (cookie del panel o JWT de la app).</summary>
public class ServicioAutenticacion(AppDbContext db, OpcionesJwt jwt)
{
    private static readonly PasswordHasher<Usuario> Hasher = new();

    public static string Hashear(string password) => Hasher.HashPassword(new Usuario(), password);

    public static bool Verificar(Usuario u, string password) =>
        Hasher.VerifyHashedPassword(u, u.PasswordHash, password) != PasswordVerificationResult.Failed;

    public async Task<Usuario> ValidarAsync(string email, string password)
    {
        var usuario = await db.Usuarios.Include(u => u.Perfil)
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower());
        if (usuario is null || !Verificar(usuario, password))
            throw ErrorNegocio.NoAutorizado("Credenciales inválidas");
        if (!usuario.Activo)
            throw ErrorNegocio.NoAutorizado("Cuenta desactivada");
        return usuario;
    }

    /// <summary>Igual que buildJwtPayload del proyecto base: manda el rol de mayor nivel.</summary>
    public async Task<Alcance> AlcanceAsync(int usuarioId)
    {
        var roles = await db.UsuarioRoles.IgnoreQueryFilters()
            .Where(r => r.UsuarioId == usuarioId)
            .Include(r => r.Rol).Include(r => r.Cadena).Include(r => r.Sucursal)
            .ToListAsync();
        var top = roles.OrderByDescending(r => r.Rol!.Nivel).FirstOrDefault();
        if (top is null) return new Alcance(null, 0, null, null, null, null);

        if (top.Rol!.Nombre == Roles.Cliente)
        {
            var m = await db.Membresias.IgnoreQueryFilters().Include(x => x.Cadena).Include(x => x.Sucursal)
                .Where(x => x.UsuarioId == usuarioId && x.Activa).OrderBy(x => x.Id).FirstOrDefaultAsync();
            return new Alcance(top.Rol.Nombre, top.Rol.Nivel, m?.CadenaId, m?.SucursalId, m?.Cadena?.Nombre, m?.Sucursal?.Nombre);
        }
        var cadenaId = top.CadenaId ?? top.Sucursal?.CadenaId;
        var cadena = top.Cadena?.Nombre ?? (cadenaId is null ? null : await db.Cadenas.Where(c => c.Id == cadenaId).Select(c => c.Nombre).FirstOrDefaultAsync());
        return new Alcance(top.Rol.Nombre, top.Rol.Nivel, cadenaId, top.SucursalId, cadena, top.Sucursal?.Nombre);
    }

    public async Task<ClaimsPrincipal> PrincipalAsync(Usuario u, string esquema)
    {
        var a = await AlcanceAsync(u.Id);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Email, u.Email),
            new(Claims.Nivel, a.Nivel.ToString()),
            new(Claims.Nombre, u.Perfil?.NombreCompleto is { Length: > 0 } nombre ? nombre : u.Email),
        };
        if (a.Rol is not null) claims.Add(new Claim(ClaimTypes.Role, a.Rol));
        if (a.CadenaId is not null) claims.Add(new Claim(Claims.Cadena, a.CadenaId.ToString()!));
        if (a.SucursalId is not null) claims.Add(new Claim(Claims.Sucursal, a.SucursalId.ToString()!));
        if ((a.Sucursal ?? a.Cadena) is { } alcance) claims.Add(new Claim(Claims.Alcance, alcance));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, esquema));
    }

    public string Token(ClaimsPrincipal principal)
    {
        var credenciales = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(jwt.Emisor, jwt.Emisor, principal.Claims,
            expires: DateTime.UtcNow.AddHours(jwt.HorasValidez), signingCredentials: credenciales);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
