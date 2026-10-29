using System.Data;
using System.Security.Cryptography;
using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record BeneficioDto(
    int Id, int CadenaId, string Cadena, string Nombre, string? Descripcion, string Tipo, string? Condiciones,
    DateOnly VigenteDesde, DateOnly VigenteHasta, int? CuposTotales, int LimitePorSocio, int AsistenciasMinimas,
    bool RequierePlanActivo, bool Activo, int Canjes);

/// <summary>Beneficio visto por el socio: vigencia, reglas y si puede canjearlo ahora (y por qué no).</summary>
public record BeneficioSocioDto(
    int Id, string Cadena, string Nombre, string? Descripcion, string Tipo, string? Condiciones,
    DateOnly VigenteDesde, DateOnly VigenteHasta, IReadOnlyList<string> Reglas, int CanjesUsados, int? CuposRestantes,
    bool Elegible, string? Motivo);

public record CanjeDto(
    int Id, string Codigo, int BeneficioId, string Beneficio, string Tipo, int UsuarioId, string Socio, string Cadena,
    DateTime CanjeadoEn, string Estado, DateTime? EntregadoEn);

/// <summary>Datos del socio con los que se decide si puede canjear un beneficio.</summary>
public record ContextoCanje(DateOnly Hoy, bool TienePlanActivo, int Asistencias30Dias, int CanjesDelSocio, int CanjesTotales);

public class BeneficioForm
{
    /// <summary>Solo lo usa el super administrador; el gerente crea beneficios de su cadena.</summary>
    public int? CadenaId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = TiposBeneficio.Descuento;
    public string? Condiciones { get; set; }
    public DateOnly VigenteDesde { get; set; }
    public DateOnly VigenteHasta { get; set; }
    public int? CuposTotales { get; set; }
    public int LimitePorSocio { get; set; } = 1;
    public int AsistenciasMinimas { get; set; }
    public bool RequierePlanActivo { get; set; } = true;
}

/// <summary>
/// Beneficios, vales y cupones con reglas verificables de canje (RF-03, CU-37). El gerente los
/// define; el socio canjea los que le corresponden y recibe un comprobante; recepción lo valida
/// y lo entrega una sola vez.
/// </summary>
public class BeneficiosService(AppDbContext db, UsuarioActual actual, SuscripcionesService suscripciones)
{
    // ── Gestión (gerente) ────────────────────────────────────────────────

    /// <summary>Beneficios de la cadena del personal (el filtro global limita la cadena).</summary>
    public async Task<List<BeneficioDto>> ListarAsync() =>
        await db.Beneficios.OrderByDescending(b => b.Activo).ThenByDescending(b => b.VigenteHasta).ThenBy(b => b.Nombre)
            .Select(b => new BeneficioDto(b.Id, b.CadenaId, b.Cadena!.Nombre, b.Nombre, b.Descripcion, b.Tipo, b.Condiciones,
                b.VigenteDesde, b.VigenteHasta, b.CuposTotales, b.LimitePorSocio, b.AsistenciasMinimas, b.RequierePlanActivo, b.Activo, b.Canjes.Count))
            .ToListAsync();

    public async Task<BeneficioDto> CrearAsync(BeneficioForm f)
    {
        Validar(f);
        var cadenaId = actual.EsSuperAdmin
            ? f.CadenaId ?? throw ErrorNegocio.Solicitud("Elige la cadena del beneficio.")
            : actual.CadenaId ?? throw ErrorNegocio.Prohibido("No tienes una cadena asignada.");
        var b = new Beneficio { CadenaId = cadenaId };
        Copiar(f, b);
        db.Beneficios.Add(b);
        await db.SaveChangesAsync();
        return await ObtenerAsync(b.Id);
    }

    public async Task<BeneficioDto> ActualizarAsync(int id, BeneficioForm f)
    {
        Validar(f);
        var b = await db.Beneficios.FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado("Beneficio no encontrado.");
        Copiar(f, b);
        await db.SaveChangesAsync();
        return await ObtenerAsync(id);
    }

    /// <summary>Activa o desactiva el beneficio. No se elimina para conservar sus canjes.</summary>
    public async Task<BeneficioDto> CambiarActivoAsync(int id, bool activo)
    {
        var b = await db.Beneficios.FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado("Beneficio no encontrado.");
        b.Activo = activo;
        await db.SaveChangesAsync();
        return await ObtenerAsync(id);
    }

    // ── Socio: consultar y canjear ───────────────────────────────────────

