using System.Text.Json;
using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Cliente;

public partial class InicioClientePage : PaginaBase
{
    private List<Disponibilidad> clasesDeHoy = [];

    public InicioClientePage() => InitializeComponent();

    protected override async Task CargarAsync()
    {
        var u = Sesion.Usuario!;
        Saludo.Text = $"Hola, {u.Nombre}";
        Gimnasio.Text = u.Sucursal is null ? "Aún no tienes un gimnasio asignado" : $"{u.Sucursal} · {u.Cadena}";

        var activa = await Api.GetAsync<Suscripcion?>("/api/suscripciones/activa");
        Plan.Text = activa?.Plan ?? "Sin plan activo";
        Vigencia.Text = activa?.Vigencia ?? "Activa tu plan en recepción para reservar clases.";
        Restante.Text = activa?.Restante ?? "";

        var hoy = Hora.Hoy;
        clasesDeHoy = u.SucursalId is int sucursalId
            ? await Api.GetAsync<List<Disponibilidad>>($"/api/reservas/disponibilidad?sucursalId={sucursalId}&fecha={hoy:yyyy-MM-dd}")
            : [];
        BindableLayout.SetEmptyView(ClasesHoy, u.SucursalId is null
            ? "No tienes un gimnasio asignado para consultar las clases de hoy."
            : "No hay clases disponibles hoy en tu gimnasio.");
        BindableLayout.SetItemsSource(ClasesHoy, clasesDeHoy);

        var reservas = await Api.GetAsync<List<Reserva>>("/api/reservas/mias");
        BindableLayout.SetItemsSource(Proximas, reservas
            .Where(r => !r.EsPasada && r.Estado is "CONFIRMADA" or "PENDIENTE" && r.Fecha >= hoy)
            .OrderBy(r => r.Fecha).ThenBy(r => r.Inicio).Take(3).ToList());

        var espera = await Api.GetAsync<List<EntradaEspera>>("/api/lista-espera/mias");
        var notificada = espera.FirstOrDefault(e => e.Estado == "NOTIFICADA");
        AvisoEspera.IsVisible = notificada is not null;
        if (notificada is not null) TextoEspera.Text = $"¡Se liberó un cupo en {notificada.Actividad} ({notificada.Cuando})! Resérvalo antes que otro socio.";

        var sesiones = await Api.GetAsync<List<SesionEntrenamiento>>("/api/entrenamiento/sesiones?limite=100");
        Sesiones.Text = sesiones.Count(s => Hora.Local(s.Inicio).Month == DateTime.Now.Month && Hora.Local(s.Inicio).Year == DateTime.Now.Year).ToString();

        var sinLeer = await Api.GetAsync<JsonElement>("/api/notificaciones/sin-leer");
        Notificaciones.Text = sinLeer.GetProperty("cantidad").GetInt32().ToString();

        BindableLayout.SetItemsSource(Frecuentes, await Api.GetAsync<List<GimnasioFrecuente>>("/api/sucursales/frecuentes"));
    }

    private async void OnRefrescar(object? sender, EventArgs e)
    {
        await IntentarAsync(CargarAsync);
        Refresco.IsRefreshing = false;
    }

    private async void OnCarnet(object? sender, EventArgs e) => await IrAsync(new CarnetPage());

    private async void OnReservar(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//SucursalesPage");
}
