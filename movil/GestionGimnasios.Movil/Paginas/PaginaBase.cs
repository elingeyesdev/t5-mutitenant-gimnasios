using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas;

/// <summary>
/// Base de todas las pantallas: recarga los datos al aparecer y muestra los errores de la API
/// con su mensaje en lugar de cerrar la app.
/// </summary>
public abstract class PaginaBase : ContentPage
{
    protected static Api Api => Sesion.Api;

    /// <summary>Indica que hay una llamada a la API en curso (reemplaza a Page.Ocupado, obsoleto en .NET 10).</summary>
    public static readonly BindableProperty OcupadoProperty = BindableProperty.Create(nameof(Ocupado), typeof(bool), typeof(PaginaBase), false);

    public bool Ocupado
    {
        get => (bool)GetValue(OcupadoProperty);
        set => SetValue(OcupadoProperty, value);
    }

    /// <summary>Se llama cada vez que la pantalla aparece. Las pantallas sin datos remotos no lo sobrescriben.</summary>
    protected virtual Task CargarAsync() => Task.CompletedTask;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await IntentarAsync(CargarAsync);
    }

    /// <summary>Ejecuta una acción mostrando el indicador de carga y avisando los errores.</summary>
    protected async Task<bool> IntentarAsync(Func<Task> accion)
    {
        if (Ocupado) return false;
        Ocupado = true;
        try
        {
            await accion();
            return true;
        }
        catch (ErrorApi e)
        {
            if (e.Estado != System.Net.HttpStatusCode.Unauthorized) await AvisarAsync("No se pudo completar", e.Message);
            return false;
        }
        finally
        {
            Ocupado = false;
        }
    }

    protected Task AvisarAsync(string titulo, string mensaje) => DisplayAlertAsync(titulo, mensaje, "Aceptar");

    protected Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar = "Sí") =>
        DisplayAlertAsync(titulo, mensaje, aceptar, "Cancelar");

    protected Task IrAsync(Page pagina) => Navigation.PushAsync(pagina);

    protected static int? Entero(string? texto) => int.TryParse(texto, out var n) ? n : null;

    protected static decimal? LeerDecimal(string? texto) =>
        decimal.TryParse(texto?.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : null;

    protected static string? Texto(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    /// <summary>Estilo de Resources/Styles (incluye los diccionarios combinados) para pantallas armadas en C#.</summary>
    protected static Style Estilo(string clave) =>
        Application.Current!.Resources.TryGetValue(clave, out var s) ? (Style)s : throw new KeyNotFoundException(clave);

    // ── Piezas para pantallas de solo lectura armadas en C# ──

    protected static Label Seccion(string texto) => new() { Text = texto, Style = Estilo("Seccion") };

    protected static Label Vacio(string texto) => new() { Text = texto, Style = Estilo("Vacio") };

    /// <summary>Tarjeta con un título y líneas de detalle (las nulas o vacías se omiten).</summary>
    protected static Border Tarjeta(string titulo, params string?[] lineas)
    {
        var contenido = new VerticalStackLayout { Spacing = 2, Children = { new Label { Text = titulo, Style = Estilo("Nombre") } } };
        foreach (var l in lineas.Where(l => !string.IsNullOrWhiteSpace(l)))
            contenido.Add(new Label { Text = l, Style = Estilo("Detalle") });
        return new Border { Style = Estilo("Tarjeta"), Content = contenido };
    }

    /// <summary>Agrega a la tarjeta un botón con su acción (estilo null = botón principal).</summary>
    protected static Border ConBoton(Border tarjeta, string texto, Func<Task> accion, string? estilo = "Chico")
    {
        var b = new Button { Text = texto, HorizontalOptions = LayoutOptions.Start, Margin = new Thickness(0, 6, 0, 0) };
        if (estilo is not null) b.Style = Estilo(estilo);
        b.Clicked += async (_, _) => await accion();
        ((Layout)tarjeta.Content!).Add(b);
        return tarjeta;
    }

    protected static string Numero(decimal? n) => n?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "";
}
