using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record CadenaDisponibleDto(int Id, string Nombre, string? Descripcion);
public record ConsentimientoDto(
    int Id, int UsuarioId, string Socio, int CadenaId, string Cadena, IReadOnlyList<string> Datos,
    DateTime OtorgadoEn, DateTime? RevocadoEn, int Consultas, DateTime? UltimaConsulta)
{
    public bool Vigente => RevocadoEn is null;
}
public record ConsultaPerfilDto(DateTime ConsultadoEn, string ConsultadoPor);
public record PerfilPortableDto(
    int ConsentimientoId, int UsuarioId, string Socio, string Cadena, IReadOnlyList<string> Datos, DateTime OtorgadoEn,
    PerfilEntrenamientoDto? Entrenamiento, RestriccionDto? Restriccion, IReadOnlyList<SesionResumenDto>? Sesiones, IReadOnlyList<MetricaDto>? Metricas);

public class ConsentimientoForm
{
    public int CadenaId { get; set; }
    public List<string> Datos { get; set; } = [];
}

/// <summary>
/// Perfil deportivo portable (RF-02, CU-36). El socio autoriza a otra cadena a consultar solo
/// los datos que elige y puede revocarlo cuando quiera. Sin consentimiento vigente, la otra
/// cadena no accede al historial (RT-01). Cada consulta queda registrada.
/// </summary>
public class PortabilidadService(AppDbContext db, UsuarioActual actual)
{
    // ── Socio: dar, ver y revocar consentimientos ────────────────────────

    /// <summary>Cadenas que el socio puede autorizar: las activas donde todavía no es socio.</summary>
    public async Task<List<CadenaDisponibleDto>> CadenasDisponiblesAsync()
    {
        var propias = db.Membresias.IgnoreQueryFilters().Where(m => m.UsuarioId == actual.Id && m.Activa).Select(m => m.CadenaId);
        return await db.Cadenas.Where(c => c.Activa && !propias.Contains(c.Id)).OrderBy(c => c.Nombre)
            .Select(c => new CadenaDisponibleDto(c.Id, c.Nombre, c.Descripcion)).ToListAsync();
    }

    public Task<List<ConsentimientoDto>> MisConsentimientosAsync() =>
        Consulta(db.Consentimientos.IgnoreQueryFilters().Where(c => c.UsuarioId == actual.Id));

    public async Task<ConsentimientoDto> OtorgarAsync(ConsentimientoForm f)
    {
        var datos = ValidarDatos(f.Datos);
        var cadena = await db.Cadenas.FirstOrDefaultAsync(c => c.Id == f.CadenaId && c.Activa)
                     ?? throw ErrorNegocio.NoEncontrado("Cadena no encontrada.");
        if (await db.Membresias.IgnoreQueryFilters().AnyAsync(m => m.UsuarioId == actual.Id && m.CadenaId == f.CadenaId && m.Activa))
            throw ErrorNegocio.Solicitud($"Ya eres socio de {cadena.Nombre}: su personal ya puede ver tu perfil.");

        // Un solo consentimiento vigente por cadena: el anterior se revoca y queda en el historial.
        var anteriores = await db.Consentimientos.IgnoreQueryFilters()
            .Where(c => c.UsuarioId == actual.Id && c.CadenaId == f.CadenaId && c.RevocadoEn == null).ToListAsync();
        foreach (var a in anteriores) a.RevocadoEn = DateTime.UtcNow;

        var nuevo = new ConsentimientoPerfil { UsuarioId = actual.Id, CadenaId = f.CadenaId, Datos = datos };
        db.Consentimientos.Add(nuevo);
        await db.SaveChangesAsync();
        return await PropioAsync(nuevo.Id);
    }

