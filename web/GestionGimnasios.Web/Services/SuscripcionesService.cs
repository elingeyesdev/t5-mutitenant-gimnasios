using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record PlanDto(int Id, int? CadenaId, string? Cadena, string Nombre, string? Descripcion, decimal PrecioMensual, int? MaxSucursales, IReadOnlyList<string> Beneficios, bool Activo);
public record PagoDto(int Id, decimal Monto, DateTime Fecha, string? Metodo, string Estado);
public record SuscripcionResumenDto(string Plan, DateOnly Fin);
public record SuscripcionDto(
    int Id, int UsuarioId, string Usuario, int PlanId, string Plan, decimal PrecioMensual, int? SucursalPrincipalId, string? SucursalPrincipal,
    string Estado, DateOnly Inicio, DateOnly Fin, bool RenovacionAutomatica, int DiasRestantes, IReadOnlyList<PagoDto> Pagos);

public class PlanForm
{
    public int? CadenaId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal PrecioMensual { get; set; }
    public int? MaxSucursales { get; set; }
    public List<string> Beneficios { get; set; } = [];
    public bool Activo { get; set; } = true;
}

public class SuscripcionForm
{
    public int UsuarioId { get; set; }
    public int PlanId { get; set; }
    public int? SucursalPrincipalId { get; set; }
    public DateOnly? Inicio { get; set; }
    public int Meses { get; set; } = 1;
    public bool RenovacionAutomatica { get; set; }
    /// <summary>Registra el primer pago junto con la suscripción.</summary>
    public decimal? MontoPagado { get; set; }
    public string? Metodo { get; set; } = "EFECTIVO";
}

public class PagoForm
{
    public decimal Monto { get; set; }
    public string? Metodo { get; set; } = "EFECTIVO";
    public string Estado { get; set; } = "PAGADO";
}

/// <summary>Planes, suscripciones y pagos (CU-19, CU-20, CU-21).</summary>
public class SuscripcionesService(AppDbContext db, UsuarioActual actual, Alcances alcances, DatosPersonales datos)
{
    // ── Planes ───────────────────────────────────────────────────────────

    /// <summary>Planes globales y de la cadena del usuario (o de una cadena concreta).</summary>
    public async Task<List<PlanDto>> PlanesAsync(int? cadenaId = null, bool incluirInactivos = false)
    {
        var q = db.Planes.Include(p => p.Cadena).AsQueryable();
        if (!incluirInactivos) q = q.Where(p => p.Activo);
        var cadena = cadenaId ?? (actual.EsPersonal ? actual.CadenaId : null);
        if (cadena is not null) q = q.Where(p => p.CadenaId == null || p.CadenaId == cadena);
        return (await q.OrderBy(p => p.CadenaId).ThenBy(p => p.PrecioMensual).ToListAsync()).Select(APlan).ToList();
    }

    public async Task<PlanDto> CrearPlanAsync(PlanForm f)
    {
        var cadenaId = actual.EsSuperAdmin ? f.CadenaId : actual.CadenaId;
        ValidarPlan(f);
        if (await db.Planes.AnyAsync(p => p.CadenaId == cadenaId && p.Nombre == f.Nombre.Trim()))
            throw ErrorNegocio.Conflicto("Ya existe un plan con ese nombre.");
        var p = new PlanSuscripcion { CadenaId = cadenaId };
        CopiarPlan(f, p);
        db.Planes.Add(p);
        await db.SaveChangesAsync();
        return APlan(await db.Planes.Include(x => x.Cadena).FirstAsync(x => x.Id == p.Id));
    }

    public async Task<PlanDto> ActualizarPlanAsync(int id, PlanForm f)
    {
        var p = await PlanGestionable(id);
        ValidarPlan(f);
        if (await db.Planes.AnyAsync(x => x.CadenaId == p.CadenaId && x.Nombre == f.Nombre.Trim() && x.Id != id))
            throw ErrorNegocio.Conflicto("Ya existe un plan con ese nombre.");
        CopiarPlan(f, p);
        await db.SaveChangesAsync();
        return APlan(p);
    }

