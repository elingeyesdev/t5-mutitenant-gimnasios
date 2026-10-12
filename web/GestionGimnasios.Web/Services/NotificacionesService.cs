using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record NotificacionDto(int Id, string Tipo, string Titulo, string Cuerpo, DateTime EnviadaEn, bool Leida);
public record PreferenciasDto(bool Push, bool ConfirmacionesReserva, bool RecordatoriosClase, bool AlertasCancelacion, bool Promociones);
public record PlantillaDto(int Id, string Tipo, string Titulo, string Cuerpo);

public class AvisoForm
{
    public string Titulo { get; set; } = "";
    public string Cuerpo { get; set; } = "";
    public int? PlantillaId { get; set; }
    /// <summary>Destinatarios: un usuario, una sucursal o toda la cadena (si ambos son nulos).</summary>
    public int? UsuarioId { get; set; }
    public int? SucursalId { get; set; }
    /// <summary>Solo clientes, solo personal o todos.</summary>
    public string Destino { get; set; } = "TODOS";
}

public static class TiposNotificacion
{
    public const string General = "GENERAL";
    public const string Recordatorio = "RECORDATORIO_RESERVA";
    public const string ReservaCancelada = "RESERVA_CANCELADA";
    public const string ReservaConfirmada = "RESERVA_CONFIRMADA";
    public const string IngresoRegistrado = "INGRESO_REGISTRADO";
    public const string Inactividad = "ALERTA_INACTIVIDAD";
    public const string Asesoria = "ASESORIA";
    public const string CupoLiberado = "CUPO_LIBERADO";
}

