using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record PerfilDto(
    string Nombres, string Apellidos, string? Telefono, DateOnly? FechaNacimiento, string? Genero, string? Ci,
    decimal? AlturaCm, string? NivelExperiencia, string? CondicionesMedicas, IReadOnlyList<string> DeportesFavoritos,
    string? AvatarUrl, string? NombreUsuario);

public record RolAsignadoDto(int Id, int RolId, string Rol, int Nivel, int? CadenaId, string? Cadena, int? SucursalId, string? Sucursal);
public record ClienteResumenDto(int Id, string Email, string Nombres, string Apellidos, string? Ci)
{
    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}

public record MembresiaDto(int Id, int CadenaId, string Cadena, int? SucursalId, string? Sucursal, bool Activa);

public record UsuarioDto(
    int Id, string Email, bool Activo, DateTime CreadoEn, string? Rol, int Nivel,
    int? CadenaId, string? Cadena, int? SucursalId, string? Sucursal, PerfilDto? Perfil,
    IReadOnlyList<RolAsignadoDto> Roles, IReadOnlyList<MembresiaDto> Membresias)
{
    public string NombreCompleto => Perfil is null ? Email : $"{Perfil.Nombres} {Perfil.Apellidos}".Trim();
}

public class UsuarioForm
{
    public string Email { get; set; } = "";
    public string? Password { get; set; }
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Ci { get; set; }
    public string? Genero { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public bool Activo { get; set; } = true;
    public int? RolId { get; set; }
    /// <summary>Cadena del gerente (solo la asigna el super administrador).</summary>
    public int? CadenaId { get; set; }
    /// <summary>Sucursal del personal, o sucursal habitual del cliente.</summary>
    public int? SucursalId { get; set; }
    /// <summary>Plan mensual inicial al registrar un socio desde el panel.</summary>
    public int? PlanId { get; set; }
}

public class FiltroUsuarios
{
    public string? Buscar { get; set; }
    public int? RolId { get; set; }
    public int? Nivel { get; set; }
    public int? SucursalId { get; set; }
    public string? Estado { get; set; }
    public string? Orden { get; set; }
    public int Limite { get; set; } = 20;
    public int Desde { get; set; }
}

public class PerfilForm
{
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? Telefono { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public int? Edad { get; set; }
    public string? Genero { get; set; }
    public string? Ci { get; set; }
    public decimal? AlturaCm { get; set; }
    public string? NivelExperiencia { get; set; }
    public string? CondicionesMedicas { get; set; }
    public List<string>? DeportesFavoritos { get; set; }
    public string? NombreUsuario { get; set; }
}

public class UsuariosService(AppDbContext db, UsuarioActual actual, Alcances alcances, Almacenamiento archivos, SuscripcionesService suscripciones)
{
    // ── Consultas ────────────────────────────────────────────────────────

    /// <summary>Usuarios del alcance del que consulta: todos, su cadena o su sucursal (como el proyecto base).</summary>
    public IQueryable<Usuario> UsuariosVisibles()
    {
        var q = db.Usuarios.AsQueryable();
        if (actual.EsSuperAdmin) return q;
        if (!actual.EsPersonal || actual.Nivel < Niveles.Recepcionista) return q.Where(_ => false);

        var cadena = actual.CadenaId;
        if (actual.SucursalFiltro is int sucursal)
            return q.Where(u => u.Roles.Any(r => r.SucursalId == sucursal) || u.Membresias.Any(m => m.SucursalId == sucursal));
        return q.Where(u => u.Roles.Any(r => r.CadenaId == cadena || r.Sucursal!.CadenaId == cadena) || u.Membresias.Any(m => m.CadenaId == cadena));
    }

