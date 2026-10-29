using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record ReglaInactividadDto(int CadenaId, int DiasAviso, int DiasRiesgo, bool SoloConPlanActivo, bool Activa, DateTime? ActualizadaEn);
public record ResultadoDeteccion(int Revisados, int Nuevas, int Actualizadas, int Cerradas);
public record ResumenAlertasDto(int Medias, int Altas, int EnSeguimiento, int CerradasUltimos30Dias);
public record IntervencionDto(int Id, string Tipo, string Nota, string RegistradaPor, DateTime RegistradaEn);
public record AlertaDto(
    int Id, int UsuarioId, string Socio, string? Telefono, int? SucursalId, string? Sucursal, string Nivel, int DiasInactivo,
    DateTime? UltimaActividad, string Estado, DateTime DetectadaEn, DateTime? CerradaEn, string? MotivoCierre,
    IReadOnlyList<IntervencionDto> Intervenciones);

public class ReglaInactividadForm
{
    public int DiasAviso { get; set; } = 7;
    public int DiasRiesgo { get; set; } = 14;
    public bool SoloConPlanActivo { get; set; } = true;
    public bool Activa { get; set; } = true;
}

public class IntervencionForm
{
    public string Tipo { get; set; } = TiposIntervencion.Llamada;
    public string Nota { get; set; } = "";
    /// <summary>Cierra la alerta después de registrar el contacto.</summary>
    public bool Cerrar { get; set; }
}

/// <summary>
/// Alertas tempranas de inactividad o abandono (RF-04, CU-38). El gerente configura los umbrales
/// de su cadena; el sistema revisa a diario a los socios y abre, actualiza o cierra alertas; el
/// personal registra el contacto o el seguimiento. Todo respeta la cadena y la sucursal (RT-01, RT-02).
/// </summary>
public class AlertasService(AppDbContext db, UsuarioActual actual)
{
    // ── Umbrales de la cadena (gerente) ──────────────────────────────────

    public async Task<ReglaInactividadDto> ReglaAsync()
    {
        var cadenaId = CadenaPropia();
        var r = await db.ReglasInactividad.FirstOrDefaultAsync(x => x.CadenaId == cadenaId);
        return r is null ? ADto(new ReglaInactividad { CadenaId = cadenaId }, null) : ADto(r, r.ActualizadaEn);
    }

    public async Task<ReglaInactividadDto> GuardarReglaAsync(ReglaInactividadForm f)
    {
        ValidarRegla(f);
        var cadenaId = CadenaPropia();
        var r = await db.ReglasInactividad.FirstOrDefaultAsync(x => x.CadenaId == cadenaId);
        if (r is null) db.ReglasInactividad.Add(r = new ReglaInactividad { CadenaId = cadenaId });
        r.DiasAviso = f.DiasAviso;
        r.DiasRiesgo = f.DiasRiesgo;
        r.SoloConPlanActivo = f.SoloConPlanActivo;
        r.Activa = f.Activa;
        r.ActualizadaEn = DateTime.UtcNow;
        r.ActualizadaPorId = actual.Id;
        await db.SaveChangesAsync();
        return ADto(r, r.ActualizadaEn);
    }

    /// <summary>El gerente ejecuta la detección de su cadena sin esperar a la revisión diaria.</summary>
    public Task<ResultadoDeteccion> DetectarAhoraAsync() => DetectarAsync(db, CadenaPropia());

    public async Task<ResumenAlertasDto> ResumenAsync()
    {
        var q = Visibles();
        var desde = DateTime.UtcNow.AddDays(-30);
        return new ResumenAlertasDto(
            await q.CountAsync(a => a.Estado == EstadosAlerta.Abierta && a.Nivel == NivelesAlerta.Medio),
            await q.CountAsync(a => a.Estado == EstadosAlerta.Abierta && a.Nivel == NivelesAlerta.Alto),
            await q.CountAsync(a => a.Estado == EstadosAlerta.EnSeguimiento),
            await q.CountAsync(a => a.Estado == EstadosAlerta.Cerrada && a.CerradaEn >= desde));
    }

    // ── Alertas e intervenciones (entrenador, recepción, gerente) ────────

    /// <summary>Alertas del alcance del personal. Sin estado, devuelve las que siguen abiertas o en seguimiento.</summary>
    public async Task<List<AlertaDto>> ListarAsync(string? estado = null, string? nivel = null, int? sucursalId = null)
    {
        var q = Visibles();
        q = string.IsNullOrWhiteSpace(estado) ? q.Where(a => a.Estado != EstadosAlerta.Cerrada) : q.Where(a => a.Estado == estado);
        if (!string.IsNullOrWhiteSpace(nivel)) q = q.Where(a => a.Nivel == nivel);
        if (sucursalId is not null) q = q.Where(a => a.SucursalId == sucursalId);
        return await Consulta(q.OrderByDescending(a => a.Nivel == NivelesAlerta.Alto).ThenByDescending(a => a.DiasInactivo).Take(200));
    }

