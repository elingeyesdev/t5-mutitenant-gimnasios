using System.Text.Json;

namespace GestionGimnasios.Movil.Paginas.Comun;

public partial class RecuperarPage : PaginaBase
{
    public RecuperarPage(string? email)
    {
        InitializeComponent();
        Email.Text = email;
    }

    private async void OnEnviar(object? sender, EventArgs e)
    {
        if (Texto(Email.Text) is null)
        {
            await AvisarAsync("Correo", "Escribe el correo de tu cuenta.");
            return;
        }
        JsonElement r = default;
        if (await IntentarAsync(async () => r = await Api.PostAsync<JsonElement>("/api/auth/olvide-password", new { email = Email.Text.Trim() })))
        {
            await AvisarAsync("Código enviado", r.GetProperty("mensaje").GetString() ?? "Revisa tu correo.");
            Paso2.IsVisible = true;
        }
    }

    private async void OnRestablecer(object? sender, EventArgs e)
    {
        if (Password.Text != Password2.Text)
        {
            await AvisarAsync("Contraseña", "Las contraseñas no coinciden.");
            return;
        }
        var datos = new { email = Email.Text.Trim(), codigo = Codigo.Text?.Trim() ?? "", password = Password.Text ?? "" };
        if (await IntentarAsync(() => Api.PostAsync("/api/auth/restablecer-password", datos)))
        {
            await AvisarAsync("Listo", "Tu contraseña fue cambiada. Ya puedes iniciar sesión.");
            await Navigation.PopAsync();
        }
    }
}
