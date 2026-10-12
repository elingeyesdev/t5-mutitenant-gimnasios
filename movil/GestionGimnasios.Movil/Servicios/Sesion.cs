using System.Text.Json;
using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Servicios;

/// <summary>Sesión del usuario: token guardado de forma segura en el dispositivo y datos del usuario.</summary>
public static class Sesion
{
    private const string ClaveToken = "token";
    private const string ClaveUsuario = "usuario";
    private const string ClaveServidor = "servidor";

    /// <summary>En el emulador de Android, 10.0.2.2 es la computadora donde corre la web.</summary>
    public const string ServidorPorDefecto = "http://10.0.2.2:5253";

    public static Api Api { get; } = new();
    public static Usuario? Usuario { get; private set; }

    public static string Servidor
    {
        get => Preferences.Default.Get(ClaveServidor, DeviceInfo.Platform == DevicePlatform.Android ? ServidorPorDefecto : "http://localhost:5253");
        set => Preferences.Default.Set(ClaveServidor, value.Trim());
    }

    public static bool Iniciada => Usuario is not null && Api.Token is not null;
    public static int Nivel => Usuario?.Nivel ?? 0;
    public static string Rol => Usuario?.Rol ?? "";
    public static bool EsCliente => Rol == "CLIENTE";

    public static async Task<bool> RestaurarAsync()
    {
        var token = await SecureStorage.Default.GetAsync(ClaveToken);
        var json = Preferences.Default.Get<string?>(ClaveUsuario, null);
        if (token is null || json is null) return false;
        Api.Token = token;
        Usuario = JsonSerializer.Deserialize<Usuario>(json, Api.Json);
        try
        {
            // Confirma que el token sigue vigente y refresca los datos del usuario.
            await ActualizarUsuarioAsync();
            return true;
        }
        catch (ErrorApi e) when (e.Estado != System.Net.HttpStatusCode.ServiceUnavailable)
        {
            Cerrar();
            return false;
        }
        catch (ErrorApi)
        {
            return Usuario is not null; // sin conexión: se mantiene la sesión guardada
        }
    }

    public static async Task IniciarAsync(string email, string password)
    {
        var r = await Api.PostAsync<SesionIniciada>("/api/auth/login", new { email, password });
        await GuardarAsync(r);
    }

    public static async Task RegistrarAsync(object datos)
    {
        var r = await Api.PostAsync<SesionIniciada>("/api/auth/registro", datos);
        await GuardarAsync(r);
    }

    public static async Task ActualizarUsuarioAsync()
    {
        Usuario = await Api.GetAsync<Usuario>("/api/auth/yo");
        Preferences.Default.Set(ClaveUsuario, JsonSerializer.Serialize(Usuario, Api.Json));
    }

    public static void Cerrar()
    {
        Api.Token = null;
        Usuario = null;
        SecureStorage.Default.Remove(ClaveToken);
        Preferences.Default.Remove(ClaveUsuario);
    }

    private static async Task GuardarAsync(SesionIniciada r)
    {
        Api.Token = r.Token;
        Usuario = r.Usuario;
        await SecureStorage.Default.SetAsync(ClaveToken, r.Token);
        Preferences.Default.Set(ClaveUsuario, JsonSerializer.Serialize(r.Usuario, Api.Json));
    }
}