    /// <summary>Beneficios vigentes o próximos de las cadenas donde el socio tiene membresía activa.</summary>
    public async Task<List<BeneficioSocioDto>> DisponiblesAsync()
    {
        var hoy = Reloj.Hoy;
        var cadenas = await CadenasDelSocio().ToListAsync();
        var beneficios = await db.Beneficios.IgnoreQueryFilters().Include(b => b.Cadena)
            .Where(b => cadenas.Contains(b.CadenaId) && b.Activo && b.VigenteHasta >= hoy)
            .OrderBy(b => b.VigenteHasta).ToListAsync();
        if (beneficios.Count == 0) return [];

        var ids = beneficios.Select(b => b.Id).ToList();
        var totales = await db.Canjes.IgnoreQueryFilters().Where(c => ids.Contains(c.BeneficioId))
            .GroupBy(c => c.BeneficioId).Select(g => new { g.Key, Total = g.Count(), Mios = g.Count(c => c.UsuarioId == actual.Id) })
            .ToDictionaryAsync(x => x.Key);
        var desde = DateTime.UtcNow.AddDays(-30);
        var asistencias = await db.Accesos.IgnoreQueryFilters().Where(a => a.UsuarioId == actual.Id && a.Ingreso >= desde && cadenas.Contains(a.CadenaId))
            .GroupBy(a => a.CadenaId).Select(g => new { g.Key, Total = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Total);
        var planes = new Dictionary<int, bool>();
        foreach (var c in cadenas) planes[c] = await suscripciones.TieneActivaAsync(actual.Id, c);

        return beneficios.Select(b =>
        {
            var t = totales.GetValueOrDefault(b.Id);
            var ctx = new ContextoCanje(hoy, planes.GetValueOrDefault(b.CadenaId), asistencias.GetValueOrDefault(b.CadenaId), t?.Mios ?? 0, t?.Total ?? 0);
            var motivo = MotivoNoElegible(b, ctx);
            return new BeneficioSocioDto(b.Id, b.Cadena?.Nombre ?? "", b.Nombre, b.Descripcion, b.Tipo, b.Condiciones, b.VigenteDesde, b.VigenteHasta,
                Reglas(b), ctx.CanjesDelSocio, b.CuposTotales is int cupos ? Math.Max(0, cupos - ctx.CanjesTotales) : null, motivo is null, motivo);
        }).ToList();
    }

    /// <summary>
    /// Verifica elegibilidad, vigencia y uso, y registra el canje una sola vez. La transacción
    /// serializable evita que dos pedidos simultáneos superen el límite.
    /// </summary>
    public async Task<CanjeDto> CanjearAsync(int beneficioId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var b = await db.Beneficios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == beneficioId)
                ?? throw ErrorNegocio.NoEncontrado("Beneficio no encontrado.");
        if (!await CadenasDelSocio().ContainsAsync(b.CadenaId))
            throw ErrorNegocio.Prohibido("Este beneficio es para socios de otra cadena.");

        var desde = DateTime.UtcNow.AddDays(-30);
        var ctx = new ContextoCanje(
            Reloj.Hoy,
            await suscripciones.TieneActivaAsync(actual.Id, b.CadenaId),
            await db.Accesos.IgnoreQueryFilters().CountAsync(a => a.UsuarioId == actual.Id && a.CadenaId == b.CadenaId && a.Ingreso >= desde),
            await db.Canjes.IgnoreQueryFilters().CountAsync(c => c.BeneficioId == b.Id && c.UsuarioId == actual.Id),
            await db.Canjes.IgnoreQueryFilters().CountAsync(c => c.BeneficioId == b.Id));
        if (MotivoNoElegible(b, ctx) is string motivo) throw ErrorNegocio.Conflicto(motivo);

        var canje = new CanjeBeneficio { CadenaId = b.CadenaId, BeneficioId = b.Id, UsuarioId = actual.Id, Codigo = await NuevoCodigoAsync() };
        db.Canjes.Add(canje);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return (await Canjes(db.Canjes.IgnoreQueryFilters().Where(c => c.Id == canje.Id))).First();
    }

    public Task<List<CanjeDto>> MisCanjesAsync() =>
        Canjes(db.Canjes.IgnoreQueryFilters().Where(c => c.UsuarioId == actual.Id));

    // ── Recepción: validar y entregar ────────────────────────────────────

    /// <summary>Últimos canjes de la cadena del personal.</summary>
    public Task<List<CanjeDto>> CanjesAsync(string? estado = null, int limite = 100)
    {
        var q = db.Canjes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(c => c.Estado == estado);
        return Canjes(q, limite);
    }

    public async Task<CanjeDto> CanjePorCodigoAsync(string codigo) =>
        (await Canjes(db.Canjes.Where(c => c.Codigo == Normalizar(codigo)))).FirstOrDefault()
        ?? throw ErrorNegocio.NoEncontrado("No existe un canje con ese código en tu cadena.");

    /// <summary>Recepción entrega el beneficio: el comprobante se usa una sola vez.</summary>
    public async Task<CanjeDto> EntregarAsync(string codigo)
    {
        var c = await db.Canjes.FirstOrDefaultAsync(x => x.Codigo == Normalizar(codigo))
                ?? throw ErrorNegocio.NoEncontrado("No existe un canje con ese código en tu cadena.");
        if (c.Estado == EstadosCanje.Entregado)
            throw ErrorNegocio.Conflicto($"Este comprobante ya se usó el {Reloj.ALocal(c.EntregadoEn!.Value):dd/MM/yyyy HH:mm}.");
        c.Estado = EstadosCanje.Entregado;
        c.EntregadoEn = DateTime.UtcNow;
        c.EntregadoPorId = actual.Id;
        await db.SaveChangesAsync();
        return await CanjePorCodigoAsync(c.Codigo);
    }

