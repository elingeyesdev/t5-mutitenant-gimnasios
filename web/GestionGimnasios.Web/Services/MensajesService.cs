using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record ConversacionDto(int Id, int OtroUsuarioId, string OtroUsuario, string? AvatarUrl, string? UltimoMensaje, DateTime? UltimoEn, int SinLeer);
public record MensajeDto(int Id, int ConversacionId, int RemitenteId, bool EsMio, string Contenido, bool Eliminado, DateTime CreadoEn, DateTime? LeidoEn);
public record VisitaDto(int Id, int SucursalId, string Sucursal, DateTime Entrada, DateTime? Salida, int? DuracionMin);

/// <summary>
/// Mensajería entre un cliente y su entrenador o nutricionista. En el proyecto base el aviso en
/// tiempo real usaba Socket.io; aquí la app consulta la API (el tiempo real queda pendiente de aprobación).
/// </summary>
public class MensajesService(AppDbContext db, UsuarioActual actual, NotificacionesService notificaciones)
{
    public async Task<ConversacionDto> AbrirAsync(int otroUsuarioId)
    {
        var otroNivel = await db.UsuarioRoles.IgnoreQueryFilters().Where(r => r.UsuarioId == otroUsuarioId).Select(r => (int?)r.Rol!.Nivel).MaxAsync() ?? 0;
        int clienteId, asesorId;
        if (actual.Nivel == Niveles.Cliente && otroNivel == Niveles.Asesor) (clienteId, asesorId) = (actual.Id, otroUsuarioId);
        else if (actual.Nivel == Niveles.Asesor && otroNivel == Niveles.Cliente) (clienteId, asesorId) = (otroUsuarioId, actual.Id);
        else throw ErrorNegocio.Prohibido("Solo un cliente puede iniciar chat con un entrenador o nutricionista.");

        var c = await db.Conversaciones.FirstOrDefaultAsync(x => x.ClienteId == clienteId && x.AsesorId == asesorId);
        if (c is null)
        {
            db.Conversaciones.Add(c = new Conversacion { ClienteId = clienteId, AsesorId = asesorId });
            await db.SaveChangesAsync();
        }
        return (await ConversacionesAsync()).First(x => x.Id == c.Id);
    }

    public async Task<List<ConversacionDto>> ConversacionesAsync()
    {
        var lista = await db.Conversaciones.Where(c => c.ClienteId == actual.Id || c.AsesorId == actual.Id)
            .Select(c => new
            {
                c.Id,
                Otro = c.ClienteId == actual.Id ? c.Asesor! : c.Cliente!,
                Ultimo = c.Mensajes.OrderByDescending(m => m.CreadoEn).FirstOrDefault(),
                SinLeer = c.Mensajes.Count(m => m.RemitenteId != actual.Id && m.LeidoEn == null && !m.BorradoParaReceptor),
            })
            .Select(x => new ConversacionDto(x.Id, x.Otro.Id, x.Otro.Perfil!.Nombres + " " + x.Otro.Perfil.Apellidos, x.Otro.Perfil.AvatarUrl,
                x.Ultimo == null ? null : x.Ultimo.Contenido, x.Ultimo == null ? null : x.Ultimo.CreadoEn, x.SinLeer))
            .ToListAsync();
        return lista.OrderByDescending(c => c.UltimoEn ?? DateTime.MinValue).ToList();
    }

    public async Task<List<MensajeDto>> HistorialAsync(int conversacionId, int? despuesDe = null)
    {
        await Miembro(conversacionId);
        var q = db.Mensajes.Where(m => m.ConversacionId == conversacionId);
        if (despuesDe is not null) q = q.Where(m => m.Id > despuesDe);
        var mensajes = await q.OrderBy(m => m.CreadoEn).ToListAsync();
        return mensajes.Where(m => m.RemitenteId == actual.Id ? !m.BorradoParaRemitente : !m.BorradoParaReceptor).Select(ADto).ToList();
    }