    public async Task EliminarPlanAsync(int id)
    {
        var p = await PlanGestionable(id);
        if (await db.Suscripciones.IgnoreQueryFilters().AnyAsync(s => s.PlanId == id)) p.Activo = false;
        else db.Planes.Remove(p);
        await db.SaveChangesAsync();
    }

    // ── Suscripciones ────────────────────────────────────────────────────

    /// <summary>Plan y vencimiento de los socios de una página, dentro del alcance del personal.</summary>
    public async Task<Dictionary<int, SuscripcionResumenDto>> ResumenPorUsuariosAsync(IEnumerable<int> usuarioIds, int? sucursalId = null)
    {
        var ids = usuarioIds.Distinct().ToArray();
        if (ids.Length == 0) return [];

        var hoy = Reloj.Hoy;
        var q = SuscripcionesVisibles().Where(s => ids.Contains(s.UsuarioId)
            && s.Inicio <= hoy && s.Estado != EstadosSuscripcion.Cancelada);
        if (sucursalId is not null) q = q.Where(s => s.SucursalPrincipalId == sucursalId);

        var resumenes = await q.AsNoTracking()
            .OrderByDescending(s => s.Estado == EstadosSuscripcion.Activa && s.Fin >= hoy)
            .ThenByDescending(s => s.Fin).ThenByDescending(s => s.Id)
            .Select(s => new { s.UsuarioId, Plan = s.Plan!.Nombre, s.Fin })
            .ToListAsync();
        return resumenes.GroupBy(s => s.UsuarioId)
            .ToDictionary(g => g.Key, g => new SuscripcionResumenDto(g.First().Plan, g.First().Fin));
    }

    public async Task<List<SuscripcionDto>> ListarAsync(string? estado = null, int? sucursalId = null)
    {
        await VencerAsync();
        var q = SuscripcionesVisibles();
        if (!string.IsNullOrEmpty(estado)) q = q.Where(s => s.Estado == estado);
        if (sucursalId is not null) q = q.Where(s => s.SucursalPrincipalId == sucursalId);
        return (await Consulta(q).OrderByDescending(s => s.CreadaEn).ToListAsync()).Select(ADto).ToList();
    }

    public async Task<List<SuscripcionDto>> DeUsuarioAsync(int usuarioId)
    {
        await datos.ExigirAsync(usuarioId);
        await VencerAsync();
        var q = db.Suscripciones.Where(s => s.UsuarioId == usuarioId);
        if (actual.EsPersonal && actual.CadenaId is int cadena) q = q.Where(s => s.CadenaId == cadena);
        return (await Consulta(q).OrderByDescending(s => s.Fin).ToListAsync()).Select(ADto).ToList();
    }

    public async Task<SuscripcionDto?> ActivaAsync(int usuarioId)
    {
        await VencerAsync();
        var s = await Consulta(db.Suscripciones.Where(x => x.UsuarioId == usuarioId && x.Estado == EstadosSuscripcion.Activa))
            .OrderByDescending(x => x.Fin).FirstOrDefaultAsync();
        return s is null ? null : ADto(s);
    }

    /// <summary>¿El usuario tiene una suscripción activa válida hoy? (para cualquier cadena o una concreta).</summary>
    public Task<bool> TieneActivaAsync(int usuarioId, int? cadenaId = null)
    {
        var hoy = Reloj.Hoy;
        return db.Suscripciones.AnyAsync(s => s.UsuarioId == usuarioId && s.Estado == EstadosSuscripcion.Activa
                                              && s.Inicio <= hoy && s.Fin >= hoy && (cadenaId == null || s.CadenaId == null || s.CadenaId == cadenaId));
    }

    public async Task<SuscripcionDto> ObtenerAsync(int id)
    {
        var s = await Consulta(SuscripcionesVisibles()).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw ErrorNegocio.NoEncontrado($"Suscripción {id} no encontrada");
        return ADto(s);
    }