    public async Task<Pagina<UsuarioDto>> ListarAsync(FiltroUsuarios f)
    {
        var q = UsuariosVisibles();
        if (!string.IsNullOrWhiteSpace(f.Buscar))
        {
            var t = f.Buscar.Trim();
            q = q.Where(u => u.Email.Contains(t) || u.Perfil!.Nombres.Contains(t) || u.Perfil.Apellidos.Contains(t) || (u.Perfil.Nombres + " " + u.Perfil.Apellidos).Contains(t) || u.Perfil.Ci == t);
        }
        if (f.RolId is not null) q = q.Where(u => u.Roles.Any(r => r.RolId == f.RolId));
        if (f.Nivel is not null) q = q.Where(u => u.Roles.Any(r => r.Rol!.Nivel == f.Nivel));
        if (f.SucursalId is not null) q = q.Where(u => u.Roles.Any(r => r.SucursalId == f.SucursalId) || u.Membresias.Any(m => m.SucursalId == f.SucursalId));
        if (f.Estado == "activos") q = q.Where(u => u.Activo);
        else if (f.Estado == "inactivos") q = q.Where(u => !u.Activo);

        q = f.Orden switch
        {
            "za" => q.OrderByDescending(u => u.Perfil!.Apellidos).ThenByDescending(u => u.Perfil!.Nombres),
            "id_asc" => q.OrderBy(u => u.Id),
            "id_desc" => q.OrderByDescending(u => u.Id),
            _ => q.OrderBy(u => u.Perfil!.Apellidos).ThenBy(u => u.Perfil!.Nombres),
        };
        var total = await q.CountAsync();
        var limite = Math.Clamp(f.Limite, 1, 500);
        var pagina = await Completo(q).Skip(f.Desde).Take(limite).ToListAsync();
        return new Pagina<UsuarioDto>(pagina.Select(ADto).ToList(), total, limite, f.Desde);
    }

    public async Task<UsuarioDto> ObtenerAsync(int id)
    {
        var u = await Completo(db.Usuarios).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw ErrorNegocio.NoEncontrado($"Usuario con ID {id} no encontrado");
        return ADto(u);
    }

    public async Task<UsuarioDto> ObtenerVisibleAsync(int id)
    {
        if (id != actual.Id && actual.Nivel >= Niveles.Recepcionista && !await UsuariosVisibles().AnyAsync(u => u.Id == id))
            throw ErrorNegocio.Prohibido("No tienes acceso a este usuario.");
        return await ObtenerAsync(id);
    }

    /// <summary>Clientes activos para iniciar un chat o asignar reservas (searchClientUsers).</summary>
    public async Task<List<ClienteResumenDto>> BuscarClientesAsync(string? texto, int limite = 20)
    {
        var q = db.Usuarios.Where(u => u.Activo && u.Roles.Any(r => r.Rol!.Nivel == Niveles.Cliente));
        if (actual.EsPersonal && actual.CadenaId is int cadena)
            q = q.Where(u => u.Membresias.Any(m => m.CadenaId == cadena));
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = texto.Trim();
            q = q.Where(u => u.Email.Contains(t) || u.Perfil!.Nombres.Contains(t) || u.Perfil.Apellidos.Contains(t) || u.Perfil.Ci == t);
        }
        return await q.OrderBy(u => u.Perfil!.Apellidos).Take(limite)
            .Select(u => new ClienteResumenDto(u.Id, u.Email, u.Perfil!.Nombres, u.Perfil.Apellidos, u.Perfil.Ci))
            .ToListAsync();
    }

    // ── Altas, cambios y bajas (CU-06) ───────────────────────────────────

    public async Task<UsuarioDto> CrearAsync(UsuarioForm f, bool registroPublico = false)
    {
        var email = f.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) throw ErrorNegocio.Solicitud("El correo no es válido.");
        if (string.IsNullOrWhiteSpace(f.Password) || f.Password.Length < 6) throw ErrorNegocio.Solicitud("La contraseña debe tener al menos 6 caracteres.");
        if (string.IsNullOrWhiteSpace(f.Nombres) || string.IsNullOrWhiteSpace(f.Apellidos)) throw ErrorNegocio.Solicitud("Nombres y apellidos son obligatorios.");
        if (!string.IsNullOrWhiteSpace(f.Ci) && await db.Perfiles.AnyAsync(p => p.Ci == f.Ci.Trim()))
            throw ErrorNegocio.Solicitud("El Carnet de Identidad (CI) ya está registrado en otra cuenta.");
        if (await db.Usuarios.AnyAsync(u => u.Email == email))
            throw ErrorNegocio.Conflicto(registroPublico
                ? $"El usuario {email} ya se encuentra registrado. Por favor inicie sesión."
                : "Ya existe un usuario con este email");

