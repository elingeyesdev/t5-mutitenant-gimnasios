using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Comun;

/// <summary>Pantalla de arranque: recupera la sesión guardada y decide si mostrar el login o el menú.</summary>
public class InicioPage : ContentPage
{
    public InicioPage()
    {
        BackgroundColor = Color.FromArgb("#1F5FA8");
        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 16,
            Children =
            {
                new Label { Text = "Gestión Gimnasios", FontSize = 26, FontFamily = "OpenSansSemibold", TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center },
                new ActivityIndicator { IsRunning = true, Color = Colors.White },
            },
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (await Sesion.RestaurarAsync()) App.EntrarAlMenu();
        else if (Window is not null) Window.Page = new NavigationPage(new LoginPage());
    }
}