    public async Task<SuscripcionDto> CrearAsync(SuscripcionForm f)
    {
        var plan = await db.Planes.FirstOrDefaultAsync(p => p.Id == f.PlanId && p.Activo) ?? throw ErrorNegocio.NoEncontrado($"Plan {f.PlanId} no encontrado");
        if (!await db.Usuarios.AnyAsync(u => u.Id == f.UsuarioId && u.Activo)) throw ErrorNegocio.NoEncontrado($"Usuario {f.UsuarioId} no encontrado.");
        if (f.Meses is < 1 or > 24) throw ErrorNegocio.Solicitud("La duración debe ser de 1 a 24 meses.");

        var sucursalId = f.SucursalPrincipalId is int sid ? alcances.ForzarSucursal(sid) : actual.SucursalFiltro;
        Sucursal? sucursal = sucursalId is null ? null : await alcances.ExigirSucursalAsync(sucursalId.Value, "No puede asignar suscripciones a otra sucursal");
        var cadenaId = plan.CadenaId ?? sucursal?.CadenaId ?? actual.CadenaId;
        if (plan.CadenaId is not null && sucursal is not null && sucursal.CadenaId != plan.CadenaId)
            throw ErrorNegocio.Solicitud("El plan pertenece a otra cadena.");

        var inicio = f.Inicio ?? Reloj.Hoy;
        var suscripcion = new Suscripcion
        {
            UsuarioId = f.UsuarioId, PlanId = plan.Id, CadenaId = cadenaId, SucursalPrincipalId = sucursal?.Id,
            Inicio = inicio, Fin = inicio.AddMonths(f.Meses).AddDays(-1), RenovacionAutomatica = f.RenovacionAutomatica,
        };
        if (f.MontoPagado is > 0) suscripcion.Pagos.Add(new PagoSuscripcion { Monto = f.MontoPagado.Value, Metodo = f.Metodo });
        db.Suscripciones.Add(suscripcion);

        // La suscripción a una cadena crea la membresía del socio en ella (decisión 4.2).
        if (cadenaId is int c && !await db.Membresias.IgnoreQueryFilters().AnyAsync(m => m.UsuarioId == f.UsuarioId && m.CadenaId == c))
            db.Membresias.Add(new Membresia { UsuarioId = f.UsuarioId, CadenaId = c, SucursalId = sucursal?.Id });

        await db.SaveChangesAsync();
        return await ObtenerAsync(suscripcion.Id);
    }

    public async Task<SuscripcionDto> CambiarEstadoAsync(int id, string estado)
    {
        if (estado is not (EstadosSuscripcion.Activa or EstadosSuscripcion.Vencida or EstadosSuscripcion.Cancelada))
            throw ErrorNegocio.Solicitud("Estado de suscripción no válido.");
        var s = await SuscripcionesVisibles().FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado($"Suscripción {id} no encontrada");
        s.Estado = estado;
        s.ActualizadaEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await ObtenerAsync(id);
    }

    public async Task<PagoDto> RegistrarPagoAsync(int suscripcionId, PagoForm f)
    {
        if (f.Monto <= 0) throw ErrorNegocio.Solicitud("El monto debe ser mayor a 0.");
        var s = await SuscripcionesVisibles().FirstOrDefaultAsync(x => x.Id == suscripcionId) ?? throw ErrorNegocio.NoEncontrado($"Suscripción {suscripcionId} no encontrada");
        var p = new PagoSuscripcion { SuscripcionId = s.Id, Monto = f.Monto, Metodo = f.Metodo, Estado = f.Estado };
        db.Pagos.Add(p);
        await db.SaveChangesAsync();
        return new PagoDto(p.Id, p.Monto, p.Fecha, p.Metodo, p.Estado);
    }

    public async Task<List<PagoDto>> PagosAsync(int suscripcionId)
    {
        var s = await db.Suscripciones.FirstOrDefaultAsync(x => x.Id == suscripcionId) ?? throw ErrorNegocio.NoEncontrado($"Suscripción {suscripcionId} no encontrada");
        await datos.ExigirAsync(s.UsuarioId);
        return await db.Pagos.Where(p => p.SuscripcionId == suscripcionId).OrderByDescending(p => p.Fecha)
            .Select(p => new PagoDto(p.Id, p.Monto, p.Fecha, p.Metodo, p.Estado)).ToListAsync();
    }