        var rol = registroPublico
            ? await db.Roles.FirstAsync(r => r.Nombre == Roles.Cliente)
            : await db.Roles.FindAsync(f.RolId ?? 0) ?? throw ErrorNegocio.NoEncontrado($"Rol {f.RolId} no encontrado.");

        if (!registroPublico && rol.Nombre == Roles.Cliente && string.IsNullOrWhiteSpace(f.Ci))
            throw ErrorNegocio.Solicitud("El Carnet de Identidad (CI) es obligatorio para registrar un socio.");

        var registrarSocio = !registroPublico && rol.Nombre == Roles.Cliente;
        if (registrarSocio)
        {
            if (f.PlanId is null)
                throw ErrorNegocio.Solicitud("Selecciona un plan para registrar al socio.");
            if (!(await suscripciones.PlanesAsync()).Any(p => p.Id == f.PlanId))
                throw ErrorNegocio.Solicitud("El plan seleccionado no está activo o no está disponible para tu cadena.");
            if ((actual.SucursalFiltro ?? f.SucursalId) is null)
                throw ErrorNegocio.Solicitud("Selecciona la sucursal habitual del socio.");
            if (!f.Activo)
                throw ErrorNegocio.Solicitud("El socio debe estar activo para registrar su suscripción inicial.");
        }

        var u = new Usuario
        {
            Email = email,
            PasswordHash = ServicioAutenticacion.Hashear(f.Password),
            Activo = registroPublico || f.Activo,
            Perfil = new PerfilUsuario
            {
                Nombres = f.Nombres.Trim(),
                Apellidos = f.Apellidos.Trim(),
                Telefono = f.Telefono,
                Ci = string.IsNullOrWhiteSpace(f.Ci) ? null : f.Ci.Trim(),
                Genero = f.Genero,
                FechaNacimiento = f.FechaNacimiento,
            },
        };
        if (!registroPublico) await AsignarRolAsync(u, rol, f);
        else u.Roles.Add(new UsuarioRol { RolId = rol.Id });

        // El alta del socio y su suscripción se confirman juntas.
        await using var transaccion = registrarSocio ? await db.Database.BeginTransactionAsync() : null;
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        if (registrarSocio)
        {
            await suscripciones.CrearAsync(new SuscripcionForm
            {
                UsuarioId = u.Id,
                PlanId = f.PlanId!.Value,
                SucursalPrincipalId = actual.SucursalFiltro ?? f.SucursalId,
                Inicio = Reloj.Hoy,
                Meses = 1,
            });
        }
        var creado = await ObtenerAsync(u.Id);
        if (transaccion is not null) await transaccion.CommitAsync();
        return creado;
    }

