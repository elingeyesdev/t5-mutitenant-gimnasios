using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>
/// Carnet virtual del socio. El original mostraba un QR; mientras el docente no apruebe una librería
/// de QR, recepción escribe el número de socio en su escáner para registrar el ingreso o la salida.
/// </summary>
public class CarnetPage : PaginaBase
{
    private readonly Label estado = new() { HorizontalTextAlignment = TextAlignment.Center, FontFamily = "OpenSansSemibold" };
    private readonly Label plan = new() { HorizontalTextAlignment = TextAlignment.Center, Style = Estilo("Detalle") };

    public CarnetPage()
    {
        Title = "Carnet virtual";
        var u = Sesion.Usuario!;
        DeviceDisplay.Current.KeepScreenOn = true;
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 10,
                Children =
                {
                    new Border
                    {
                        Style = Estilo("Tarjeta"),
                        Padding = new Thickness(20, 28),
                        Content = new VerticalStackLayout
                        {
                            Spacing = 8,
                            Children =
                            {
                                new Label { Text = "GESTIÓN GIMNASIOS", Style = Estilo("Seccion"), HorizontalOptions = LayoutOptions.Center },
                                new Label { Text = u.NombreCompleto, Style = Estilo("Titulo"), HorizontalTextAlignment = TextAlignment.Center },
                                new Label { Text = $"CI {u.Perfil?.Ci ?? "—"} · {u.Lugar}", Style = Estilo("Detalle"), HorizontalTextAlignment = TextAlignment.Center },
                                new BoxView { HeightRequest = 1, Color = Colores.Gris, Margin = new Thickness(0, 12) },
                                new Label { Text = "Número de socio", Style = Estilo("Detalle"), HorizontalTextAlignment = TextAlignment.Center },
                                new Label { Text = u.Id.ToString("D6"), Style = Estilo("Codigo") },
                                estado,
                                plan,
                            },
                        },
                    },
                    new Label
                    {
                        Text = "Muestra este número en recepción al entrar y al salir del gimnasio.",
                        Style = Estilo("Detalle"),
                        HorizontalTextAlignment = TextAlignment.Center,
                    },
                },
            },
        };
    }

    protected override async Task CargarAsync()
    {
        var activa = await Api.GetAsync<Suscripcion?>("/api/suscripciones/activa");
        estado.Text = activa is null ? "SIN PLAN ACTIVO" : "SOCIO ACTIVO";
        estado.TextColor = activa is null ? Colores.Peligro : Colores.Exito;
        plan.Text = activa is null ? "Acércate a recepción para activar tu plan." : $"{activa.Plan} · {activa.Restante}";
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DeviceDisplay.Current.KeepScreenOn = false;
    }
}
