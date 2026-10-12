using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Comun;

public partial class RegistroPage : PaginaBase
{
    public RegistroPage() => InitializeComponent();

    private async void OnCrear(object? sender, EventArgs e)
    {
        if (Texto(Nombres.Text) is null || Texto(Apellidos.Text) is null || Texto(Email.Text) is null || string.IsNullOrEmpty(Password.Text))
        {
            await AvisarAsync("Datos incompletos", "Completa los campos marcados con *.");
            return;
        }
        if (Password.Text != Password2.Text)
        {
            await AvisarAsync("Contraseña", "Las contraseñas no coinciden.");
            return;
        }
        var datos = new
        {
            email = Email.Text.Trim(),
            password = Password.Text,
            nombres = Nombres.Text.Trim(),
            apellidos = Apellidos.Text.Trim(),
            telefono = Texto(Telefono.Text),
            ci = Texto(Ci.Text),
            genero = Genero.SelectedItem as string,
        };
        if (await IntentarAsync(() => Sesion.RegistrarAsync(datos)))
            App.EntrarAlMenu();
    }
}
