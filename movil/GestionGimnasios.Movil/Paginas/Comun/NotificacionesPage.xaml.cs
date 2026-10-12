using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Comun;

/// <summary>Notificaciones dentro de la app (reemplazan las push hasta que el docente apruebe Firebase).</summary>
public partial class NotificacionesPage : PaginaBase
{
    private bool cargandoPreferencias;
    private Preferencias? preferencias;

    public NotificacionesPage() => InitializeComponent();

    protected override async Task CargarAsync()
    {
        Lista.ItemsSource = await Api.GetAsync<List<Notificacion>>("/api/notificaciones?limite=100");
        if (preferencias is null)
        {
            preferencias = await Api.GetAsync<Preferencias>("/api/notificaciones/preferencias");
            cargandoPreferencias = true;
            Confirmaciones.IsToggled = preferencias.ConfirmacionesReserva;
            Recordatorios.IsToggled = preferencias.RecordatoriosClase;
            Cancelaciones.IsToggled = preferencias.AlertasCancelacion;
            Promociones.IsToggled = preferencias.Promociones;
            cargandoPreferencias = false;
        }
    }

    private async void OnRefrescar(object? sender, EventArgs e)
    {
        await IntentarAsync(CargarAsync);
        Refresco.IsRefreshing = false;
    }

    private async void OnSeleccion(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Notificacion n) return;
        Lista.SelectedItem = null;
        if (!n.Leida && await IntentarAsync(() => Api.PutAsync($"/api/notificaciones/{n.Id}/leida")))
            await IntentarAsync(CargarAsync);
    }

    private async void OnTodasLeidas(object? sender, EventArgs e)
    {
        if (await IntentarAsync(() => Api.PutAsync("/api/notificaciones/leidas")))
            await IntentarAsync(CargarAsync);
    }

    private async void OnPreferencia(object? sender, ToggledEventArgs e)
    {
        if (cargandoPreferencias || preferencias is null) return;
        preferencias = preferencias with
        {
            ConfirmacionesReserva = Confirmaciones.IsToggled,
            RecordatoriosClase = Recordatorios.IsToggled,
            AlertasCancelacion = Cancelaciones.IsToggled,
            Promociones = Promociones.IsToggled,
        };
        var p = preferencias;
        await IntentarAsync(() => Api.PutAsync<Preferencias>("/api/notificaciones/preferencias", p));
    }
}
