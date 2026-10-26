using System.Globalization;
using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Detalle del gimnasio y reserva de clases (CU-09, CU-10) con lista de espera (CU-12).</summary>
public partial class SucursalDetallePage : PaginaBase
{
    private readonly int sucursalId;
    private Sucursal? sucursal;

    public SucursalDetallePage(int sucursalId)
    {
        InitializeComponent();
        this.sucursalId = sucursalId;
        var hoy = Hora.Hoy.ToDateTime(TimeOnly.MinValue);
        Fecha.MinimumDate = hoy;
        Fecha.MaximumDate = hoy.AddDays(14);
        Fecha.Date = hoy;
    }

    private DateOnly FechaElegida => DateOnly.FromDateTime(Fecha.Date ?? DateTime.Today);

    protected override async Task CargarAsync()
    {
        sucursal = await Api.GetAsync<Sucursal>($"/api/sucursales/{sucursalId}");
        Title = sucursal.Nombre;
        Nombre.Text = sucursal.Nombre;
        Cadena.Text = sucursal.Cadena;
        Direccion.Text = sucursal.Direccion;
        Descripcion.Text = sucursal.Descripcion;
        Descripcion.IsVisible = !string.IsNullOrEmpty(sucursal.Descripcion);
        Estado.Text = sucursal.Estado;
        Estado.TextColor = sucursal.ColorEstado;
        Aforo.Text = $"{sucursal.OcupacionActual}/{sucursal.CapacidadMaxima}";
        Maquinas.Text = $"{sucursal.Maquinas.Disponibles}/{sucursal.Maquinas.Total}";
        BindableLayout.SetItemsSource(Horarios, sucursal.Horarios);
        Servicios.Text = sucursal.ServiciosTexto;
        await CargarClasesAsync();
    }

    private async Task CargarClasesAsync() =>
        BindableLayout.SetItemsSource(Clases,
            await Api.GetAsync<List<Disponibilidad>>($"/api/reservas/disponibilidad?sucursalId={sucursalId}&fecha={FechaElegida:yyyy-MM-dd}"));

    private async void OnFecha(object? sender, DateChangedEventArgs e) => await IntentarAsync(CargarClasesAsync);

    private async void OnReservar(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Disponibilidad d) return;
        if (!await ConfirmarAsync("Reservar", $"{d.Actividad}\n{Hora.Fecha(d.Fecha)} · {d.Horario}\n{d.Sucursal}", "Reservar")) return;
        Reserva? r = null;
        if (await IntentarAsync(async () => r = await Api.PostAsync<Reserva>("/api/reservas", new { horarioActividadId = d.HorarioId, fecha = d.Fecha })))
        {
            await AvisarAsync("Reserva confirmada", $"Tu código de ingreso es {r!.Codigo}. Muéstralo en recepción al llegar.");
            await IntentarAsync(CargarClasesAsync);
        }
    }

    private async void OnEsperar(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Disponibilidad d) return;
        EntradaEspera? r = null;
        if (await IntentarAsync(async () => r = await Api.PostAsync<EntradaEspera>("/api/lista-espera", new { horarioActividadId = d.HorarioId, fecha = d.Fecha })))
        {
            await AvisarAsync("Lista de espera", $"Quedaste en la posición {r!.Posicion}. Te avisaremos si se libera un cupo.");
            await IntentarAsync(CargarClasesAsync);
        }
    }

    /// <summary>Abre la ubicación en OpenStreetMap (sin SDK de mapas mientras no lo apruebe el docente).</summary>
    private async void OnMapa(object? sender, EventArgs e)
    {
        if (sucursal?.Ubicacion is not { } u)
        {
            await AvisarAsync("Mapa", "Este gimnasio no tiene ubicación registrada.");
            return;
        }
        var url = string.Create(CultureInfo.InvariantCulture, $"https://www.openstreetmap.org/?mlat={u.Latitud}&mlon={u.Longitud}#map=17/{u.Latitud}/{u.Longitud}");
        await Launcher.Default.OpenAsync(url);
    }
}
