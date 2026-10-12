using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Rutina asignada por el entrenador (CU-16) y acceso a registrar entrenamientos (CU-17).</summary>
public partial class MiRutinaPage : PaginaBase
{
    private Rutina? rutina;

    public MiRutinaPage() => InitializeComponent();

    protected override async Task CargarAsync()
    {
        var rutinas = await Api.GetAsync<List<Rutina>>("/api/rutinas/mias");
        rutina = rutinas.FirstOrDefault(r => r.Activa) ?? rutinas.FirstOrDefault();
        SinRutina.IsVisible = rutina is null;
        TarjetaRutina.IsVisible = rutina is not null;
        BotonRutina.IsEnabled = rutina is not null;
        TituloEjercicios.IsVisible = rutina is not null;
        if (rutina is not null)
        {
            Nombre.Text = rutina.Nombre;
            Detalle.Text = rutina.Detalle;
            Entrenador.Text = rutina.EntrenadorTexto;
            Descripcion.Text = rutina.Descripcion;
            BindableLayout.SetItemsSource(Ejercicios, rutina.Ejercicios.OrderBy(e => e.Orden).ToList());
        }
        else BindableLayout.SetItemsSource(Ejercicios, null);
        BindableLayout.SetItemsSource(Sesiones, await Api.GetAsync<List<SesionEntrenamiento>>("/api/entrenamiento/sesiones?limite=5"));
    }

    private async void OnRefrescar(object? sender, EventArgs e)
    {
        await IntentarAsync(CargarAsync);
        Refresco.IsRefreshing = false;
    }

    private async void OnEntrenarRutina(object? sender, EventArgs e)
    {
        if (rutina is not null) await IrAsync(new EntrenarPage(rutina));
    }

    private async void OnLibre(object? sender, EventArgs e) => await IrAsync(new EntrenarPage(null));

    private async void OnVideo(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is RutinaEjercicio r && r.TieneVideo)
            await Launcher.Default.OpenAsync($"https://www.youtube.com/watch?v={r.YoutubeVideoId}");
    }
}