/// <summary>
/// Notificaciones dentro de la app y del panel (CU-30). Las push (Expo Push en el proyecto base)
/// quedan pendientes de aprobación del docente.
/// </summary>
public class NotificacionesService(AppDbContext db, UsuarioActual actual, UsuariosService usuarios)
{
    /// <summary>Crea una notificación respetando las preferencias del usuario.</summary>
    public async Task NotificarAsync(int usuarioId, string tipo, string titulo, string cuerpo, bool guardar = true)
    {
        var pref = await db.PreferenciasNotificacion.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);
        if (pref is not null)
        {
            var permitido = tipo switch
            {
                TiposNotificacion.Recordatorio => pref.RecordatoriosClase,
                TiposNotificacion.ReservaCancelada => pref.AlertasCancelacion,
                TiposNotificacion.ReservaConfirmada or TiposNotificacion.IngresoRegistrado => pref.ConfirmacionesReserva,
                _ => true,
            };
            if (!permitido) return;
        }
        db.Notificaciones.Add(new Notificacion { UsuarioId = usuarioId, Tipo = tipo, Titulo = titulo, Cuerpo = cuerpo });
        if (guardar) await db.SaveChangesAsync();
    }

    public async Task<List<NotificacionDto>> MiasAsync(int limite = 50) =>
        await db.Notificaciones.Where(n => n.UsuarioId == actual.Id).OrderByDescending(n => n.EnviadaEn).Take(limite)
            .Select(n => new NotificacionDto(n.Id, n.Tipo, n.Titulo, n.Cuerpo, n.EnviadaEn, n.LeidaEn != null)).ToListAsync();

    public Task<int> SinLeerAsync() => db.Notificaciones.CountAsync(n => n.UsuarioId == actual.Id && n.LeidaEn == null);

    public async Task MarcarLeidaAsync(int id)
    {
        var n = await db.Notificaciones.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == actual.Id)
                ?? throw ErrorNegocio.NoEncontrado("Notificación no encontrada.");
        n.LeidaEn ??= DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public Task MarcarTodasLeidasAsync() =>
        db.Notificaciones.Where(n => n.UsuarioId == actual.Id && n.LeidaEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.LeidaEn, DateTime.UtcNow));

    public async Task<PreferenciasDto> PreferenciasAsync()
    {
        var p = await db.PreferenciasNotificacion.FirstOrDefaultAsync(x => x.UsuarioId == actual.Id) ?? new PreferenciaNotificacion();
        return new PreferenciasDto(p.Push, p.ConfirmacionesReserva, p.RecordatoriosClase, p.AlertasCancelacion, p.Promociones);
    }

    public async Task<PreferenciasDto> GuardarPreferenciasAsync(PreferenciasDto f)
    {
        var p = await db.PreferenciasNotificacion.FirstOrDefaultAsync(x => x.UsuarioId == actual.Id);
        if (p is null) db.PreferenciasNotificacion.Add(p = new PreferenciaNotificacion { UsuarioId = actual.Id });
        p.Push = f.Push;
        p.ConfirmacionesReserva = f.ConfirmacionesReserva;
        p.RecordatoriosClase = f.RecordatoriosClase;
        p.AlertasCancelacion = f.AlertasCancelacion;
        p.Promociones = f.Promociones;
        p.ActualizadaEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return f;
    }

    // ── Plantillas y avisos del personal ─────────────────────────────────

    public Task<List<PlantillaDto>> PlantillasAsync() =>
        db.Plantillas.OrderBy(p => p.Tipo).Select(p => new PlantillaDto(p.Id, p.Tipo, p.Titulo, p.Cuerpo)).ToListAsync();

    public async Task<PlantillaDto> GuardarPlantillaAsync(PlantillaDto f)
    {
        var tipo = f.Tipo.Trim().ToUpperInvariant().Replace(' ', '_');
        if (string.IsNullOrEmpty(tipo) || string.IsNullOrWhiteSpace(f.Titulo)) throw ErrorNegocio.Solicitud("Tipo y título son obligatorios.");
        var p = f.Id > 0 ? await db.Plantillas.FindAsync(f.Id) : await db.Plantillas.FirstOrDefaultAsync(x => x.Tipo == tipo);
        if (p is null) db.Plantillas.Add(p = new PlantillaNotificacion());
        else if (await db.Plantillas.AnyAsync(x => x.Tipo == tipo && x.Id != p.Id)) throw ErrorNegocio.Conflicto("Ya existe una plantilla con ese tipo.");
        p.Tipo = tipo;
        p.Titulo = f.Titulo;
        p.Cuerpo = f.Cuerpo;
        await db.SaveChangesAsync();
        return new PlantillaDto(p.Id, p.Tipo, p.Titulo, p.Cuerpo);
    }

    /// <summary>El gerente o recepcionista envía un aviso a usuarios de su alcance; devuelve cuántos lo recibieron.</summary>
    public async Task<int> EnviarAvisoAsync(AvisoForm f)
    {
        if (string.IsNullOrWhiteSpace(f.Titulo) || string.IsNullOrWhiteSpace(f.Cuerpo)) throw ErrorNegocio.Solicitud("Título y mensaje son obligatorios.");
        var q = usuarios.UsuariosVisibles().Where(u => u.Activo);
        if (f.UsuarioId is not null) q = q.Where(u => u.Id == f.UsuarioId);
        if (f.SucursalId is not null) q = q.Where(u => u.Roles.Any(r => r.SucursalId == f.SucursalId) || u.Membresias.Any(m => m.SucursalId == f.SucursalId));
        if (f.Destino == "CLIENTES") q = q.Where(u => u.Roles.Any(r => r.Rol!.Nombre == Roles.Cliente));
        else if (f.Destino == "PERSONAL") q = q.Where(u => u.Roles.Any(r => r.Rol!.Nivel > Niveles.Cliente));
        var ids = await q.Select(u => u.Id).ToListAsync();
        if (f.UsuarioId is not null && ids.Count == 0) throw ErrorNegocio.Prohibido("El usuario no está en tu alcance.");
        foreach (var id in ids)
            db.Notificaciones.Add(new Notificacion { UsuarioId = id, PlantillaId = f.PlantillaId, Tipo = TiposNotificacion.General, Titulo = f.Titulo.Trim(), Cuerpo = f.Cuerpo.Trim() });
        await db.SaveChangesAsync();
        return ids.Count;
    }
}
