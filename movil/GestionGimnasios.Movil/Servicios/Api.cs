using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestionGimnasios.Movil.Servicios;

/// <summary>Error que devuelve la API con su mensaje para el usuario ({ "mensaje": "..." }).</summary>
public class ErrorApi(HttpStatusCode estado, string mensaje) : Exception(mensaje)
{
    public HttpStatusCode Estado { get; } = estado;
}

/// <summary>
/// Cliente HTTP de la API de la web. La app nunca accede a la base de datos: todo pasa por aquí
/// con el token JWT de la sesión.
/// </summary>
public class Api
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public string? Token { get; set; }

    /// <summary>Se dispara cuando la API responde 401: la sesión venció o la cuenta fue desactivada.</summary>
    public event Action? SesionVencida;

    public Task<T> GetAsync<T>(string ruta) => EnviarAsync<T>(HttpMethod.Get, ruta, null);
    public Task<T> PostAsync<T>(string ruta, object? cuerpo = null) => EnviarAsync<T>(HttpMethod.Post, ruta, cuerpo);
    public Task<T> PutAsync<T>(string ruta, object? cuerpo = null) => EnviarAsync<T>(HttpMethod.Put, ruta, cuerpo);
    public Task<T> PatchAsync<T>(string ruta, object? cuerpo = null) => EnviarAsync<T>(HttpMethod.Patch, ruta, cuerpo);
    public Task PostAsync(string ruta, object? cuerpo = null) => EnviarAsync<JsonElement?>(HttpMethod.Post, ruta, cuerpo);
    public Task PutAsync(string ruta, object? cuerpo = null) => EnviarAsync<JsonElement?>(HttpMethod.Put, ruta, cuerpo);
    public Task DeleteAsync(string ruta) => EnviarAsync<JsonElement?>(HttpMethod.Delete, ruta, null);

    /// <summary>Sube un archivo como multipart/form-data en el campo "archivo" (avatar, imágenes).</summary>
    public async Task<T> SubirAsync<T>(string ruta, Stream contenido, string nombre, string tipo)
    {
        var archivo = new StreamContent(contenido);
        archivo.Headers.ContentType = new MediaTypeHeaderValue(tipo);
        var form = new MultipartFormDataContent { { archivo, "archivo", nombre } };
        return await EnviarAsync<T>(HttpMethod.Post, ruta, form);
    }

    /// <summary>Las imágenes subidas se guardan en la web con rutas relativas (/uploads/...).</summary>
    public static string? UrlCompleta(string? url) =>
        string.IsNullOrEmpty(url) || url.StartsWith("http") ? url : Sesion.Servidor.TrimEnd('/') + url;

    private async Task<T> EnviarAsync<T>(HttpMethod metodo, string ruta, object? cuerpo)
    {
        using var req = new HttpRequestMessage(metodo, Sesion.Servidor.TrimEnd('/') + ruta);
        if (Token is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        if (cuerpo is HttpContent contenido) req.Content = contenido;
        else if (cuerpo is not null) req.Content = JsonContent.Create(cuerpo, options: Json);

        HttpResponseMessage res;
        try
        {
            res = await http.SendAsync(req);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ErrorApi(HttpStatusCode.ServiceUnavailable, $"No se pudo conectar con el servidor ({Sesion.Servidor}). Revisa la conexión.");
        }

        using (res)
        {
            if (res.StatusCode == HttpStatusCode.Unauthorized && Token is not null && !ruta.StartsWith("/api/auth/login"))
            {
                SesionVencida?.Invoke();
                throw new ErrorApi(res.StatusCode, "Tu sesión expiró. Vuelve a iniciar sesión.");
            }
            if (!res.IsSuccessStatusCode) throw new ErrorApi(res.StatusCode, await MensajeAsync(res));
            if (res.StatusCode == HttpStatusCode.NoContent || res.Content.Headers.ContentLength == 0) return default!;
            return (await res.Content.ReadFromJsonAsync<T>(Json))!;
        }
    }

    private static async Task<string> MensajeAsync(HttpResponseMessage res)
    {
        if (res.StatusCode == HttpStatusCode.TooManyRequests) return "Demasiados intentos. Espera un minuto e inténtalo de nuevo.";
        try
        {
            var json = await res.Content.ReadFromJsonAsync<JsonElement>(Json);
            if (json.TryGetProperty("mensaje", out var m)) return m.GetString() ?? "Error";
        }
        catch (JsonException)
        {
        }
        return res.StatusCode switch
        {
            HttpStatusCode.Forbidden => "No tienes permisos para esta acción.",
            HttpStatusCode.NotFound => "No se encontró lo solicitado.",
            _ => $"Error del servidor ({(int)res.StatusCode}).",
        };
    }
}