    public async Task<UsuarioDto> ActualizarAsync(int id, UsuarioForm f)
    {
        var u = await db.Usuarios.Include(x => x.Perfil).Include(x => x.Roles).ThenInclude(r => r.Rol).Include(x => x.Membresias)
                    .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw ErrorNegocio.NoEncontrado($"Usuario con ID {id} no encontrado");
        var propio = id == actual.Id;
        var cambioEstructural = f.RolId is not null && f.RolId != u.Roles.OrderByDescending(r => r.Rol!.Nivel).FirstOrDefault()?.RolId
                                || f.Activo != u.Activo;

        if (propio && cambioEstructural)
            throw ErrorNegocio.Prohibido("Operación prohibida: No puedes modificar tu propio nivel, jurisdicción o estado contractual.");
        if (!propio)
        {
            await ExigirVisibleAsync(id);
            if (!actual.EsSuperAdmin && NivelDe(u) >= actual.Nivel)
                throw ErrorNegocio.Prohibido("No puedes editar a un usuario de nivel igual o superior al tuyo.");
        }

        var email = f.Email.Trim().ToLowerInvariant();
        if (email != u.Email)
        {
            if (await db.Usuarios.AnyAsync(x => x.Email == email && x.Id != id)) throw ErrorNegocio.Conflicto("Ya existe un usuario con este email");
            u.Email = email;
        }
        var ci = string.IsNullOrWhiteSpace(f.Ci) ? null : f.Ci.Trim();
        if (ci is not null && await db.Perfiles.AnyAsync(p => p.Ci == ci && p.UsuarioId != id))
            throw ErrorNegocio.Solicitud("El Carnet de Identidad (CI) ya está registrado en otra cuenta.");
        if (!string.IsNullOrWhiteSpace(f.Password)) u.PasswordHash = ServicioAutenticacion.Hashear(f.Password);

        u.Perfil ??= new PerfilUsuario();
        u.Perfil.Nombres = f.Nombres.Trim();
        u.Perfil.Apellidos = f.Apellidos.Trim();
        u.Perfil.Telefono = f.Telefono;
        u.Perfil.Ci = ci;
        u.Perfil.Genero = f.Genero;
        u.Perfil.FechaNacimiento = f.FechaNacimiento ?? u.Perfil.FechaNacimiento;
        u.ActualizadoEn = DateTime.UtcNow;

        if (!propio)
        {
            u.Activo = f.Activo;
            if (f.RolId is not null)
            {
                var rol = await db.Roles.FindAsync(f.RolId.Value) ?? throw ErrorNegocio.NoEncontrado($"Rol {f.RolId} no encontrado.");
                db.UsuarioRoles.RemoveRange(u.Roles);
                u.Roles.Clear();
                await AsignarRolAsync(u, rol, f);
            }
        }
        await db.SaveChangesAsync();
        return await ObtenerAsync(id);
    }