    public async Task<MensajeDto> EnviarAsync(int conversacionId, string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido)) throw ErrorNegocio.Solicitud("El mensaje está vacío.");
        if (contenido.Length > 2000) throw ErrorNegocio.Solicitud("El mensaje supera los 2000 caracteres.");
        var c = await Miembro(conversacionId);
        var m = new Mensaje { ConversacionId = conversacionId, RemitenteId = actual.Id, Contenido = contenido.Trim() };
        db.Mensajes.Add(m);
        await db.SaveChangesAsync();
        var receptor = actual.Id == c.ClienteId ? c.AsesorId : c.ClienteId;
        var vista = m.Contenido.Length > 50 ? m.Contenido[..50] + "..." : m.Contenido;
        await notificaciones.NotificarAsync(receptor, "MENSAJE", $"Nuevo mensaje de {actual.Nombre}", vista);
        return ADto(m);
    }

    public async Task MarcarLeidaAsync(int conversacionId)
    {
        await Miembro(conversacionId);
        await db.Mensajes.Where(m => m.ConversacionId == conversacionId && m.RemitenteId != actual.Id && m.LeidoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.LeidoEn, DateTime.UtcNow));
    }

    public async Task<MensajeDto> EliminarMensajeAsync(int mensajeId, bool paraTodos)
    {
        var m = await db.Mensajes.FindAsync(mensajeId) ?? throw ErrorNegocio.NoEncontrado("Mensaje no encontrado.");
        await Miembro(m.ConversacionId);
        var esRemitente = m.RemitenteId == actual.Id;
        if (paraTodos)
        {
            if (!esRemitente) throw ErrorNegocio.Prohibido("Solo el remitente puede eliminar para todos.");
            m.BorradoParaTodos = true;
            m.Contenido = "Mensaje eliminado";
        }
        else if (esRemitente) m.BorradoParaRemitente = true;
        else m.BorradoParaReceptor = true;
        await db.SaveChangesAsync();
        return ADto(m);
    }

    /// <summary>Vacía la conversación para el usuario actual (clearConversation).</summary>
    public async Task VaciarAsync(int conversacionId)
    {
        await Miembro(conversacionId);
        await db.Mensajes.Where(m => m.ConversacionId == conversacionId && m.RemitenteId == actual.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.BorradoParaRemitente, true));
        await db.Mensajes.Where(m => m.ConversacionId == conversacionId && m.RemitenteId != actual.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.BorradoParaReceptor, true));
    }

    private async Task<Conversacion> Miembro(int conversacionId)
    {
        var c = await db.Conversaciones.FindAsync(conversacionId) ?? throw ErrorNegocio.NoEncontrado("Conversación no encontrada.");
        if (c.ClienteId != actual.Id && c.AsesorId != actual.Id) throw ErrorNegocio.Prohibido("No perteneces a esta conversación.");
        return c;
    }

    private MensajeDto ADto(Mensaje m) => new(m.Id, m.ConversacionId, m.RemitenteId, m.RemitenteId == actual.Id, m.Contenido, m.BorradoParaTodos, m.CreadoEn, m.LeidoEn);
}

/// <summary>Visitas registradas por la app del cliente (historial de gimnasios).</summary>
public class VisitasService(AppDbContext db, UsuarioActual actual)
{
    public async Task<VisitaDto> RegistrarAsync(int sucursalId, DateTime entrada, DateTime? salida)
    {
        if (actual.Nivel > Niveles.Cliente) throw ErrorNegocio.Prohibido("Solo los clientes pueden registrar visitas.");
        if (salida is not null && salida < entrada) throw ErrorNegocio.Solicitud("La salida no puede ser anterior a la entrada.");
        if (!await db.Sucursales.AnyAsync(s => s.Id == sucursalId)) throw ErrorNegocio.NoEncontrado("Sucursal no encontrada.");
        var v = new Visita
        {
            UsuarioId = actual.Id, SucursalId = sucursalId, Entrada = entrada.ToUniversalTime(), Salida = salida?.ToUniversalTime(),
            DuracionMin = salida is null ? null : (int)(salida.Value - entrada).TotalMinutes,
        };
        db.Visitas.Add(v);
        await db.SaveChangesAsync();
        return (await MiasAsync()).First(x => x.Id == v.Id);
    }

    public Task<List<VisitaDto>> MiasAsync() =>
        db.Visitas.Where(v => v.UsuarioId == actual.Id).OrderByDescending(v => v.Entrada)
            .Select(v => new VisitaDto(v.Id, v.SucursalId, v.Sucursal!.Nombre, v.Entrada, v.Salida, v.DuracionMin)).ToListAsync();
}
