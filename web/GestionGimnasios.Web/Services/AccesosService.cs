using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record VistaPreviaAcceso(int UsuarioId, string NombreCompleto, string Rol, string Sucursal, bool Dentro, string? AvatarUrl);
public record AccesoDto(
    int Id, int UsuarioId, string Usuario, string Email, string Rol, int SucursalId, string Sucursal,
    DateTime Ingreso, DateTime? Salida, string Metodo, string Estado, int? MinutosDentro);

/// <summary>
/// Control de acceso a la sucursal (CU-26, CU-27). El código del carnet virtual es el número de
/// usuario, como en el proyecto base. Ingresa el personal de la sucursal y los socios con
/// membresía en la cadena.
/// </summary>
public class AccesosService(AppDbContext db, UsuarioActual actual, Alcances alcances, NotificacionesService notificaciones)
{
    /// <summary>Datos de la persona antes de confirmar su ingreso o salida (scanPreview).</summary>
    public async Task<VistaPreviaAcceso> VistaPreviaAsync(string codigo)
    {
        var (u, sucursal, rol) = await ResolverAsync(codigo);
        var dentro = await db.Accesos.AnyAsync(a => a.UsuarioId == u.Id && a.SucursalId == sucursal.Id && a.Estado == EstadosAcceso.Activo);
        return new VistaPreviaAcceso(u.Id, u.Perfil?.NombreCompleto ?? u.Email, rol, sucursal.Nombre, dentro, u.Perfil?.AvatarUrl);
    }

    /// <summary>Registra el ingreso, o la salida si la persona ya estaba dentro.</summary>
    public async Task<AccesoDto> RegistrarAsync(string codigo, string metodo = "MANUAL")
    {
        var (u, sucursal, _) = await ResolverAsync(codigo);
        var abierto = await db.Accesos.FirstOrDefaultAsync(a => a.UsuarioId == u.Id && a.SucursalId == sucursal.Id && a.Estado == EstadosAcceso.Activo);
        if (abierto is not null)
        {
            abierto.Salida = DateTime.UtcNow;
            abierto.Estado = EstadosAcceso.Completado;
            await db.SaveChangesAsync();
            return (await Consulta(db.Accesos.Where(a => a.Id == abierto.Id))).First();
        }
        var acceso = new Acceso
        {
            CadenaId = sucursal.CadenaId, UsuarioId = u.Id, SucursalId = sucursal.Id,
            Metodo = metodo is "QR" ? "QR" : "MANUAL", RegistradoPorId = actual.Id,
        };
        db.Accesos.Add(acceso);
        await db.SaveChangesAsync();
        await notificaciones.NotificarAsync(u.Id, TiposNotificacion.IngresoRegistrado, "Ingreso registrado", $"Registramos tu ingreso a {sucursal.Nombre}.");
        return (await Consulta(db.Accesos.Where(a => a.Id == acceso.Id))).First();
    }

    public async Task<AccesoDto> RegistrarSalidaAsync(int id)
    {
        var a = await db.Accesos.FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado($"Check-in {id} no encontrado");
        await alcances.ExigirSucursalAsync(a.SucursalId);
        if (a.Estado == EstadosAcceso.Activo)
        {
            a.Salida = DateTime.UtcNow;
            a.Estado = EstadosAcceso.Completado;
            await db.SaveChangesAsync();
        }
        return (await Consulta(db.Accesos.Where(x => x.Id == id))).First();
    }

    /// <summary>Auditoría filtrable por estado, sucursal y rango de fechas.</summary>
    public async Task<Pagina<AccesoDto>> AuditoriaAsync(string? estado = null, int? sucursalId = null, DateOnly? desde = null, DateOnly? hasta = null, string? buscar = null, string? tipo = null, int limite = 50, int salto = 0)
    {
        var visibles = alcances.SucursalesGestionables().Select(s => s.Id);
        var q = db.Accesos.Where(a => visibles.Contains(a.SucursalId));
        if (!string.IsNullOrEmpty(estado)) q = q.Where(a => a.Estado == estado);
        if (sucursalId is not null) q = q.Where(a => a.SucursalId == sucursalId);
        if (desde is not null) { var d = Reloj.InicioDelDiaUtc(desde.Value); q = q.Where(a => a.Ingreso >= d); }
        if (hasta is not null) { var h = Reloj.InicioDelDiaUtc(hasta.Value.AddDays(1)); q = q.Where(a => a.Ingreso < h); }
        if (!string.IsNullOrWhiteSpace(buscar)) q = q.Where(a => a.Usuario!.Email.Contains(buscar) || (a.Usuario.Perfil!.Nombres + " " + a.Usuario.Perfil.Apellidos).Contains(buscar));
        if (tipo == "PERSONAL") q = q.Where(a => a.Usuario!.Roles.Any(r => r.Rol!.Nivel > Niveles.Cliente));
        else if (tipo == "CLIENTES") q = q.Where(a => a.Usuario!.Roles.All(r => r.Rol!.Nivel <= Niveles.Cliente));
        var total = await q.CountAsync();
        var lista = await Consulta(q.OrderByDescending(a => a.Ingreso).Skip(salto).Take(Math.Clamp(limite, 1, 500)));
        return new Pagina<AccesoDto>(lista, total, limite, salto);
    }

