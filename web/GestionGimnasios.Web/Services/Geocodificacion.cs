using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace GestionGimnasios.Web.Services;

/// <summary>Búsqueda de direcciones para el mapa de sucursales (sección "Geocodificacion" de appsettings).</summary>
public sealed class OpcionesGeocodificacion
{
    /// <summary>En false el mapa funciona solo marcando con clic, sin consultar ningún servicio.</summary>
    public bool Habilitada { get; set; } = true;

    /// <summary>Servidor compatible con la API de Nominatim. Se cambia por configuración, sin recompilar.</summary>
    public string Url { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>Identifica a la aplicación ante el servicio, como pide su política de uso.</summary>
    public string UserAgent { get; set; } = "GestionGimnasios/1.0 (panel de sucursales)";

    public string Pais { get; set; } = "bo";

    public int HorasCache { get; set; } = 24;
}

public sealed record LugarEncontrado(double Latitud, double Longitud);

public sealed record DireccionSugerida(string Direccion, string? Ciudad);

/// <summary>Disponible = false cuando el servicio no respondió; Valor = null cuando no hubo resultado.</summary>
public sealed record Consulta<T>(bool Disponible, T? Valor) where T : class;

/// <summary>
/// Turno único para toda la aplicación: como máximo una consulta por intervalo al servicio de
/// direcciones, sin importar cuántos usuarios busquen a la vez. Si la cola se alarga o el servicio
/// pide esperar (429/403), se responde "no disponible" y el usuario marca la sucursal con un clic.
/// </summary>
public sealed class TurnoGeocodificacion(TimeSpan intervalo, TimeSpan esperaMaxima)
{
    private readonly SemaphoreSlim turno = new(1, 1);
    private DateTimeOffset ultima = DateTimeOffset.MinValue;
    private long pausadoHasta;

    public TurnoGeocodificacion() : this(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)) { }

    public async Task<bool> EsperarAsync(CancellationToken ct = default)
    {
        if (DateTimeOffset.UtcNow.UtcTicks < Interlocked.Read(ref pausadoHasta)) return false;
        if (!await turno.WaitAsync(esperaMaxima, ct)) return false;
        try
        {
            var espera = ultima + intervalo - DateTimeOffset.UtcNow;
            if (espera > TimeSpan.Zero) await Task.Delay(espera, ct);
            ultima = DateTimeOffset.UtcNow;
            return true;
        }
        finally
        {
            turno.Release();
        }
    }

    public void Pausar(TimeSpan tiempo) => Interlocked.Exchange(ref pausadoHasta, (DateTimeOffset.UtcNow + tiempo).UtcTicks);
}

