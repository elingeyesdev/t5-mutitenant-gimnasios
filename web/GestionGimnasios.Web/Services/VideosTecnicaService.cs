using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

/// <summary>Lo que ve el socio: sin el análisis preliminar y con la valoración solo cuando el entrenador ya revisó.</summary>
public record VideoSocioDto(int Id, string Ejercicio, string? Comentario, string Entrenador, DateTime EnviadoEn, string Estado, string? Valoracion, string? Observacion, DateTime? RevisadoEn);

public record VideoRevisionDto(
    int Id, int ClienteId, string Socio, string Ejercicio, string GrupoMuscular, string? Comentario, DateTime EnviadoEn, long TamanoBytes,
    string Estado, string? AnalisisPreliminar, string? Valoracion, string? Observacion, DateTime? RevisadoEn);

public record ArchivoVideo(string Ruta, string TipoContenido);

public class VideoForm
{
    public int EjercicioId { get; set; }
    public string? Comentario { get; set; }
    public IFormFile? Archivo { get; set; }
}

public class RevisionVideoForm
{
    public string Valoracion { get; set; } = "";
    public string Observacion { get; set; } = "";
}

/// <summary>
/// Revisión de la técnica por video (RF-01, CU-35). El socio envía el video a su entrenador; el
/// entrenador lo evalúa y solo entonces el socio recibe la devolución. Sin una tecnología de
/// análisis aprobada, el análisis preliminar figura como no disponible (alterno 2a).
/// </summary>
public class VideosTecnicaService(AppDbContext db, UsuarioActual actual, ArchivosPrivados archivos, NotificacionesService notificaciones)
{
    public const long MaximoBytes = 50 * 1024 * 1024;
    public const int MaximoPendientes = 3;
    private static readonly Dictionary<string, string> Tipos = new()
    {
        [".mp4"] = "video/mp4", [".mov"] = "video/quicktime", [".webm"] = "video/webm",
    };

    // ── Socio ────────────────────────────────────────────────────────────

    public async Task<VideoSocioDto> EnviarAsync(VideoForm f)
    {
        var comentario = ValidarComentario(f.Comentario);
        if (f.Archivo is null) throw ErrorNegocio.Solicitud("Adjunta el video del ejercicio.");
        var extension = await ValidarArchivoAsync(f.Archivo);

        var asesoria = await db.Asesorias
                           .Where(a => a.ClienteId == actual.Id && a.Estado == EstadosAsesoria.Activa
                                       && db.UsuarioRoles.IgnoreQueryFilters().Any(r => r.UsuarioId == a.AsesorId && r.Rol!.Nombre == Roles.Entrenador))
                           .FirstOrDefaultAsync()
                       ?? throw ErrorNegocio.Conflicto("Necesitas una asesoría activa con un entrenador para enviar videos.");
        var ejercicio = await db.Ejercicios.FirstOrDefaultAsync(e => e.Id == f.EjercicioId && e.Activo)
                        ?? throw ErrorNegocio.NoEncontrado("Ejercicio no encontrado.");
        if (await db.VideosTecnica.CountAsync(v => v.ClienteId == actual.Id && v.Estado == EstadosVideoTecnica.Pendiente) >= MaximoPendientes)
            throw ErrorNegocio.Conflicto($"Ya tienes {MaximoPendientes} videos esperando revisión. Espera la devolución de tu entrenador.");

        var video = new VideoTecnica
        {
            ClienteId = actual.Id, AsesorId = asesoria.AsesorId, EjercicioId = ejercicio.Id, Comentario = comentario,
            Archivo = await archivos.GuardarAsync(f.Archivo, "videos", extension), TipoContenido = Tipos[extension], TamanoBytes = f.Archivo.Length,
        };
        db.VideosTecnica.Add(video);
        await db.SaveChangesAsync();
        await notificaciones.NotificarAsync(asesoria.AsesorId, TiposNotificacion.VideoTecnica, "Nuevo video de técnica",
            $"{actual.Nombre} envió un video de {ejercicio.Nombre} para revisar.");
        return (await MisVideosAsync()).First(v => v.Id == video.Id);
    }

    public async Task<List<VideoSocioDto>> MisVideosAsync() =>
        (await ConDatos(db.VideosTecnica.Where(v => v.ClienteId == actual.Id)).OrderByDescending(v => v.EnviadoEn).ToListAsync())
        .Select(ADevolucion).ToList();

    public async Task<ArchivoVideo> ArchivoPropioAsync(int id)
    {
        var v = await db.VideosTecnica.FirstOrDefaultAsync(x => x.Id == id && x.ClienteId == actual.Id)
                ?? throw ErrorNegocio.NoEncontrado("Video no encontrado.");
        return Archivo(v);
    }

    // ── Entrenador ───────────────────────────────────────────────────────

    /// <summary>Videos de los alumnos del entrenador. Primero los que esperan revisión.</summary>
    public async Task<List<VideoRevisionDto>> RecibidosAsync(string? estado = null)
    {
        var q = Recibidos();
        if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(v => v.Estado == estado);
        return (await ConDatos(q).OrderBy(v => v.Estado == EstadosVideoTecnica.Pendiente ? 0 : 1).ThenBy(v => v.EnviadoEn).Take(200).ToListAsync())
            .Select(ARevision).ToList();
    }

    public async Task<VideoRevisionDto> ObtenerAsync(int id) =>
        ARevision(await ConDatos(Recibidos()).FirstOrDefaultAsync(v => v.Id == id) ?? throw ErrorNegocio.NoEncontrado("Video no encontrado."));

