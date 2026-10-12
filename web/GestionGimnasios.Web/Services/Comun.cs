using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

/// <summary>Hora local de Santa Cruz (America/La_Paz), como APP_TIMEZONE del proyecto base.</summary>
public static class Reloj
{
    private static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");

    public static DateTime Ahora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);
    public static DateOnly Hoy => DateOnly.FromDateTime(Ahora);
    public static DateTime ALocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);
    public static DateTime AUtc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zona);
    public static DateTime InicioDelDiaUtc(DateOnly dia) => AUtc(dia.ToDateTime(TimeOnly.MinValue));
}

/// <summary>
/// Reglas de alcance por sucursal (RT-02). La cadena ya la filtra EF Core; aquí se limita al
/// personal de sucursal (recepcionista, entrenador, instructor) a su propia sucursal.
/// </summary>
public class Alcances(AppDbContext db, UsuarioActual usuario)
{
    /// <summary>Sucursales que el usuario puede administrar (la consulta ya viene filtrada por cadena).</summary>
    public IQueryable<Sucursal> SucursalesGestionables()
    {
        var q = db.Sucursales.AsQueryable();
        if (usuario.SucursalFiltro is int propia) q = q.Where(s => s.Id == propia);
        return q;
    }

    public async Task<Sucursal> ExigirSucursalAsync(int sucursalId, string mensaje = "No tienes jurisdicción sobre esta sucursal.")
    {
        var existe = await db.Sucursales.IgnoreQueryFilters().AnyAsync(s => s.Id == sucursalId);
        if (!existe) throw ErrorNegocio.NoEncontrado($"Sucursal {sucursalId} no encontrada.");
        return await SucursalesGestionables().FirstOrDefaultAsync(s => s.Id == sucursalId)
               ?? throw ErrorNegocio.Prohibido(mensaje);
    }

    /// <summary>Para el personal de sucursal la sucursal se toma del token (anti-manipulación, como enforceGymId).</summary>
    public int ForzarSucursal(int sucursalSolicitada) =>
        usuario.EsPersonal && usuario.Nivel < Niveles.Gerente
            ? usuario.SucursalId ?? throw ErrorNegocio.Prohibido("No tienes una sucursal asignada.")
            : sucursalSolicitada;
}

/// <summary>Página de resultados con el total, como { data, meta } del proyecto base.</summary>
public record Pagina<T>(IReadOnlyList<T> Datos, int Total, int Limite, int Desde);

/// <summary>Guarda archivos en wwwroot/uploads (el proyecto base usaba Cloudinary y uploads/ locales).</summary>
public class Almacenamiento(IWebHostEnvironment env)
{
    private static readonly HashSet<string> Permitidas = [".jpg", ".jpeg", ".png", ".webp"];

    public async Task<string> GuardarImagenAsync(IFormFile archivo, string carpeta)
    {
        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!Permitidas.Contains(ext)) throw ErrorNegocio.Solicitud("Formato de imagen no permitido (jpg, png o webp).");
        if (archivo.Length > 5 * 1024 * 1024) throw ErrorNegocio.Solicitud("La imagen supera los 5 MB.");

        var dir = Path.Combine(env.WebRootPath, "uploads", carpeta);
        Directory.CreateDirectory(dir);
        var nombre = $"{Guid.NewGuid():N}{ext}";
        await using var destino = File.Create(Path.Combine(dir, nombre));
        await archivo.CopyToAsync(destino);
        return $"/uploads/{carpeta}/{nombre}";
    }

    public void Eliminar(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/")) return;
        var ruta = Path.Combine(env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(ruta)) File.Delete(ruta);
    }
}