    public async Task<ConsentimientoDto> RevocarAsync(int id)
    {
        var c = await db.Consentimientos.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == actual.Id)
                ?? throw ErrorNegocio.NoEncontrado("Consentimiento no encontrado.");
        if (c.RevocadoEn is not null) throw ErrorNegocio.Solicitud("Este consentimiento ya estaba revocado.");
        c.RevocadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await PropioAsync(id);
    }

    /// <summary>Quién consultó el perfil con este consentimiento y cuándo (así el socio lo verifica).</summary>
    public async Task<List<ConsultaPerfilDto>> ConsultasAsync(int id)
    {
        if (!await db.Consentimientos.IgnoreQueryFilters().AnyAsync(x => x.Id == id && x.UsuarioId == actual.Id))
            throw ErrorNegocio.NoEncontrado("Consentimiento no encontrado.");
        return await db.ConsultasPerfil.Where(x => x.ConsentimientoId == id).OrderByDescending(x => x.ConsultadoEn)
            .Select(x => new ConsultaPerfilDto(x.ConsultadoEn, x.ConsultadoPor!.Perfil!.Nombres + " " + x.ConsultadoPor.Perfil.Apellidos))
            .ToListAsync();
    }

    // ── Personal de la cadena autorizada ─────────────────────────────────

    /// <summary>Socios con consentimiento vigente para la cadena del personal (el filtro global limita la cadena).</summary>
    public Task<List<ConsentimientoDto>> AutorizadosAsync()
    {
        ExigirPersonal();
        return Consulta(db.Consentimientos.Where(c => c.RevocadoEn == null));
    }

    /// <summary>Perfil del socio con solo los datos autorizados. Sin consentimiento vigente se rechaza.</summary>
    public async Task<PerfilPortableDto> PerfilAsync(int usuarioId)
    {
        ExigirPersonal();
        var c = await db.Consentimientos.Include(x => x.Usuario).ThenInclude(u => u!.Perfil).Include(x => x.Cadena)
                    .Where(x => x.UsuarioId == usuarioId && x.RevocadoEn == null)
                    .OrderByDescending(x => x.OtorgadoEn).FirstOrDefaultAsync()
                ?? throw ErrorNegocio.Prohibido("El socio no autorizó a tu cadena a consultar su perfil, o revocó la autorización.");

        PerfilEntrenamientoDto? entrenamiento = null;
        RestriccionDto? restriccion = null;
        List<SesionResumenDto>? sesiones = null;
        List<MetricaDto>? metricas = null;
        if (c.Datos.Contains(DatosPortables.Perfil))
            entrenamiento = await db.PerfilesEntrenamiento.Where(p => p.UsuarioId == usuarioId)
                .Select(p => new PerfilEntrenamientoDto(p.ObjetivoPrincipal, p.NivelExperiencia, p.TiposPreferidos, p.AreasPrioritarias, p.DiasDisponibles))
                .FirstOrDefaultAsync();
        if (c.Datos.Contains(DatosPortables.Restricciones))
            restriccion = await db.Restricciones.Where(r => r.UsuarioId == usuarioId)
                .Select(r => new RestriccionDto(r.Tipo, r.Descripcion, r.AreasAfectadas, r.MovimientosEvitar, r.RequiereAprobacion, r.Activa))
                .FirstOrDefaultAsync();
        if (c.Datos.Contains(DatosPortables.Historial))
            sesiones = await db.Sesiones.Where(s => s.UsuarioId == usuarioId).OrderByDescending(s => s.Inicio).Take(20)
                .Select(s => new SesionResumenDto(s.Id, s.Inicio, s.Rutina != null ? s.Rutina.Nombre : (s.TipoDeporte ?? "Entrenamiento libre"), s.DuracionSeg, s.Series.Count))
                .ToListAsync();
        if (c.Datos.Contains(DatosPortables.Metricas))
            metricas = (await db.Metricas.Where(m => m.UsuarioId == usuarioId).OrderByDescending(m => m.RegistradaEn).Take(20).ToListAsync())
                .Select(EntrenamientoService.AMetrica).ToList();

        db.ConsultasPerfil.Add(new ConsultaPerfilPortable { ConsentimientoId = c.Id, ConsultadoPorId = actual.Id });
        await db.SaveChangesAsync();

        return new PerfilPortableDto(c.Id, usuarioId, c.Usuario?.Perfil?.NombreCompleto ?? c.Usuario?.Email ?? "", c.Cadena?.Nombre ?? "",
            c.Datos, c.OtorgadoEn, entrenamiento, restriccion, sesiones, metricas);
    }

    // ── Reglas ───────────────────────────────────────────────────────────

    /// <summary>Normaliza los datos elegidos: al menos uno, sin repetidos y solo los permitidos.</summary>
    public static List<string> ValidarDatos(IEnumerable<string>? datos)
    {
        var lista = (datos ?? []).Select(d => d.Trim().ToUpperInvariant()).Where(d => d.Length > 0).Distinct().ToList();
        if (lista.Count == 0) throw ErrorNegocio.Solicitud("Elige al menos un dato para compartir.");
        var invalidos = lista.Except(DatosPortables.Todos).ToList();
        if (invalidos.Count > 0) throw ErrorNegocio.Solicitud($"Dato no permitido: {string.Join(", ", invalidos)}.");
        return DatosPortables.Todos.Where(lista.Contains).ToList();
    }

    /// <summary>Solo el personal administrativo de una cadena (o el super administrador) consulta perfiles portables.</summary>
    private void ExigirPersonal()
    {
        if (actual.EsSuperAdmin) return;
        if (!actual.EsPersonal || actual.Nivel < Niveles.Recepcionista || actual.CadenaId is null)
            throw ErrorNegocio.Prohibido("Solo el personal administrativo de una cadena puede consultar perfiles portables.");
    }

    private async Task<ConsentimientoDto> PropioAsync(int id) =>
        (await Consulta(db.Consentimientos.IgnoreQueryFilters().Where(c => c.Id == id))).First();

    private static async Task<List<ConsentimientoDto>> Consulta(IQueryable<ConsentimientoPerfil> q) =>
        await q.OrderByDescending(c => c.OtorgadoEn)
            .Select(c => new ConsentimientoDto(
                c.Id, c.UsuarioId, c.Usuario!.Perfil!.Nombres + " " + c.Usuario.Perfil.Apellidos, c.CadenaId, c.Cadena!.Nombre,
                c.Datos, c.OtorgadoEn, c.RevocadoEn, c.Consultas.Count, c.Consultas.Max(x => (DateTime?)x.ConsultadoEn)))
            .ToListAsync();
}