    public async Task<ArchivoVideo> ArchivoAsync(int id) =>
        Archivo(await Recibidos().FirstOrDefaultAsync(v => v.Id == id) ?? throw ErrorNegocio.NoEncontrado("Video no encontrado."));

    /// <summary>El entrenador deja su valoración final. Desde aquí el socio ve la devolución y ya no se modifica.</summary>
    public async Task<VideoRevisionDto> RevisarAsync(int id, RevisionVideoForm f)
    {
        var observacion = ValidarRevision(f);
        var v = await Recibidos().Include(x => x.Ejercicio).FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado("Video no encontrado.");
        if (v.Estado != EstadosVideoTecnica.Pendiente) throw ErrorNegocio.Conflicto("Este video ya fue revisado.");

        v.Valoracion = f.Valoracion;
        v.Observacion = observacion;
        v.Estado = EstadosVideoTecnica.Revisado;
        v.RevisadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await notificaciones.NotificarAsync(v.ClienteId, TiposNotificacion.VideoTecnica, "Tu video fue revisado",
            $"{actual.Nombre} dejó su devolución sobre tu técnica en {v.Ejercicio?.Nombre}.");
        return await ObtenerAsync(id);
    }

    // ── Reglas ───────────────────────────────────────────────────────────

    /// <summary>Comprueba los primeros bytes: el archivo debe ser un video de verdad, no solo tener la extensión.</summary>
    public static bool FirmaValida(string extension, ReadOnlySpan<byte> inicio)
    {
        if (inicio.Length < 12) return false;
        var caja = System.Text.Encoding.ASCII.GetString(inicio.Slice(4, 4));
        return extension switch
        {
            ".mp4" => caja == "ftyp",
            ".mov" => caja is "ftyp" or "moov" or "mdat" or "wide",
            ".webm" => inicio[0] == 0x1A && inicio[1] == 0x45 && inicio[2] == 0xDF && inicio[3] == 0xA3,
            _ => false,
        };
    }

    public static string? ValidarComentario(string? comentario)
    {
        var texto = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
        if (texto?.Length > 500) throw ErrorNegocio.Solicitud("El comentario no puede superar los 500 caracteres.");
        return texto;
    }

    public static string ValidarRevision(RevisionVideoForm f)
    {
        if (!ValoracionesTecnica.Todas.Contains(f.Valoracion)) throw ErrorNegocio.Solicitud("Elige una valoración.");
        var observacion = f.Observacion?.Trim() ?? "";
        if (observacion.Length == 0) throw ErrorNegocio.Solicitud("Escribe tu observación para el socio.");
        if (observacion.Length > 1000) throw ErrorNegocio.Solicitud("La observación no puede superar los 1000 caracteres.");
        return observacion;
    }

    /// <summary>Mientras el video no fue revisado, el socio no recibe ninguna valoración.</summary>
    public static VideoSocioDto ADevolucion(VideoTecnica v)
    {
        var revisado = v.Estado == EstadosVideoTecnica.Revisado;
        return new VideoSocioDto(v.Id, v.Ejercicio?.Nombre ?? "", v.Comentario, v.Asesor?.Perfil?.NombreCompleto ?? "", v.EnviadoEn, v.Estado,
            revisado ? v.Valoracion : null, revisado ? v.Observacion : null, revisado ? v.RevisadoEn : null);
    }

    // ── Apoyo ────────────────────────────────────────────────────────────

    /// <summary>Solo el entrenador asignado y mientras la asesoría siga activa: los videos son datos personales del socio.</summary>
    private IQueryable<VideoTecnica> Recibidos() =>
        db.VideosTecnica.Where(v => v.AsesorId == actual.Id
                                    && db.Asesorias.Any(a => a.ClienteId == v.ClienteId && a.AsesorId == actual.Id && a.Estado == EstadosAsesoria.Activa));

    private static async Task<string> ValidarArchivoAsync(IFormFile archivo)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!Tipos.ContainsKey(extension)) throw ErrorNegocio.Solicitud("Formato de video no permitido (mp4, mov o webm).");
        if (archivo.Length > MaximoBytes) throw ErrorNegocio.Solicitud("El video supera los 50 MB.");

        var inicio = new byte[12];
        await using var lectura = archivo.OpenReadStream();
        var leidos = await lectura.ReadAtLeastAsync(inicio, inicio.Length, throwOnEndOfStream: false);
        if (!FirmaValida(extension, inicio.AsSpan(0, leidos))) throw ErrorNegocio.Solicitud("El archivo no es un video válido.");
        return extension;
    }

    private ArchivoVideo Archivo(VideoTecnica v)
    {
        var ruta = archivos.RutaCompleta(v.Archivo);
        if (!File.Exists(ruta)) throw ErrorNegocio.NoEncontrado("No se encontró el archivo del video.");
        return new ArchivoVideo(ruta, v.TipoContenido);
    }

    private static IQueryable<VideoTecnica> ConDatos(IQueryable<VideoTecnica> q) =>
        q.Include(v => v.Ejercicio).Include(v => v.Cliente).ThenInclude(u => u!.Perfil).Include(v => v.Asesor).ThenInclude(u => u!.Perfil);

    private static VideoRevisionDto ARevision(VideoTecnica v) => new(
        v.Id, v.ClienteId, v.Cliente?.Perfil?.NombreCompleto ?? "", v.Ejercicio?.Nombre ?? "", v.Ejercicio?.GrupoMuscular ?? "", v.Comentario, v.EnviadoEn, v.TamanoBytes,
        v.Estado, v.AnalisisPreliminar, v.Valoracion, v.Observacion, v.RevisadoEn);
}
