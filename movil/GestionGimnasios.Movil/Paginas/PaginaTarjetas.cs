namespace GestionGimnasios.Movil.Paginas;

/// <summary>
/// Pantalla de lectura que se arma con tarjetas en <see cref="Cuerpo"/> cada vez que aparece
/// (deslizar hacia abajo recarga). Las subclases solo implementan <see cref="Armar"/>.
/// </summary>
public abstract class PaginaTarjetas : PaginaBase
{
    protected readonly VerticalStackLayout Cuerpo = new() { Padding = 16, Spacing = 0 };
    private readonly RefreshView refresco;

    protected PaginaTarjetas(string titulo)
    {
        Title = titulo;
        refresco = new RefreshView { Content = new ScrollView { Content = Cuerpo } };
        refresco.Refreshing += async (_, _) =>
        {
            await IntentarAsync(CargarAsync);
            refresco.IsRefreshing = false;
        };
        Content = refresco;
    }

    /// <summary>Consulta la API y devuelve las vistas a mostrar, en orden.</summary>
    protected abstract Task<IEnumerable<View>> Armar();

    protected override async Task CargarAsync()
    {
        var vistas = (await Armar()).ToList();
        Cuerpo.Clear();
        foreach (var v in vistas) Cuerpo.Add(v);
    }

    protected Task RecargarAsync() => IntentarAsync(CargarAsync);
}
