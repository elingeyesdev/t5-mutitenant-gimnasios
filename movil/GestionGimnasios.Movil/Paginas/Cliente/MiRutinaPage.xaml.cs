using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Rutina asignada por el entrenador (CU-16), catálogo de ejercicios y acceso a registrar entrenamientos (CU-17).</summary>
public partial class MiRutinaPage : PaginaBase
{
    private Rutina? rutina;
    private List<Ejercicio> catalogo = [];
    private readonly ColeccionEjercicios ejerciciosVisibles = [];
    private CancellationTokenSource? filtroCancellation;

    public MiRutinaPage()
    {
        InitializeComponent();
        EjerciciosCatalogo.ItemsSource = ejerciciosVisibles;
    }

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

        var ejerciciosRutina = rutina?.Ejercicios.OrderBy(e => e.Orden).ToList() ?? [];
        VacioEjerciciosRutina.Text = rutina is null
            ? "No tienes una rutina asignada."
            : "Esta rutina todavía no tiene ejercicios configurados.";
        VacioEjerciciosRutina.IsVisible = ejerciciosRutina.Count == 0;
        BindableLayout.SetItemsSource(EjerciciosRutina, ejerciciosRutina);
        FiltrarEjercicios(BuscarEjercicios.Text);
        BindableLayout.SetItemsSource(Sesiones, await sesionesTask);
    }

    private void FiltrarEjercicios(string? texto, bool conservarFoco = false)
    {
        var consulta = TextoEjercicio.Normalizar(texto);
        var ejercicios = TextoEjercicio.Ordenar(catalogo, texto);
        VacioEjercicios.Text = consulta.Length == 0
            ? "No hay ejercicios disponibles."
            : "No se encontraron ejercicios";
        VacioEjercicios.IsVisible = ejercicios.Count == 0;
        ejerciciosVisibles.Reemplazar(ejercicios);
        if (conservarFoco)
            Dispatcher.Dispatch(() => BuscarEjercicios.Focus());
    }

    private async void OnBuscarEjercicios(object? sender, TextChangedEventArgs e)
    {
        filtroCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        filtroCancellation = cancellation;

        try
        {
            await Task.Delay(180, cancellation.Token);
            if (!cancellation.IsCancellationRequested)
                FiltrarEjercicios(e.NewTextValue, conservarFoco: true);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(filtroCancellation, cancellation))
                filtroCancellation = null;
            cancellation.Dispose();
        }
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
        switch ((sender as BindableObject)?.BindingContext)
        {
            case RutinaEjercicio ejercicioRutina:
                await IrAsync(new SimulacionEjercicioPage(ejercicioRutina.EjercicioId));
                break;
            case Ejercicio ejercicio:
                await IrAsync(new SimulacionEjercicioPage(ejercicio.Id));
                break;
        }
    }

    private sealed class ColeccionEjercicios : ObservableCollection<Ejercicio>
    {
        public void Reemplazar(IEnumerable<Ejercicio> ejercicios)
        {
            Items.Clear();
            foreach (var ejercicio in ejercicios)
                Items.Add(ejercicio);

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