    // ── Reglas ───────────────────────────────────────────────────────────

    /// <summary>Por qué el socio no puede canjear el beneficio ahora, o null si puede.</summary>
    public static string? MotivoNoElegible(Beneficio b, ContextoCanje c)
    {
        if (!b.Activo) return "El beneficio ya no está disponible.";
        if (c.Hoy < b.VigenteDesde) return $"Disponible desde el {b.VigenteDesde:dd/MM/yyyy}.";
        if (c.Hoy > b.VigenteHasta) return "El beneficio está vencido.";
        if (c.CanjesDelSocio >= b.LimitePorSocio)
            return b.LimitePorSocio == 1 ? "Ya canjeaste este beneficio." : $"Ya usaste los {b.LimitePorSocio} canjes permitidos.";
        if (b.CuposTotales is int cupos && c.CanjesTotales >= cupos) return "El beneficio está agotado.";
        if (b.RequierePlanActivo && !c.TienePlanActivo) return "Necesitas un plan activo en la cadena.";
        if (c.Asistencias30Dias < b.AsistenciasMinimas)
            return $"Necesitas {b.AsistenciasMinimas} ingresos en los últimos 30 días; llevas {c.Asistencias30Dias}.";
        return null;
    }

    /// <summary>Reglas del beneficio en texto, para mostrarlas al socio.</summary>
    public static List<string> Reglas(Beneficio b)
    {
        var reglas = new List<string> { $"Válido del {b.VigenteDesde:dd/MM/yyyy} al {b.VigenteHasta:dd/MM/yyyy}" };
        if (b.RequierePlanActivo) reglas.Add("Requiere un plan activo en la cadena");
        if (b.AsistenciasMinimas > 0) reglas.Add($"Mínimo {b.AsistenciasMinimas} ingresos en los últimos 30 días");
        reglas.Add(b.LimitePorSocio == 1 ? "Un canje por socio" : $"Hasta {b.LimitePorSocio} canjes por socio");
        if (b.CuposTotales is int cupos) reglas.Add($"{cupos} canjes disponibles en total");
        return reglas;
    }

    public static void Validar(BeneficioForm f)
    {
        if (string.IsNullOrWhiteSpace(f.Nombre)) throw ErrorNegocio.Solicitud("El nombre del beneficio es obligatorio.");
        if (!TiposBeneficio.Todos.Contains(f.Tipo)) throw ErrorNegocio.Solicitud("Tipo de beneficio no válido.");
        if (f.VigenteHasta < f.VigenteDesde) throw ErrorNegocio.Solicitud("La vigencia termina antes de empezar.");
        if (f.LimitePorSocio < 1) throw ErrorNegocio.Solicitud("Cada socio debe poder canjearlo al menos una vez.");
        if (f.CuposTotales is < 1) throw ErrorNegocio.Solicitud("Los cupos totales deben ser al menos 1, o déjalos vacíos.");
        if (f.AsistenciasMinimas < 0) throw ErrorNegocio.Solicitud("Las asistencias mínimas no pueden ser negativas.");
    }

    // ── Apoyo ────────────────────────────────────────────────────────────

    private IQueryable<int> CadenasDelSocio() =>
        db.Membresias.IgnoreQueryFilters().Where(m => m.UsuarioId == actual.Id && m.Activa).Select(m => m.CadenaId);

    private static void Copiar(BeneficioForm f, Beneficio b)
    {
        b.Nombre = f.Nombre.Trim();
        b.Descripcion = f.Descripcion;
        b.Tipo = f.Tipo;
        b.Condiciones = f.Condiciones;
        b.VigenteDesde = f.VigenteDesde;
        b.VigenteHasta = f.VigenteHasta;
        b.CuposTotales = f.CuposTotales;
        b.LimitePorSocio = f.LimitePorSocio;
        b.AsistenciasMinimas = f.AsistenciasMinimas;
        b.RequierePlanActivo = f.RequierePlanActivo;
    }

    private async Task<BeneficioDto> ObtenerAsync(int id) => (await ListarAsync()).First(b => b.Id == id);

    /// <summary>Código corto y legible del comprobante (8 caracteres hexadecimales), único.</summary>
    private async Task<string> NuevoCodigoAsync()
    {
        string codigo;
        do codigo = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
        while (await db.Canjes.IgnoreQueryFilters().AnyAsync(c => c.Codigo == codigo));
        return codigo;
    }

    private static string Normalizar(string codigo) => (codigo ?? "").Trim().ToUpperInvariant();

    private static async Task<List<CanjeDto>> Canjes(IQueryable<CanjeBeneficio> q, int limite = 100) =>
        await q.OrderByDescending(c => c.CanjeadoEn).Take(limite)
            .Select(c => new CanjeDto(c.Id, c.Codigo, c.BeneficioId, c.Beneficio!.Nombre, c.Beneficio.Tipo, c.UsuarioId,
                c.Usuario!.Perfil!.Nombres + " " + c.Usuario.Perfil.Apellidos, c.Beneficio.Cadena!.Nombre, c.CanjeadoEn, c.Estado, c.EntregadoEn))
            .ToListAsync();
}