/// <summary>
/// El navegador nunca consulta el servicio de direcciones directamente: pasa por aquí, que aplica
/// el turno común, guarda las respuestas en caché y se identifica con un User-Agent propio.
/// </summary>
public sealed class GeocodificacionService(
    HttpClient http, IMemoryCache cache, TurnoGeocodificacion turno, OpcionesGeocodificacion opciones,
    ILogger<GeocodificacionService> log)
{
    public static readonly TimeSpan PausaSiElServicioRechaza = TimeSpan.FromMinutes(5);

    public static void Configurar(HttpClient cliente, OpcionesGeocodificacion opciones)
    {
        cliente.BaseAddress = new Uri(opciones.Url.TrimEnd('/') + "/");
        cliente.Timeout = TimeSpan.FromSeconds(8);
        cliente.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", opciones.UserAgent);
    }

    public Task<Consulta<LugarEncontrado>> BuscarAsync(string? texto, string? ciudad, CancellationToken ct = default)
    {
        texto = texto?.Trim() ?? "";
        if (texto.Length is < 3 or > 200) return Task.FromResult(new Consulta<LugarEncontrado>(true, null));
        var consulta = string.IsNullOrWhiteSpace(ciudad) ? texto : $"{texto}, {ciudad.Trim()}";
        var ruta = $"search?format=jsonv2&limit=1&countrycodes={Uri.EscapeDataString(opciones.Pais)}&accept-language=es&q={Uri.EscapeDataString(consulta)}";
        return ConsultarAsync("geo:buscar:" + consulta.ToLowerInvariant(), ruta, LeerLugar, ct);
    }

    public Task<Consulta<DireccionSugerida>> DireccionAsync(double latitud, double longitud, CancellationToken ct = default)
    {
        if (latitud is < -90 or > 90 || longitud is < -180 or > 180)
            return Task.FromResult(new Consulta<DireccionSugerida>(true, null));
        // Redondeado a unos 11 m: clics muy cercanos reutilizan la misma respuesta.
        var la = Math.Round(latitud, 4).ToString(CultureInfo.InvariantCulture);
        var lo = Math.Round(longitud, 4).ToString(CultureInfo.InvariantCulture);
        var ruta = $"reverse?format=jsonv2&accept-language=es&zoom=18&lat={la}&lon={lo}";
        return ConsultarAsync($"geo:direccion:{la},{lo}", ruta, LeerDireccion, ct);
    }

    private async Task<Consulta<T>> ConsultarAsync<T>(string clave, string ruta, Func<JsonElement, T?> leer, CancellationToken ct)
        where T : class
    {
        var noDisponible = new Consulta<T>(false, null);
        if (!opciones.Habilitada) return noDisponible;
        if (cache.TryGetValue(clave, out Consulta<T>? guardada) && guardada is not null) return guardada;
        if (!await turno.EsperarAsync(ct)) return noDisponible;
        try
        {
            using var respuesta = await http.GetAsync(ruta, ct);
            if (respuesta.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.Forbidden)
            {
                turno.Pausar(PausaSiElServicioRechaza);
                log.LogWarning("El servicio de direcciones respondió {Estado}; la búsqueda queda en pausa.", (int)respuesta.StatusCode);
                return noDisponible;
            }
            if (!respuesta.IsSuccessStatusCode) return noDisponible;
            await using var contenido = await respuesta.Content.ReadAsStreamAsync(ct);
            using var json = await JsonDocument.ParseAsync(contenido, cancellationToken: ct);
            var resultado = new Consulta<T>(true, leer(json.RootElement));
            cache.Set(clave, resultado, TimeSpan.FromHours(opciones.HorasCache));
            return resultado;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            log.LogWarning(e, "No se pudo consultar el servicio de direcciones.");
            return noDisponible;
        }
    }

    public static LugarEncontrado? LeerLugar(JsonElement raiz)
    {
        if (raiz.ValueKind != JsonValueKind.Array || raiz.GetArrayLength() == 0) return null;
        var primero = raiz[0];
        return Numero(primero, "lat") is { } la && Numero(primero, "lon") is { } lo ? new LugarEncontrado(la, lo) : null;
    }

    public static DireccionSugerida? LeerDireccion(JsonElement raiz)
    {
        if (raiz.ValueKind != JsonValueKind.Object || !raiz.TryGetProperty("address", out var a)) return null;
        var calle = Unir(" ", Texto(a, "road"), Texto(a, "house_number"));
        var direccion = Unir(", ", calle, Texto(a, "neighbourhood") ?? Texto(a, "suburb"));
        var ciudad = Texto(a, "city") ?? Texto(a, "town") ?? Texto(a, "village");
        return direccion.Length == 0 && ciudad is null ? null : new DireccionSugerida(direccion, ciudad);
    }

    private static string? Texto(JsonElement e, string nombre) =>
        e.TryGetProperty(nombre, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
            ? v.GetString()!.Trim()
            : null;

    private static double? Numero(JsonElement e, string nombre) =>
        double.TryParse(Texto(e, nombre), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : null;

    private static string Unir(string separador, params string?[] partes) =>
        string.Join(separador, partes.Where(p => !string.IsNullOrEmpty(p)));
}