    public async Task<AlertaDto> ObtenerAsync(int id) =>
        (await Consulta(Visibles().Where(a => a.Id == id))).FirstOrDefault() ?? throw ErrorNegocio.NoEncontrado("Alerta no encontrada.");

    /// <summary>Registra el contacto con el socio; la alerta pasa a seguimiento o se cierra.</summary>
    public async Task<AlertaDto> RegistrarIntervencionAsync(int id, IntervencionForm f)
    {
        if (!TiposIntervencion.Todos.Contains(f.Tipo)) throw ErrorNegocio.Solicitud("Tipo de contacto no válido.");
        if (string.IsNullOrWhiteSpace(f.Nota)) throw ErrorNegocio.Solicitud("Describe el contacto o el seguimiento.");
        var a = await Visibles().FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado("Alerta no encontrada.");
        if (a.Estado == EstadosAlerta.Cerrada) throw ErrorNegocio.Solicitud("La alerta ya está cerrada.");

        db.Intervenciones.Add(new IntervencionAlerta { AlertaId = a.Id, RegistradaPorId = actual.Id, Tipo = f.Tipo, Nota = f.Nota.Trim() });
        a.Estado = f.Cerrar ? EstadosAlerta.Cerrada : EstadosAlerta.EnSeguimiento;
        a.ActualizadaEn = DateTime.UtcNow;
        if (f.Cerrar)
        {
            a.CerradaEn = DateTime.UtcNow;
            a.MotivoCierre = "Cerrada por el personal después del seguimiento.";
        }
        await db.SaveChangesAsync();
        return await ObtenerAsync(id);
    }

    // ── Detección (revisión diaria y manual) ─────────────────────────────

