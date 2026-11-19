using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Rutina asignada por el entrenador (CU-16), catálogo de ejercicios y acceso a registrar entrenamientos (CU-17).</summary>
public partial class MiRutinaPage : PaginaBase
{
    private Rutina? rutina;
    private List<Ejercicio> catalogo = [];

    public MiRutinaPage() => InitializeComponent();

    protected override async Task CargarAsync()
    {
        var rutinasTask = Api.GetAsync<List<Rutina>>("/api/rutinas/mias");
        var ejerciciosTask = Api.GetAsync<List<Ejercicio>>("/api/ejercicios");
        var sesionesTask = Api.GetAsync<List<SesionEntrenamiento>>("/api/entrenamiento/sesiones?limite=5");
        await Task.WhenAll(rutinasTask, ejerciciosTask, sesionesTask);

        var rutinas = await rutinasTask;
        rutina = rutinas.FirstOrDefault(r => r.Activa) ?? rutinas.FirstOrDefault();
        catalogo = TextoEjercicio.Ordenar(await ejerciciosTask, null);

        SinRutina.IsVisible = rutina is null;
        TarjetaRutina.IsVisible = rutina is not null;
        BotonRutina.IsEnabled = rutina is not null;
        if (rutina is not null)
        {
            Nombre.Text = rutina.Nombre;
            Detalle.Text = rutina.Detalle;
            Entrenador.Text = rutina.EntrenadorTexto;
            Descripcion.Text = rutina.Descripcion;
        }

        FiltrarEjercicios();
        BindableLayout.SetItemsSource(Sesiones, await sesionesTask);
    }

    private void FiltrarEjercicios()
    {
        var consulta = TextoEjercicio.Normalizar(BuscarEjercicios.Text);
        var ejercicios = TextoEjercicio.Ordenar(catalogo, BuscarEjercicios.Text);
        VacioEjercicios.Text = consulta.Length == 0
            ? "No hay ejercicios disponibles."
            : "No se encontraron ejercicios";
        VacioEjercicios.IsVisible = ejercicios.Count == 0;
        BindableLayout.SetItemsSource(Ejercicios, ejercicios);
    }

    private void OnBuscarEjercicios(object? sender, TextChangedEventArgs e) => FiltrarEjercicios();

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
        if ((sender as BindableObject)?.BindingContext is Ejercicio ejercicio)
            await IrAsync(new SimulacionEjercicioPage(ejercicio.Id));
    }
}
