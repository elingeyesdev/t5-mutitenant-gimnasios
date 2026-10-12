using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Comun;

public partial class ConversacionesPage : PaginaBase
{
    public ConversacionesPage() => InitializeComponent();

    protected override async Task CargarAsync() =>
        Lista.ItemsSource = await Api.GetAsync<List<Conversacion>>("/api/mensajes/conversaciones");

    private async void OnRefrescar(object? sender, EventArgs e)
    {
        await IntentarAsync(CargarAsync);
        Refresco.IsRefreshing = false;
    }

    private async void OnSeleccion(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Conversacion c) return;
        Lista.SelectedItem = null;
        await IrAsync(new ChatPage(c.Id, c.OtroUsuario));
    }
}
