using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestionGimnasios.Web.Controllers.Api;

/// <summary>Base de los controladores de la API: JSON, autenticación obligatoria y prefijo /api.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiBase : ControllerBase;

public record LoginRequest(string Email, string Password);
public record CorreoRequest(string Email);
public record RestablecerRequest(string Email, string Codigo, string Password);

[Route("api/auth")]
public class AuthApi(CuentaService cuenta, UsuariosService usuarios, UsuarioActual actual) : ApiBase
{
    [AllowAnonymous, HttpPost("login"), EnableRateLimiting(Politicas.LimiteLogin)]
    public Task<SesionIniciadaDto> Login(LoginRequest r) => cuenta.IniciarSesionAsync(r.Email, r.Password);

    [AllowAnonymous, HttpPost("registro"), EnableRateLimiting(Politicas.LimiteLogin)]
    public Task<SesionIniciadaDto> Registro(UsuarioForm f) => cuenta.RegistrarAsync(f);

    [AllowAnonymous, HttpPost("olvide-password"), EnableRateLimiting(Politicas.LimiteRecuperacion)]
    public async Task<object> OlvidePassword(CorreoRequest r) => new { mensaje = await cuenta.OlvidePasswordAsync(r.Email) };

    [AllowAnonymous, HttpPost("restablecer-password"), EnableRateLimiting(Politicas.LimiteRecuperacion)]
    public async Task<object> Restablecer(RestablecerRequest r) => new { mensaje = await cuenta.RestablecerPasswordAsync(r.Email, r.Codigo, r.Password) };

    [HttpGet("yo")]
    public Task<UsuarioDto> Yo() => usuarios.ObtenerAsync(actual.Id);

    /// <summary>La app descarta el token; se mantiene por compatibilidad con el proyecto base.</summary>
    [AllowAnonymous, HttpPost("logout")]
    public NoContentResult Logout() => NoContent();
}

[Route("api/usuarios")]
public class UsuariosApi(UsuariosService usuarios, UsuarioActual actual) : ApiBase
{
    [HttpGet, Authorize(Politicas.Admin)]
    public Task<Pagina<UsuarioDto>> Listar([FromQuery] FiltroUsuarios f) => usuarios.ListarAsync(f);

    [HttpGet("yo")]
    public Task<UsuarioDto> Yo() => usuarios.ObtenerAsync(actual.Id);

    [HttpPatch("yo/perfil")]
    public Task<PerfilDto> ActualizarPerfil(PerfilForm f) => usuarios.ActualizarMiPerfilAsync(f);

    [HttpPost("yo/avatar")]
    public async Task<object> Avatar(IFormFile archivo) => new { avatarUrl = await usuarios.GuardarAvatarAsync(archivo) };

    [HttpGet("clientes"), Authorize(Politicas.Staff)]
    public Task<List<ClienteResumenDto>> BuscarClientes(string? buscar, int limite = 20) => usuarios.BuscarClientesAsync(buscar, limite);

    [HttpGet("{id:int}/perfil-publico")]
    public Task<object> PerfilPublico(int id) => usuarios.PerfilPublicoAsync(id);

    [HttpGet("{id:int}")]
    public Task<UsuarioDto> Obtener(int id) => usuarios.ObtenerVisibleAsync(id);

    [HttpPost, Authorize(Politicas.Admin)]
    public Task<UsuarioDto> Crear(UsuarioForm f) => usuarios.CrearAsync(f);

    [HttpPut("{id:int}"), Authorize(Politicas.Admin)]
    public Task<UsuarioDto> Actualizar(int id, UsuarioForm f) => usuarios.ActualizarAsync(id, f);

    [HttpDelete("{id:int}"), Authorize(Politicas.Admin)]
    public async Task<NoContentResult> Eliminar(int id)
    {
        await usuarios.EliminarAsync(id);
        return NoContent();
    }
}

[Route("api/roles")]
public class RolesApi(RolesService roles) : ApiBase
{
    [HttpGet, Authorize(Politicas.Admin)]
    public Task<List<RolDto>> Listar() => roles.RolesAsync();

    [HttpGet("asignables"), Authorize(Politicas.Admin)]
    public Task<List<RolDto>> Asignables() => roles.RolesAsignablesAsync();

    [HttpGet("{id:int}"), Authorize(Politicas.Admin)]
    public Task<RolDto> Obtener(int id) => roles.RolAsync(id);

    [HttpPost, Authorize(Politicas.SuperAdmin)]
    public Task<RolDto> Crear(RolForm f) => roles.CrearRolAsync(f);

    [HttpPut("{id:int}"), Authorize(Politicas.SuperAdmin)]
    public Task<RolDto> Actualizar(int id, RolForm f) => roles.ActualizarRolAsync(id, f);

    [HttpDelete("{id:int}"), Authorize(Politicas.SuperAdmin)]
    public async Task<NoContentResult> Eliminar(int id)
    {
        await roles.EliminarRolAsync(id);
        return NoContent();
    }

    [HttpGet("permisos"), Authorize(Politicas.SuperAdmin)]
    public Task<List<PermisoDto>> Permisos() => roles.PermisosAsync();

    [HttpPost("permisos"), Authorize(Politicas.SuperAdmin)]
    public Task<PermisoDto> CrearPermiso(PermisoForm f) => roles.CrearPermisoAsync(f);

    [HttpPost("{rolId:int}/permisos/{permisoId:int}"), Authorize(Politicas.SuperAdmin)]
    public async Task<NoContentResult> AsignarPermiso(int rolId, int permisoId)
    {
        await roles.AsignarPermisoAsync(rolId, permisoId);
        return NoContent();
    }

    [HttpDelete("{rolId:int}/permisos/{permisoId:int}"), Authorize(Politicas.SuperAdmin)]
    public async Task<NoContentResult> QuitarPermiso(int rolId, int permisoId)
    {
        await roles.QuitarPermisoAsync(rolId, permisoId);
        return NoContent();
    }

    [HttpGet("usuario/{usuarioId:int}"), Authorize(Politicas.Admin)]
    public Task<List<RolAsignadoDto>> DeUsuario(int usuarioId) => roles.RolesDeUsuarioAsync(usuarioId);

    [HttpDelete("asignacion/{id:int}"), Authorize(Politicas.SuperAdmin)]
    public async Task<NoContentResult> QuitarAsignacion(int id)
    {
        await roles.QuitarAsignacionAsync(id);
        return NoContent();
    }
}

/// <summary>Parámetros del sistema. Corrección RT-04: en el proyecto base PUT y DELETE no exigían rol.</summary>
[Route("api/parametros"), Authorize(Politicas.SuperAdmin)]
public class ParametrosApi(RolesService roles) : ApiBase
{
    [HttpGet]
    public Task<List<ParametroDto>> Listar() => roles.ParametrosAsync();

    [HttpGet("{clave}")]
    public Task<ParametroDto> Obtener(string clave) => roles.ParametroAsync(clave);

    [HttpPut]
    public Task<ParametroDto> Guardar(ParametroForm f) => roles.GuardarParametroAsync(f);

    [HttpDelete("{id:int}")]
    public async Task<NoContentResult> Eliminar(int id)
    {
        await roles.EliminarParametroAsync(id);
        return NoContent();
    }
}
