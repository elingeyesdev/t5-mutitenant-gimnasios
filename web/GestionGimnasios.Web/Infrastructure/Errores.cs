using Microsoft.AspNetCore.Diagnostics;

namespace GestionGimnasios.Web.Infrastructure;

/// <summary>Regla de negocio incumplida. El mensaje se muestra tal cual al usuario.</summary>
public class ErrorNegocio(int estado, string mensaje) : Exception(mensaje)
{
    public int Estado { get; } = estado;

    public static ErrorNegocio Solicitud(string mensaje) => new(StatusCodes.Status400BadRequest, mensaje);
    public static ErrorNegocio NoAutorizado(string mensaje) => new(StatusCodes.Status401Unauthorized, mensaje);
    public static ErrorNegocio Prohibido(string mensaje) => new(StatusCodes.Status403Forbidden, mensaje);
    public static ErrorNegocio NoEncontrado(string mensaje) => new(StatusCodes.Status404NotFound, mensaje);
    public static ErrorNegocio Conflicto(string mensaje) => new(StatusCodes.Status409Conflict, mensaje);
}

/// <summary>Convierte un ErrorNegocio de la API en una respuesta JSON { mensaje } con su código.</summary>
public class ManejadorErrores(ILogger<ManejadorErrores> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        if (!http.Request.Path.StartsWithSegments("/api")) return false;

        var (estado, mensaje) = ex is ErrorNegocio e
            ? (e.Estado, e.Message)
            : (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.");
        if (estado == StatusCodes.Status500InternalServerError) logger.LogError(ex, "Error no controlado en {Ruta}", http.Request.Path);

        http.Response.StatusCode = estado;
        await http.Response.WriteAsJsonAsync(new { mensaje }, ct);
        return true;
    }
}
