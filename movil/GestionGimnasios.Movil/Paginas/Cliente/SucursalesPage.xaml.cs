using System.Globalization;
using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Catálogo de gimnasios de todas las marcas (CU-08), con filtro por cercanía opcional.</summary>
public partial class SucursalesPage : PaginaBase
{
    private List<Sucursal> todas = [];
    private Location? ubicacion;

    public SucursalesPage() => InitializeComponent();

    protected override async Task CargarAsync()
    {
        var ruta = "/api/sucursales";
        if (ubicacion is not null)
            ruta += string.Create(CultureInfo.InvariantCulture, $"?lat={ubicacion.Latitude}&lng={ubicacion.Longitude}&radioKm=30");
        todas = await Api.GetAsync<List<Sucursal>>(ruta);
        Filtrar();
    }

    private void Filtrar()
    {
        var t = Buscar.Text?.Trim();
        Lista.ItemsSource = string.IsNullOrEmpty(t)
            ? todas
            : todas.Where(s => $"{s.Nombre} {s.Cadena} {s.Direccion}".Contains(t, StringComparison.CurrentCultureIgnoreCase)).ToList();
    }

    private void OnBuscar(object? sender, TextChangedEventArgs e) => Filtrar();

    private async void OnRefrescar(object? sender, EventArgs e)
    {
        await IntentarAsync(CargarAsync);
        Refresco.IsRefreshing = false;
    }

    private async void OnCerca(object? sender, EventArgs e)
    {
        if (ubicacion is not null)
        {
            ubicacion = null;
            BotonCerca.Text = "Cerca de mí";
            await IntentarAsync(CargarAsync);
            return;
        }
        try
        {
            if (await Permissions.RequestAsync<Permissions.LocationWhenInUse>() != PermissionStatus.Granted)
            {
                await AvisarAsync("Ubicación", "Sin permiso de ubicación se muestran todos los gimnasios.");
                return;
            }
            ubicacion = await Geolocation.Default.GetLastKnownLocationAsync()
                        ?? await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or FeatureNotEnabledException or PermissionException)
        {
            await AvisarAsync("Ubicación", "Activa la ubicación del celular para buscar gimnasios cercanos.");
            return;
        }
        if (ubicacion is null) return;
        BotonCerca.Text = "Ver todos";
        await IntentarAsync(CargarAsync);
    }

    private async void OnSeleccion(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Sucursal s) return;
        Lista.SelectedItem = null;
        await IrAsync(new SucursalDetallePage(s.Id));
    }
}