    public async Task EliminarAsync(int id)
    {
        if (id == actual.Id) throw ErrorNegocio.Prohibido("Medida de seguridad: No puedes eliminar tu propia cuenta.");
        if (actual.Nivel < Niveles.Recepcionista) throw ErrorNegocio.Prohibido("Tu nivel jerárquico no permite eliminar usuarios.");
        var u = await db.Usuarios.Include(x => x.Roles).ThenInclude(r => r.Rol).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw ErrorNegocio.NoEncontrado($"Usuario con ID {id} no encontrado o ya eliminado.");
        if (NivelDe(u) >= actual.Nivel)
            throw ErrorNegocio.Prohibido("No puedes eliminar a un usuario de nivel igual o superior al tuyo.");
        if (!actual.EsSuperAdmin && !await UsuariosVisibles().AnyAsync(x => x.Id == id))
            throw ErrorNegocio.Prohibido(actual.SucursalFiltro is null
                ? "No tienes permisos para eliminar usuarios fuera de tu marca."
                : "No tienes permisos para eliminar usuarios de otra sucursal.");

        // Datos personales sin otra dependencia (como el proyecto base).
        await db.Metricas.Where(m => m.UsuarioId == id).ExecuteDeleteAsync();
        await db.Notificaciones.Where(n => n.UsuarioId == id).ExecuteDeleteAsync();
        await db.PreferenciasNotificacion.Where(n => n.UsuarioId == id).ExecuteDeleteAsync();
        await db.PerfilesEntrenamiento.Where(n => n.UsuarioId == id).ExecuteDeleteAsync();
        await db.Restricciones.Where(n => n.UsuarioId == id).ExecuteDeleteAsync();
        db.Usuarios.Remove(u);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw ErrorNegocio.Conflicto("El usuario tiene historial asociado (reservas, accesos o entrenamientos). Desactívalo en lugar de eliminarlo.");
        }
    }

    // ── Perfil propio (CU-07) ────────────────────────────────────────────

    public async Task<PerfilDto> ActualizarMiPerfilAsync(PerfilForm f)
    {
        var p = await db.Perfiles.FirstOrDefaultAsync(x => x.UsuarioId == actual.Id);
        if (p is null)
        {
            var u = await db.Usuarios.FindAsync(actual.Id) ?? throw ErrorNegocio.NoEncontrado("Usuario no encontrado. Por favor vuelve a iniciar sesión.");
            p = new PerfilUsuario { UsuarioId = u.Id, Nombres = u.Email.Split('@')[0] };
            db.Perfiles.Add(p);
        }
        if (f.Nombres is not null) p.Nombres = f.Nombres;
        if (f.Apellidos is not null) p.Apellidos = f.Apellidos;
        if (f.Telefono is not null) p.Telefono = f.Telefono;
        if (f.Genero is not null) p.Genero = f.Genero;
        if (f.AlturaCm is not null) p.AlturaCm = f.AlturaCm;
        if (f.NivelExperiencia is not null) p.NivelExperiencia = f.NivelExperiencia;
        if (f.CondicionesMedicas is not null) p.CondicionesMedicas = f.CondicionesMedicas;
        if (f.DeportesFavoritos is not null) p.DeportesFavoritos = f.DeportesFavoritos;
        if (f.FechaNacimiento is not null) p.FechaNacimiento = f.FechaNacimiento;
        else if (f.Edad is > 0) p.FechaNacimiento = new DateOnly(DateTime.Today.Year - f.Edad.Value, 1, 1);
        if (f.Ci is not null)
        {
            if (await db.Perfiles.AnyAsync(x => x.Ci == f.Ci && x.UsuarioId != actual.Id)) throw ErrorNegocio.Solicitud("Ese carnet de identidad ya está registrado.");
            p.Ci = f.Ci;
        }
        if (f.NombreUsuario is not null)
        {
            if (await db.Perfiles.AnyAsync(x => x.NombreUsuario == f.NombreUsuario && x.UsuarioId != actual.Id)) throw ErrorNegocio.Solicitud("El nombre de usuario ya está en uso.");
            p.NombreUsuario = f.NombreUsuario;
        }
        await db.SaveChangesAsync();
        return APerfil(p);
    }

    public async Task<string> GuardarAvatarAsync(IFormFile archivo)
    {
        var p = await db.Perfiles.FirstOrDefaultAsync(x => x.UsuarioId == actual.Id) ?? throw ErrorNegocio.NoEncontrado("Perfil no encontrado.");
        archivos.Eliminar(p.AvatarUrl);
        p.AvatarUrl = await archivos.GuardarImagenAsync(archivo, "avatares");
        await db.SaveChangesAsync();
        return p.AvatarUrl;
    }

    /// <summary>Datos públicos de un usuario para el chat (getChatProfile).</summary>
    public async Task<object> PerfilPublicoAsync(int id)
    {
        var u = await Completo(db.Usuarios).FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado("Perfil no encontrado.");
        var dto = ADto(u);
        var esCliente = dto.Nivel <= Niveles.Cliente;
        return new
        {
            id = u.Id,
            nombres = u.Perfil?.Nombres ?? "",
            apellidos = u.Perfil?.Apellidos ?? "",
            genero = u.Perfil?.Genero ?? "No especificado",
            email = u.Email,
            rol = dto.Rol,
            nivel = dto.Nivel,
            cadena = esCliente ? null : dto.Cadena,
            sucursal = esCliente ? null : dto.Sucursal,
            avatarUrl = u.Perfil?.AvatarUrl,
        };
    }

    // ── Apoyo ────────────────────────────────────────────────────────────

    /// <summary>
    /// Asigna el rol con su alcance aplicando la jerarquía: super administrador sin cadena,
    /// gerente con cadena, personal con sucursal y cliente con membresía opcional.
    /// </summary>
    private async Task AsignarRolAsync(Usuario u, Rol rol, UsuarioForm f)
    {
        if (!actual.EsSuperAdmin && rol.Nivel >= actual.Nivel)
            throw ErrorNegocio.Prohibido("No puedes asignar un rol igual o superior a tu propio nivel jerárquico.");

        if (rol.Nivel >= Niveles.SuperAdmin)
        {
            u.Roles.Add(new UsuarioRol { RolId = rol.Id, AsignadoPorId = actual.Id });
            return;
        }
        if (rol.Nombre == Roles.Gerente)
        {
            var cadenaId = actual.EsSuperAdmin ? f.CadenaId : actual.CadenaId;
            if (cadenaId is null || !await db.Cadenas.AnyAsync(c => c.Id == cadenaId))
                throw ErrorNegocio.Solicitud("Este nivel jerárquico requiere obligatoriamente una Marca o Sucursal.");
            u.Roles.Add(new UsuarioRol { RolId = rol.Id, CadenaId = cadenaId, AsignadoPorId = actual.Id });
            return;
        }

        int? sucursalId = actual.SucursalFiltro ?? f.SucursalId;
        if (rol.Nombre == Roles.Cliente)
        {
            u.Roles.Add(new UsuarioRol { RolId = rol.Id, AsignadoPorId = actual.Id });
            if (sucursalId is not null)
            {
                var s = await alcances.ExigirSucursalAsync(sucursalId.Value, "No tienes permisos para asignar usuarios a una sucursal de otra Marca.");
                if (!u.Membresias.Any(m => m.CadenaId == s.CadenaId))
                    u.Membresias.Add(new Membresia { CadenaId = s.CadenaId, SucursalId = s.Id });
            }
            return;
        }

        if (sucursalId is null)
            throw ErrorNegocio.Solicitud(actual.EsGerente
                ? "Como Gerente, debes seleccionar una Sucursal válida de tu Marca."
                : "Este nivel jerárquico requiere obligatoriamente una Marca o Sucursal.");
        var sucursal = await alcances.ExigirSucursalAsync(sucursalId.Value, "No tienes permisos para asignar usuarios a una sucursal de otra Marca.");
        u.Roles.Add(new UsuarioRol { RolId = rol.Id, CadenaId = sucursal.CadenaId, SucursalId = sucursal.Id, AsignadoPorId = actual.Id });
    }

    private async Task ExigirVisibleAsync(int id)
    {
        if (!actual.EsSuperAdmin && !await UsuariosVisibles().AnyAsync(x => x.Id == id))
            throw ErrorNegocio.Prohibido("No tienes acceso a este usuario.");
    }

    private static int NivelDe(Usuario u) => u.Roles.Select(r => r.Rol?.Nivel ?? 0).DefaultIfEmpty(0).Max();

    public static IQueryable<Usuario> Completo(IQueryable<Usuario> q) =>
        q.Include(u => u.Perfil)
            .Include(u => u.Roles).ThenInclude(r => r.Rol)
            .Include(u => u.Roles).ThenInclude(r => r.Cadena)
            .Include(u => u.Roles).ThenInclude(r => r.Sucursal).ThenInclude(s => s!.Cadena)
            .Include(u => u.Membresias).ThenInclude(m => m.Cadena)
            .Include(u => u.Membresias).ThenInclude(m => m.Sucursal)
            .AsSplitQuery()
            .IgnoreQueryFilters();

    public static PerfilDto APerfil(PerfilUsuario p) => new(
        p.Nombres, p.Apellidos, p.Telefono, p.FechaNacimiento, p.Genero, p.Ci, p.AlturaCm, p.NivelExperiencia,
        p.CondicionesMedicas, p.DeportesFavoritos, p.AvatarUrl, p.NombreUsuario);

    public static UsuarioDto ADto(Usuario u)
    {
        var roles = u.Roles.OrderByDescending(r => r.Rol?.Nivel ?? 0).Select(r => new RolAsignadoDto(
            r.Id, r.RolId, r.Rol?.Nombre ?? "", r.Rol?.Nivel ?? 0,
            r.CadenaId ?? r.Sucursal?.CadenaId, r.Cadena?.Nombre ?? r.Sucursal?.Cadena?.Nombre,
            r.SucursalId, r.Sucursal?.Nombre)).ToList();
        var membresias = u.Membresias.Select(m => new MembresiaDto(m.Id, m.CadenaId, m.Cadena?.Nombre ?? "", m.SucursalId, m.Sucursal?.Nombre, m.Activa)).ToList();
        var top = roles.FirstOrDefault();
        var m0 = membresias.FirstOrDefault(m => m.Activa);
        var esCliente = top?.Rol == Roles.Cliente;
        return new UsuarioDto(
            u.Id, u.Email, u.Activo, u.CreadoEn, top?.Rol, top?.Nivel ?? 0,
            esCliente ? m0?.CadenaId : top?.CadenaId, esCliente ? m0?.Cadena : top?.Cadena,
            esCliente ? m0?.SucursalId : top?.SucursalId, esCliente ? m0?.Sucursal : top?.Sucursal,
            u.Perfil is null ? null : APerfil(u.Perfil), roles, membresias);
    }
}
