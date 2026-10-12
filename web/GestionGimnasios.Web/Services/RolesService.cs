using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record PermisoDto(int Id, string Codigo, string Nombre, string? Descripcion, string Recurso, string Accion, bool Activo);
public record RolDto(int Id, string Nombre, string? Descripcion, int Nivel, bool EsDeSistema, bool Activo, int Usuarios, IReadOnlyList<PermisoDto> Permisos);
public record ParametroDto(int Id, string Clave, string Valor, string? Descripcion, DateTime ActualizadoEn);

public class RolForm
{
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int Nivel { get; set; } = 1;
    public bool Activo { get; set; } = true;
}

public class PermisoForm
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Recurso { get; set; } = "";
    public string Accion { get; set; } = "";
}

public class ParametroForm
{
    public string Clave { get; set; } = "";
    public string Valor { get; set; } = "";
    public string? Descripcion { get; set; }
}

/// <summary>Roles, permisos y parámetros del sistema (CU-04).</summary>
public class RolesService(AppDbContext db, UsuarioActual actual)
{
    public async Task<List<RolDto>> RolesAsync()
    {
        var roles = await db.Roles.Include(r => r.Permisos).ThenInclude(p => p.Permiso).OrderByDescending(r => r.Nivel).ThenBy(r => r.Nombre).ToListAsync();
        var conteo = await db.UsuarioRoles.IgnoreQueryFilters().GroupBy(r => r.RolId).Select(g => new { g.Key, N = g.Select(x => x.UsuarioId).Distinct().Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        return roles.Select(r => ADto(r, conteo.GetValueOrDefault(r.Id))).ToList();
    }

    public async Task<RolDto> RolAsync(int id)
    {
        var r = await db.Roles.Include(x => x.Permisos).ThenInclude(p => p.Permiso).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw ErrorNegocio.NoEncontrado($"Rol {id} no encontrado");
        return ADto(r, await db.UsuarioRoles.IgnoreQueryFilters().CountAsync(x => x.RolId == id));
    }

    /// <summary>Roles que el usuario actual puede asignar (de nivel menor al suyo).</summary>
    public Task<List<RolDto>> RolesAsignablesAsync() =>
        db.Roles.Where(r => r.Activo && (actual.EsSuperAdmin || r.Nivel < actual.Nivel))
            .OrderByDescending(r => r.Nivel)
            .Select(r => new RolDto(r.Id, r.Nombre, r.Descripcion, r.Nivel, r.EsDeSistema, r.Activo, 0, new List<PermisoDto>()))
            .ToListAsync();

    public async Task<RolDto> CrearRolAsync(RolForm f)
    {
        var nombre = f.Nombre.Trim().ToUpperInvariant().Replace(' ', '_');
        if (string.IsNullOrEmpty(nombre)) throw ErrorNegocio.Solicitud("El nombre del rol es obligatorio.");
        if (await db.Roles.AnyAsync(r => r.Nombre == nombre)) throw ErrorNegocio.Conflicto("Ya existe un rol con ese nombre.");
        if (f.Nivel is < 1 or > 9) throw ErrorNegocio.Solicitud("El nivel de un rol nuevo debe estar entre 1 y 9.");
        var r = new Rol { Nombre = nombre, Descripcion = f.Descripcion, Nivel = f.Nivel, Activo = f.Activo };
        db.Roles.Add(r);
        await db.SaveChangesAsync();
        return await RolAsync(r.Id);
    }

    public async Task<RolDto> ActualizarRolAsync(int id, RolForm f)
    {
        var r = await db.Roles.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado($"Rol {id} no encontrado");
        var nombre = f.Nombre.Trim().ToUpperInvariant().Replace(' ', '_');
        if (nombre != r.Nombre)
        {
            if (r.EsDeSistema) throw ErrorNegocio.Conflicto("No se puede renombrar un rol de sistema.");
            if (await db.Roles.AnyAsync(x => x.Nombre == nombre)) throw ErrorNegocio.Conflicto("Ya existe un rol con ese nombre.");
            r.Nombre = nombre;
        }
        r.Descripcion = f.Descripcion;
        if (!r.EsDeSistema) r.Nivel = Math.Clamp(f.Nivel, 1, 9);
        r.Activo = f.Activo;
        r.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await RolAsync(id);
    }

    public async Task EliminarRolAsync(int id)
    {
        var r = await db.Roles.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado($"Rol {id} no encontrado");
        if (r.EsDeSistema) throw ErrorNegocio.Conflicto("No se pueden eliminar roles de sistema.");
        if (await db.UsuarioRoles.IgnoreQueryFilters().AnyAsync(x => x.RolId == id))
            throw ErrorNegocio.Conflicto("El rol está asignado a usuarios. Reasígnalos antes de eliminarlo.");
        db.Roles.Remove(r);
        await db.SaveChangesAsync();
    }

    public Task<List<PermisoDto>> PermisosAsync() =>
        db.Permisos.OrderBy(p => p.Recurso).ThenBy(p => p.Accion)
            .Select(p => new PermisoDto(p.Id, p.Codigo, p.Nombre, p.Descripcion, p.Recurso, p.Accion, p.Activo)).ToListAsync();

    public async Task<PermisoDto> CrearPermisoAsync(PermisoForm f)
    {
        var codigo = f.Codigo.Trim().ToUpperInvariant();
        if (await db.Permisos.AnyAsync(p => p.Codigo == codigo)) throw ErrorNegocio.Conflicto("Ya existe un permiso con ese código.");
        var p = new Permiso { Codigo = codigo, Nombre = f.Nombre, Descripcion = f.Descripcion, Recurso = f.Recurso, Accion = f.Accion };
        db.Permisos.Add(p);
        await db.SaveChangesAsync();
        return new PermisoDto(p.Id, p.Codigo, p.Nombre, p.Descripcion, p.Recurso, p.Accion, p.Activo);
    }

    public async Task AsignarPermisoAsync(int rolId, int permisoId)
    {
        if (!await db.Roles.AnyAsync(r => r.Id == rolId)) throw ErrorNegocio.NoEncontrado($"Rol {rolId} no encontrado");
        if (!await db.Permisos.AnyAsync(p => p.Id == permisoId)) throw ErrorNegocio.NoEncontrado($"Permiso {permisoId} no encontrado");
        if (await db.RolPermisos.AnyAsync(x => x.RolId == rolId && x.PermisoId == permisoId)) return;
        db.RolPermisos.Add(new RolPermiso { RolId = rolId, PermisoId = permisoId, OtorgadoPorId = actual.Id });
        await db.SaveChangesAsync();
    }

    public async Task QuitarPermisoAsync(int rolId, int permisoId)
    {
        await db.RolPermisos.Where(x => x.RolId == rolId && x.PermisoId == permisoId).ExecuteDeleteAsync();
    }

    public async Task<List<RolAsignadoDto>> RolesDeUsuarioAsync(int usuarioId) =>
        (await db.UsuarioRoles.IgnoreQueryFilters().Where(r => r.UsuarioId == usuarioId)
            .Include(r => r.Rol).Include(r => r.Cadena).Include(r => r.Sucursal).ToListAsync())
        .Select(r => new RolAsignadoDto(r.Id, r.RolId, r.Rol!.Nombre, r.Rol.Nivel, r.CadenaId, r.Cadena?.Nombre, r.SucursalId, r.Sucursal?.Nombre))
        .ToList();

    public async Task QuitarAsignacionAsync(int usuarioRolId)
    {
        var ur = await db.UsuarioRoles.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == usuarioRolId)
                 ?? throw ErrorNegocio.NoEncontrado("Asignación no encontrada.");
        if (ur.UsuarioId == actual.Id) throw ErrorNegocio.Prohibido("No puedes quitarte tus propios roles.");
        db.UsuarioRoles.Remove(ur);
        await db.SaveChangesAsync();
    }

    // ── Parámetros del sistema ───────────────────────────────────────────

    public Task<List<ParametroDto>> ParametrosAsync() =>
        db.Parametros.OrderBy(p => p.Clave).Select(p => new ParametroDto(p.Id, p.Clave, p.Valor, p.Descripcion, p.ActualizadoEn)).ToListAsync();

    public async Task<ParametroDto> ParametroAsync(string clave) =>
        await db.Parametros.Where(p => p.Clave == clave).Select(p => new ParametroDto(p.Id, p.Clave, p.Valor, p.Descripcion, p.ActualizadoEn)).FirstOrDefaultAsync()
        ?? throw ErrorNegocio.NoEncontrado($"Parámetro {clave} no encontrado");

    public async Task<ParametroDto> GuardarParametroAsync(ParametroForm f)
    {
        var clave = f.Clave.Trim();
        if (string.IsNullOrEmpty(clave)) throw ErrorNegocio.Solicitud("La clave es obligatoria.");
        var p = await db.Parametros.FirstOrDefaultAsync(x => x.Clave == clave);
        if (p is null)
        {
            p = new ParametroSistema { Clave = clave };
            db.Parametros.Add(p);
        }
        p.Valor = f.Valor;
        p.Descripcion = f.Descripcion ?? p.Descripcion;
        p.ActualizadoEn = DateTime.UtcNow;
        p.ActualizadoPorId = actual.Id;
        await db.SaveChangesAsync();
        return new ParametroDto(p.Id, p.Clave, p.Valor, p.Descripcion, p.ActualizadoEn);
    }

    public async Task EliminarParametroAsync(int id)
    {
        await db.Parametros.Where(p => p.Id == id).ExecuteDeleteAsync();
    }

    private static RolDto ADto(Rol r, int usuarios) => new(
        r.Id, r.Nombre, r.Descripcion, r.Nivel, r.EsDeSistema, r.Activo, usuarios,
        r.Permisos.Where(p => p.Permiso is not null).Select(p => new PermisoDto(p.Permiso!.Id, p.Permiso.Codigo, p.Permiso.Nombre, p.Permiso.Descripcion, p.Permiso.Recurso, p.Permiso.Accion, p.Permiso.Activo)).ToList());
}
