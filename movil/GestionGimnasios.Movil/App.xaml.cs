using GestionGimnasios.Movil.Paginas.Comun;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        // La app usa la paleta clara de la marca, igual que el panel web.
        UserAppTheme = AppTheme.Light;
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
