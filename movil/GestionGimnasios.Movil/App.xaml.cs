using GestionGimnasios.Movil.Paginas.Comun;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        Sesion.Api.SesionVencida += () => MainThread.BeginInvokeOnMainThread(CerrarSesion);
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new InicioPage()) { Title = "Gestión Gimnasios" };

    private static Window? Ventana => Current?.Windows.FirstOrDefault();

    /// <summary>Tras iniciar sesión se arma la navegación según el rol del usuario.</summary>
    public static void EntrarAlMenu()
    {
        if (Ventana is { } v) v.Page = new AppShell();
    }

    public static void CerrarSesion()
    {
        Sesion.Cerrar();
        if (Ventana is { } v) v.Page = new NavigationPage(new LoginPage());
    }
}