    /// <summary>Historial de accesos registrados por el usuario actual o propios (historial de la app).</summary>
    public async Task<List<AccesoDto>> HistorialAsync(int limite = 50)
    {
        var q = actual.Nivel >= Niveles.Recepcionista
            ? db.Accesos.Where(a => a.RegistradoPorId == actual.Id)
            : db.Accesos.IgnoreQueryFilters().Where(a => a.UsuarioId == actual.Id);
        return await Consulta(q.OrderByDescending(a => a.Ingreso).Take(limite));
    }

    /// <summary>Cierra los ingresos sin salida de más de 4 horas (tarea programada cada 30 minutos).</summary>
    public static Task<int> CerrarVencidosAsync(AppDbContext db, int horas = 4)
    {
        var limite = DateTime.UtcNow.AddHours(-horas);
        return db.Accesos.IgnoreQueryFilters().Where(a => a.Estado == EstadosAcceso.Activo && a.Salida == null && a.Ingreso < limite)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Salida, DateTime.UtcNow).SetProperty(a => a.Estado, EstadosAcceso.Completado));
    }

    // ── Apoyo ────────────────────────────────────────────────────────────

    private async Task<(Usuario Usuario, Sucursal Sucursal, string Rol)> ResolverAsync(string codigo)
    {
        if (!int.TryParse(codigo?.Trim(), out var id) || id <= 0) throw ErrorNegocio.Prohibido("Número de socio no válido.");
        if (actual.CadenaId is null) throw ErrorNegocio.Prohibido("No tienes una sucursal o marca asignada.");

        var u = await db.Usuarios.IgnoreQueryFilters().Include(x => x.Perfil).Include(x => x.Roles).ThenInclude(r => r.Rol)
                    .Include(x => x.Membresias).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw ErrorNegocio.NoEncontrado("Usuario no encontrado o sin rol asignado.");

        // Recepción registra en su sucursal; el gerente (alcance de cadena) en la sucursal de la persona.
        var sucursalId = actual.SucursalId
                         ?? u.Roles.Where(r => r.CadenaId == actual.CadenaId && r.SucursalId != null).Select(r => r.SucursalId).FirstOrDefault()
                         ?? u.Membresias.Where(m => m.CadenaId == actual.CadenaId).Select(m => m.SucursalId).FirstOrDefault()
                         ?? throw ErrorNegocio.Prohibido("El personal no pertenece a tu marca. Sin permisos para registrar su ingreso.");
        var sucursal = await alcances.ExigirSucursalAsync(sucursalId);
        if (!u.Activo) throw ErrorNegocio.Prohibido("La cuenta está desactivada.");
        var top = u.Roles.OrderByDescending(r => r.Rol!.Nivel).FirstOrDefault() ?? throw ErrorNegocio.NoEncontrado("Usuario no encontrado o sin rol asignado.");

        if (top.Rol!.Nombre == Roles.Cliente)
        {
            if (!u.Membresias.Any(m => m.CadenaId == sucursal.CadenaId && m.Activa))
                throw ErrorNegocio.Prohibido("El socio no tiene membresía en esta cadena.");
        }
        else
        {
            if (top.Rol.Nivel is < Niveles.Staff or > Niveles.Recepcionista) throw ErrorNegocio.Prohibido("Solo personal autorizado puede registrar ingreso.");
            if (actual.Nivel <= Niveles.Recepcionista && top.Rol.Nivel >= actual.Nivel && u.Id != actual.Id)
                throw ErrorNegocio.Prohibido("Tu rol no tiene permisos para escanear personal.");
            if (!u.Roles.Any(r => r.SucursalId == sucursal.Id))
                throw ErrorNegocio.Prohibido($"El personal no pertenece a la sucursal \"{sucursal.Nombre}\".");
        }
        return (u, sucursal, top.Rol.Nombre);
    }

    private static async Task<List<AccesoDto>> Consulta(IQueryable<Acceso> q)
    {
        var filas = await q.Select(a => new
        {
            a.Id, a.UsuarioId, Nombre = a.Usuario!.Perfil!.Nombres + " " + a.Usuario.Perfil.Apellidos, a.Usuario.Email,
            Rol = a.Usuario.Roles.OrderByDescending(r => r.Rol!.Nivel).Select(r => r.Rol!.Nombre).FirstOrDefault(),
            a.SucursalId, Sucursal = a.Sucursal!.Nombre, a.Ingreso, a.Salida, a.Metodo, a.Estado,
        }).ToListAsync();
        return filas.Select(a => new AccesoDto(a.Id, a.UsuarioId, a.Nombre, a.Email, a.Rol ?? Roles.Cliente, a.SucursalId, a.Sucursal, a.Ingreso, a.Salida, a.Metodo, a.Estado,
            a.Salida is null ? null : (int)(a.Salida.Value - a.Ingreso).TotalMinutes)).ToList();
    }
}
