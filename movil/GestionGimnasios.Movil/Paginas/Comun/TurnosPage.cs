using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Comun;

/// <summary>Turnos de trabajo del personal (los asigna el gerente desde el panel web).</summary>
public class TurnosPage : PaginaBase
{
    private readonly CollectionView lista = new()
    {
        Margin = new Thickness(16, 8),
        ItemTemplate = new DataTemplate(() =>
        {
            var texto = new Label { FontSize = 15 };
            texto.SetBinding(Label.TextProperty, static (Turno t) => t.Texto);
            return new Border { Style = Estilo("Tarjeta"), Content = texto };
        }),
    };

    public TurnosPage()
    {
        Title = "Mis turnos";
        lista.EmptyView = new Label { Text = "No tienes turnos asignados.", Style = Estilo("Vacio") };
        Content = lista;
    }

    protected override async Task CargarAsync() =>
        lista.ItemsSource = await Api.GetAsync<List<Turno>>("/api/personal/mis-turnos");
}
