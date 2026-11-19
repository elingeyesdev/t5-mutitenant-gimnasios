using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Selector reutilizable de ejercicios con búsqueda local y selección por ID.</summary>
public class SelectorEjercicioPage : PaginaBase
{
    private readonly IReadOnlyList<Ejercicio> catalogo;
    private readonly CollectionView lista;
    private readonly SearchBar buscar;
    private readonly TaskCompletionSource<Ejercicio?> resultado =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool finalizado;

    private SelectorEjercicioPage(IEnumerable<Ejercicio> ejercicios)
    {
        catalogo = TextoEjercicio.Ordenar(ejercicios, null);

        Title = "Seleccionar ejercicio";
        buscar = new SearchBar { Placeholder = "Buscar ejercicio...", Margin = new Thickness(16, 8, 16, 4) };
        buscar.TextChanged += (_, _) => Filtrar();

        lista = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 0 },
            EmptyView = Vacio("No se encontraron ejercicios"),
            ItemTemplate = new DataTemplate(() =>
            {
                var nombre = new Label { Style = Estilo("Nombre") };
                nombre.SetBinding(Label.TextProperty, nameof(Ejercicio.Nombre));
                var detalle = new Label { Style = Estilo("Detalle") };
                detalle.SetBinding(Label.TextProperty, nameof(Ejercicio.Detalle));
                return new Border
                {
                    Style = Estilo("Tarjeta"),
                    Margin = new Thickness(16, 0, 16, 8),
                    Content = new VerticalStackLayout { Spacing = 2, Children = { nombre, detalle } },
                };
            }),
        };
        lista.SelectionChanged += OnSeleccion;

        var cancelar = new Button { Text = "Cancelar", Style = Estilo("Secundario"), Margin = new Thickness(16, 4) };
        cancelar.Clicked += async (_, _) => await CancelarAsync();

        Content = new Grid
        {
            RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto)],
            Children = { buscar, lista, cancelar },
        };
        Grid.SetRow(lista, 1);
        Grid.SetRow(cancelar, 2);
        Filtrar();
    }

    public static async Task<Ejercicio?> ElegirAsync(INavigation navigation, IEnumerable<Ejercicio> ejercicios)
    {
        var page = new SelectorEjercicioPage(ejercicios);
        await navigation.PushAsync(page);
        return await page.resultado.Task;
    }

    private void Filtrar()
    {
        lista.ItemsSource = TextoEjercicio.Ordenar(catalogo, buscar.Text);
    }

    private async void OnSeleccion(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Ejercicio ejercicio) return;
        finalizado = true;
        resultado.TrySetResult(ejercicio);
        await Navigation.PopAsync();
    }

    private async Task CancelarAsync()
    {
        finalizado = true;
        resultado.TrySetResult(null);
        await Navigation.PopAsync();
    }

    protected override void OnDisappearing()
    {
        if (!finalizado) resultado.TrySetResult(null);
        base.OnDisappearing();
    }
}
