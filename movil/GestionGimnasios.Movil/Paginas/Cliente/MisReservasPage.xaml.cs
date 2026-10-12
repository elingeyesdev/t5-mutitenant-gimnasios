using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Reservas del socio con su código de ingreso (CU-11) y su lista de espera.</summary>
public partial class MisReservasPage : PaginaBase
{
    public MisReservasPage() => InitializeComponent();

    protected override async Task CargarAsync()
    {
        var reservas = await Api.GetAsync<List<Reserva>>("/api/reservas/mias");
        var proximas = reservas.Where(r => !r.EsPasada && r.Estado is "CONFIRMADA" or "PENDIENTE").OrderBy(r => r.Fecha).ThenBy(r => r.Inicio).ToList();
        BindableLayout.SetItemsSource(Proximas, proximas);
        BindableLayout.SetItemsSource(Historial, reservas.Except(proximas).OrderByDescending(r => r.Fecha).ThenByDescending(r => r.Inicio).Take(30).ToList());
        var espera = await Api.GetAsync<List<EntradaEspera>>("/api/lista-espera/mias");
        BindableLayout.SetItemsSource(Espera, espera.Where(e => e.Estado is "EN_ESPERA" or "NOTIFICADA").ToList());
    }

    private async void OnRefrescar(object? sender, EventArgs e)
    {
        await IntentarAsync(CargarAsync);
        Refresco.IsRefreshing = false;
    }

    private async void OnCancelar(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Reserva r) return;
        if (!await ConfirmarAsync("Cancelar reserva", $"¿Cancelar {r.Actividad} del {Hora.Fecha(r.Fecha)}? El cupo pasará al primero de la lista de espera.", "Cancelar reserva"))
            return;
        if (await IntentarAsync(() => Api.PutAsync<Reserva>($"/api/reservas/{r.Id}/cancelar")))
            await IntentarAsync(CargarAsync);
    }

    private async void OnSalirEspera(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not EntradaEspera en) return;
        if (!await ConfirmarAsync("Lista de espera", $"¿Salir de la lista de {en.Actividad}?", "Salir")) return;
        if (await IntentarAsync(() => Api.DeleteAsync($"/api/lista-espera/{en.Id}")))
            await IntentarAsync(CargarAsync);
    }
}
