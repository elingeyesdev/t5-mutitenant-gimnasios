using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Comun;

public partial class LoginPage : PaginaBase
{
    public LoginPage()
    {
        InitializeComponent();
        MostrarServidor();
    }

    private void MostrarServidor() => Servidor.Text = $"Servidor: {Sesion.Servidor} (toca para cambiar)";

    private async void OnEntrar(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Email.Text) || string.IsNullOrEmpty(Password.Text))
        {
            await AvisarAsync("Datos incompletos", "Escribe tu correo y tu contraseña.");
            return;
        }
        if (await IntentarAsync(() => Sesion.IniciarAsync(Email.Text.Trim(), Password.Text)))
            App.EntrarAlMenu();
    }

    private async void OnRegistro(object? sender, EventArgs e) => await IrAsync(new RegistroPage());

    private async void OnRecuperar(object? sender, EventArgs e) => await IrAsync(new RecuperarPage(Email.Text));

    /// <summary>Permite apuntar la app a otra computadora (por ejemplo un celular físico en la misma red Wi-Fi).</summary>
    private async void OnServidor(object? sender, EventArgs e)
    {
        var url = await DisplayPromptAsync("Servidor de la API",
            "Dirección de la web. En el emulador de Android usa http://10.0.2.2:5253; en un celular, la IP de la computadora.",
            "Guardar", "Cancelar", initialValue: Sesion.Servidor, keyboard: Keyboard.Url);
        if (string.IsNullOrWhiteSpace(url)) return;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            await AvisarAsync("Dirección no válida", "Usa el formato http://IP:puerto.");
            return;
        }
        Sesion.Servidor = url;
        MostrarServidor();
    }
}