    /// <summary>Marca como vencidas las suscripciones cuya fecha de fin ya pasó.</summary>
    public Task VencerAsync()
    {
        var hoy = Reloj.Hoy;
        return db.Suscripciones.IgnoreQueryFilters().Where(s => s.Estado == EstadosSuscripcion.Activa && s.Fin < hoy)
            .ExecuteUpdateAsync(x => x.SetProperty(s => s.Estado, EstadosSuscripcion.Vencida).SetProperty(s => s.ActualizadaEn, DateTime.UtcNow));
    }

    // ── Apoyo ────────────────────────────────────────────────────────────

    private IQueryable<Suscripcion> SuscripcionesVisibles()
    {
        var q = db.Suscripciones.AsQueryable();
        if (actual.EsSuperAdmin) return q;
        if (actual.EsCliente || actual.Nivel < Niveles.Recepcionista) return q.Where(s => s.UsuarioId == actual.Id);
        var cadena = actual.CadenaId;
        q = q.Where(s => s.CadenaId == cadena);
        if (actual.SucursalFiltro is int sucursal) q = q.Where(s => s.SucursalPrincipalId == sucursal);
        return q;
    }

    private async Task<PlanSuscripcion> PlanGestionable(int id)
    {
        var p = await db.Planes.Include(x => x.Cadena).FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado($"Plan {id} no encontrado");
        if (!actual.EsSuperAdmin && p.CadenaId != actual.CadenaId)
            throw ErrorNegocio.Prohibido("Solo puedes modificar los planes de tu cadena.");
        return p;
    }

    private static void ValidarPlan(PlanForm f)
    {
        if (string.IsNullOrWhiteSpace(f.Nombre)) throw ErrorNegocio.Solicitud("El nombre del plan es obligatorio.");
        if (f.PrecioMensual <= 0) throw ErrorNegocio.Solicitud("El precio debe ser mayor a 0.");
    }

    private static void CopiarPlan(PlanForm f, PlanSuscripcion p)
    {
        p.Nombre = f.Nombre.Trim();
        p.Descripcion = f.Descripcion;
        p.PrecioMensual = f.PrecioMensual;
        p.MaxSucursales = f.MaxSucursales;
        p.Beneficios = f.Beneficios.Where(b => !string.IsNullOrWhiteSpace(b)).Select(b => b.Trim()).ToList();
        p.Activo = f.Activo;
    }

    private static IQueryable<Suscripcion> Consulta(IQueryable<Suscripcion> q) =>
        q.Include(s => s.Usuario).ThenInclude(u => u!.Perfil).Include(s => s.Plan).Include(s => s.SucursalPrincipal).Include(s => s.Pagos).AsSplitQuery();

    public static PlanDto APlan(PlanSuscripcion p) => new(p.Id, p.CadenaId, p.Cadena?.Nombre, p.Nombre, p.Descripcion, p.PrecioMensual, p.MaxSucursales, p.Beneficios, p.Activo);

    private static SuscripcionDto ADto(Suscripcion s) => new(
        s.Id, s.UsuarioId, s.Usuario?.Perfil?.NombreCompleto ?? s.Usuario?.Email ?? "", s.PlanId, s.Plan?.Nombre ?? "", s.Plan?.PrecioMensual ?? 0,
        s.SucursalPrincipalId, s.SucursalPrincipal?.Nombre, s.Estado, s.Inicio, s.Fin, s.RenovacionAutomatica,
        Math.Max(0, s.Fin.DayNumber - Reloj.Hoy.DayNumber),
        s.Pagos.OrderByDescending(p => p.Fecha).Select(p => new PagoDto(p.Id, p.Monto, p.Fecha, p.Metodo, p.Estado)).ToList());
}