    /// <summary>
    /// Revisa a los socios con membresía activa de una cadena (o de todas) y compara sus días sin
    /// asistir con los umbrales: abre una alerta nueva, actualiza la abierta o la cierra si volvió.
    /// </summary>
    public static async Task<ResultadoDeteccion> DetectarAsync(AppDbContext db, int? cadenaId = null)
    {
        var hoy = Reloj.Hoy;
        var ahora = DateTime.UtcNow;
        var cadenas = await db.Cadenas.Where(c => c.Activa && (cadenaId == null || c.Id == cadenaId)).Select(c => c.Id).ToListAsync();
        int revisados = 0, nuevas = 0, actualizadas = 0, cerradas = 0;

        foreach (var cadena in cadenas)
        {
            var regla = await db.ReglasInactividad.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.CadenaId == cadena)
                        ?? new ReglaInactividad { CadenaId = cadena };
            if (!regla.Activa) continue;

            var socios = await db.Membresias.IgnoreQueryFilters()
                .Where(m => m.CadenaId == cadena && m.Activa && m.Usuario!.Activo && m.Usuario.Roles.Any(r => r.Rol!.Nombre == Roles.Cliente))
                .Select(m => new { m.UsuarioId, m.SucursalId, m.CreadaEn }).ToListAsync();
            if (regla.SoloConPlanActivo)
            {
                var conPlan = await db.Suscripciones
                    .Where(s => s.Estado == EstadosSuscripcion.Activa && s.Inicio <= hoy && s.Fin >= hoy && (s.CadenaId == null || s.CadenaId == cadena))
                    .Select(s => s.UsuarioId).Distinct().ToListAsync();
                socios = socios.Where(s => conPlan.Contains(s.UsuarioId)).ToList();
            }
            var ids = socios.Select(s => s.UsuarioId).Distinct().ToList();
            if (ids.Count == 0) continue;

            // Actividad = ingreso a una sucursal de la cadena o entrenamiento registrado en ella.
            var ingresos = await db.Accesos.IgnoreQueryFilters().Where(a => a.CadenaId == cadena && ids.Contains(a.UsuarioId))
                .GroupBy(a => a.UsuarioId).Select(g => new { g.Key, Ultima = g.Max(a => a.Ingreso) }).ToDictionaryAsync(x => x.Key, x => x.Ultima);
            var sucursales = db.Sucursales.IgnoreQueryFilters().Where(s => s.CadenaId == cadena).Select(s => s.Id);
            var entrenos = await db.Sesiones.Where(s => ids.Contains(s.UsuarioId) && s.SucursalId != null && sucursales.Contains(s.SucursalId.Value))
                .GroupBy(s => s.UsuarioId).Select(g => new { g.Key, Ultima = g.Max(s => s.Inicio) }).ToDictionaryAsync(x => x.Key, x => x.Ultima);
            var abiertas = await db.Alertas.IgnoreQueryFilters().Where(a => a.CadenaId == cadena && a.Estado != EstadosAlerta.Cerrada)
                .ToDictionaryAsync(a => a.UsuarioId);

            foreach (var s in socios.GroupBy(x => x.UsuarioId).Select(g => g.First()))
            {
                revisados++;
                DateTime? ultima = Max(ingresos.GetValueOrDefault(s.UsuarioId), entrenos.GetValueOrDefault(s.UsuarioId));
                var dias = DiasSinActividad(ultima ?? s.CreadaEn, hoy);
                var nivel = Nivel(dias, regla.DiasAviso, regla.DiasRiesgo);
                abiertas.TryGetValue(s.UsuarioId, out var alerta);

                if (nivel is null)
                {
                    if (alerta is null) continue;
                    alerta.Estado = EstadosAlerta.Cerrada;
                    alerta.CerradaEn = ahora;
                    alerta.MotivoCierre = ultima > alerta.UltimaActividad || (ultima is not null && alerta.UltimaActividad is null)
                        ? "El socio volvió a asistir."
                        : "Ya no supera los umbrales configurados.";
                    alerta.DiasInactivo = dias;
                    alerta.UltimaActividad = ultima;
                    alerta.ActualizadaEn = ahora;
                    cerradas++;
                }
                else if (alerta is null)
                {
                    db.Alertas.Add(new AlertaInactividad
                    {
                        CadenaId = cadena, SucursalId = s.SucursalId, UsuarioId = s.UsuarioId, Nivel = nivel,
                        DiasInactivo = dias, UltimaActividad = ultima, DetectadaEn = ahora, ActualizadaEn = ahora,
                    });
                    nuevas++;
                }
                else if (alerta.DiasInactivo != dias || alerta.Nivel != nivel)
                {
                    alerta.Nivel = nivel;
                    alerta.DiasInactivo = dias;
                    alerta.UltimaActividad = ultima;
                    alerta.SucursalId = s.SucursalId;
                    alerta.ActualizadaEn = ahora;
                    actualizadas++;
                }
            }
            await db.SaveChangesAsync();
        }
        return new ResultadoDeteccion(revisados, nuevas, actualizadas, cerradas);
    }

    // ── Reglas ───────────────────────────────────────────────────────────

    /// <summary>Nivel de la alerta para los días sin asistir, o null si no supera el umbral.</summary>
    public static string? Nivel(int diasSinActividad, int diasAviso, int diasRiesgo) =>
        diasSinActividad >= diasRiesgo ? NivelesAlerta.Alto
        : diasSinActividad >= diasAviso ? NivelesAlerta.Medio
        : null;

    /// <summary>Días completos (en hora local) desde la última actividad hasta hoy.</summary>
    public static int DiasSinActividad(DateTime ultimaUtc, DateOnly hoy) =>
        Math.Max(0, hoy.DayNumber - DateOnly.FromDateTime(Reloj.ALocal(ultimaUtc)).DayNumber);

    public static void ValidarRegla(ReglaInactividadForm f)
    {
        if (f.DiasAviso is < 1 or > 180) throw ErrorNegocio.Solicitud("Los días para el aviso deben estar entre 1 y 180.");
        if (f.DiasRiesgo <= f.DiasAviso) throw ErrorNegocio.Solicitud("Los días de riesgo alto deben ser más que los del aviso.");
        if (f.DiasRiesgo > 365) throw ErrorNegocio.Solicitud("Los días de riesgo alto no pueden superar 365.");
    }

    // ── Apoyo ────────────────────────────────────────────────────────────

    /// <summary>Alertas de la cadena (filtro global) limitadas a la sucursal del personal de sucursal.</summary>
    private IQueryable<AlertaInactividad> Visibles()
    {
        if (!actual.EsSuperAdmin && (!actual.EsPersonal || actual.Nivel < Niveles.Asesor))
            throw ErrorNegocio.Prohibido("No tienes acceso a las alertas de inactividad.");
        var q = db.Alertas.AsQueryable();
        if (actual.SucursalFiltro is int propia) q = q.Where(a => a.SucursalId == propia);
        return q;
    }

    private int CadenaPropia() =>
        actual.EsPersonal && actual.CadenaId is int c ? c : throw ErrorNegocio.Prohibido("Los umbrales los configura el gerente de cada cadena.");

    private static DateTime? Max(DateTime a, DateTime b)
    {
        var m = a > b ? a : b;
        return m == default ? null : m;
    }

    private static ReglaInactividadDto ADto(ReglaInactividad r, DateTime? actualizada) =>
        new(r.CadenaId, r.DiasAviso, r.DiasRiesgo, r.SoloConPlanActivo, r.Activa, actualizada);

    private static async Task<List<AlertaDto>> Consulta(IQueryable<AlertaInactividad> q) =>
        await q.Select(a => new AlertaDto(
                a.Id, a.UsuarioId, a.Usuario!.Perfil!.Nombres + " " + a.Usuario.Perfil.Apellidos, a.Usuario.Perfil.Telefono,
                a.SucursalId, a.Sucursal != null ? a.Sucursal.Nombre : null, a.Nivel, a.DiasInactivo, a.UltimaActividad, a.Estado,
                a.DetectadaEn, a.CerradaEn, a.MotivoCierre,
                a.Intervenciones.OrderByDescending(i => i.RegistradaEn)
                    .Select(i => new IntervencionDto(i.Id, i.Tipo, i.Nota, i.RegistradaPor!.Perfil!.Nombres + " " + i.RegistradaPor.Perfil.Apellidos, i.RegistradaEn))
                    .ToList()))
            .ToListAsync();
}
