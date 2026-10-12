using System.Text.Json;
using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Comun;

public partial class PerfilPage : PaginaBase
{
    private bool cargado;
    private bool tieneFecha;

    public PerfilPage()
    {
        InitializeComponent();
        FechaNacimiento.MaximumDate = DateTime.Today;
        FechaNacimiento.DateSelected += (_, _) => tieneFecha = true;
    }

    protected override async Task CargarAsync()
    {
        if (cargado) return;
        await Sesion.ActualizarUsuarioAsync();
        var u = Sesion.Usuario!;
        var p = u.Perfil;
        Nombre.Text = u.NombreCompleto;
        Correo.Text = $"{u.Email} · {u.RolLegible}";
        Iniciales.Text = u.Iniciales;
        Avatar.Source = Api.UrlCompleta(p?.AvatarUrl);
        Nombres.Text = p?.Nombres;
        Apellidos.Text = p?.Apellidos;
        NombreUsuario.Text = p?.NombreUsuario;
        Telefono.Text = p?.Telefono;
        Ci.Text = p?.Ci;
        Genero.SelectedItem = p?.Genero;
        if (p?.FechaNacimiento is { } f)
        {
            FechaNacimiento.Date = f.ToDateTime(TimeOnly.MinValue);
            tieneFecha = true;
        }
        Altura.Text = Numero(p?.AlturaCm);
        Nivel.SelectedItem = p?.NivelExperiencia;
        Condiciones.Text = p?.CondicionesMedicas;
        cargado = true;
    }

    private async void OnGuardar(object? sender, EventArgs e)
    {
        var datos = new
        {
            nombres = Texto(Nombres.Text),
            apellidos = Texto(Apellidos.Text),
            nombreUsuario = Texto(NombreUsuario.Text),
            telefono = Texto(Telefono.Text),
            ci = Texto(Ci.Text),
            genero = Genero.SelectedItem as string,
            fechaNacimiento = tieneFecha && FechaNacimiento.Date is { } d ? DateOnly.FromDateTime(d) : (DateOnly?)null,
            alturaCm = LeerDecimal(Altura.Text),
            nivelExperiencia = Nivel.SelectedItem as string,
            condicionesMedicas = Texto(Condiciones.Text),
        };
        if (await IntentarAsync(async () =>
            {
                await Api.PatchAsync<Perfil>("/api/usuarios/yo/perfil", datos);
                await Sesion.ActualizarUsuarioAsync();
            }))
        {
            Nombre.Text = Sesion.Usuario!.NombreCompleto;
            await AvisarAsync("Perfil", "Tus datos fueron guardados.");
        }
    }

    private async void OnFoto(object? sender, EventArgs e)
    {
        var fotos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { Title = "Elige tu foto", SelectionLimit = 1 });
        var foto = fotos?.FirstOrDefault();
        if (foto is null) return;
        await IntentarAsync(async () =>
        {
            await using var stream = await foto.OpenReadAsync();
            var r = await Api.SubirAsync<JsonElement>("/api/usuarios/yo/avatar", stream, foto.FileName, foto.ContentType ?? "image/jpeg");
            Avatar.Source = Api.UrlCompleta(r.GetProperty("avatarUrl").GetString());
            await Sesion.ActualizarUsuarioAsync();
        });
    }
}
