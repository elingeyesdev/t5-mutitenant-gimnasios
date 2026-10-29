using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Gestión de autorizaciones para compartir el perfil deportivo con otras cadenas de gimnasios (RF-02, CU-36).</summary>
public class ConsentimientoPage : PaginaBase
{
    private static readonly (string Codigo, string Texto)[] DatosDisponibles =
    [
        ("PERFIL", "Perfil de entrenamiento"),
        ("RESTRICCIONES", "Restricciones"),
        ("HISTORIAL", "Historial de sesiones"),
        ("METRICAS", "Métricas físicas"),
    ];

    private readonly Picker cadena = new() { Title = "Selecciona una cadena" };
    private readonly CheckBox perfil = new();
    private readonly CheckBox restricciones = new();
    private readonly CheckBox historial = new();
    private readonly CheckBox metricas = new();
    private readonly VerticalStackLayout opcionesDatos = new() { Spacing = 8 };
    private readonly VerticalStackLayout formulario = new() { Spacing = 8 };
    private readonly VerticalStackLayout consentimientos = new() { Spacing = 0 };
    private readonly Label estadoCadenas = new() { Style = Estilo("Vacio"), IsVisible = false };
    private readonly Button autorizar = new() { Text = "Registrar autorización" };
    private readonly RefreshView refresco;

    public ConsentimientoPage()
    {
        Title = "Autorizaciones de perfil";
        cadena.ItemDisplayBinding = new Binding(nameof(CadenaDisponible.Nombre));

        formulario.Children.Add(Seccion("Nueva autorización"));
        formulario.Children.Add(new Label
        {
            Text = "Elige una cadena y los datos de tu perfil deportivo que podrá consultar.",
            Style = Estilo("Detalle"),
        });
        formulario.Children.Add(estadoCadenas);
        formulario.Children.Add(cadena);
        opcionesDatos.Children.Add(Opcion(perfil, "Perfil de entrenamiento"));
        opcionesDatos.Children.Add(Opcion(restricciones, "Restricciones"));
        opcionesDatos.Children.Add(Opcion(historial, "Historial de sesiones"));
        opcionesDatos.Children.Add(Opcion(metricas, "Métricas físicas"));
        formulario.Children.Add(opcionesDatos);
        autorizar.Clicked += OnAutorizar;
        formulario.Children.Add(autorizar);

        var cuerpo = new VerticalStackLayout { Padding = 16, Spacing = 0 };
        cuerpo.Children.Add(formulario);
        cuerpo.Children.Add(Seccion("Mis autorizaciones"));
        cuerpo.Children.Add(consentimientos);

        refresco = new RefreshView { Content = new ScrollView { Content = cuerpo } };
        refresco.Refreshing += async (_, _) =>
        {
            await IntentarAsync(CargarAsync);
            refresco.IsRefreshing = false;
        };
        Content = refresco;
    }

    protected override async Task CargarAsync()
    {
        var cadenas = await Api.GetAsync<List<CadenaDisponible>>("/api/portabilidad/cadenas");
        cadena.ItemsSource = cadenas;
        cadena.SelectedItem = cadenas.FirstOrDefault();
        var hayCadenas = cadenas.Count > 0;
        estadoCadenas.Text = hayCadenas ? "" : "No hay cadenas disponibles para autorizar.";
        estadoCadenas.IsVisible = !hayCadenas;
        cadena.IsEnabled = hayCadenas;
        opcionesDatos.IsVisible = hayCadenas;
        autorizar.IsEnabled = hayCadenas;
        if (!hayCadenas)
        {
            perfil.IsChecked = false;
            restricciones.IsChecked = false;
            historial.IsChecked = false;
            metricas.IsChecked = false;
        }

        var lista = await Api.GetAsync<List<Consentimiento>>("/api/portabilidad/consentimientos");
        consentimientos.Clear();
        if (lista.Count == 0)
        {
            consentimientos.Add(Vacio("Todavía no tienes autorizaciones registradas."));
            return;
        }

        foreach (var consentimiento in lista)
            consentimientos.Add(ArmarConsentimiento(consentimiento));
    }

    private static HorizontalStackLayout Opcion(CheckBox check, string texto) =>
        new()
        {
            Spacing = 8,
            Children =
            {
                check,
                new Label { Text = texto, VerticalOptions = LayoutOptions.Center },
            },
        };

    private Border ArmarConsentimiento(Consentimiento consentimiento)
    {
        var detalle = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = consentimiento.Cadena, Style = Estilo("Nombre") },
                new Label { Text = $"Estado: {consentimiento.Estado}", TextColor = consentimiento.Vigente ? Colores.Exito : Colores.Peligro, Style = Estilo("Insignia") },
                new Label { Text = $"Autorizado: {consentimiento.FechaOtorgado}", Style = Estilo("Detalle") },
                new Label { Text = consentimiento.Vigente ? "" : $"Revocado: {consentimiento.FechaRevocado}", Style = Estilo("Detalle") },
                new Label { Text = $"Datos: {DatosTexto(consentimiento.Datos)}", Style = Estilo("Detalle") },
                new Label { Text = consentimiento.Consultas == 0 ? "Sin consultas registradas" : $"Consultas registradas: {consentimiento.Consultas}", Style = Estilo("Detalle") },
            },
        };

        var consultas = new VerticalStackLayout { Spacing = 2, Margin = new Thickness(0, 8, 0, 0) };
        var verConsultas = new Button { Text = "Ver consultas", Style = Estilo("Chico") };
        verConsultas.Clicked += async (_, _) => await VerConsultasAsync(consentimiento.Id, consultas, verConsultas);

        var acciones = new HorizontalStackLayout { Spacing = 8, Children = { verConsultas } };
        if (consentimiento.Vigente)
        {
            var revocar = new Button { Text = "Revocar", Style = Estilo("Peligroso") };
            revocar.Clicked += async (_, _) => await RevocarAsync(consentimiento);
            acciones.Add(revocar);
        }

        detalle.Add(acciones);
        detalle.Add(consultas);
        return new Border { Style = Estilo("Tarjeta"), Content = detalle };
    }

    private async void OnAutorizar(object? sender, EventArgs e)
    {
        if (cadena.SelectedItem is not CadenaDisponible seleccionada)
        {
            await AvisarAsync("Autorización", "Selecciona una cadena.");
            return;
        }

        var datos = DatosSeleccionados();
        if (datos.Count == 0)
        {
            await AvisarAsync("Autorización", "Selecciona al menos un dato para compartir.");
            return;
        }

        var formulario = new { cadenaId = seleccionada.Id, datos };
        if (await IntentarAsync(() => Api.PostAsync<Consentimiento>("/api/portabilidad/consentimientos", formulario)))
        {
            perfil.IsChecked = false;
            restricciones.IsChecked = false;
            historial.IsChecked = false;
            metricas.IsChecked = false;
            await AvisarAsync("Autorización registrada", $"{seleccionada.Nombre} podrá consultar los datos que elegiste.");
            await IntentarAsync(CargarAsync);
        }
    }

    private async Task RevocarAsync(Consentimiento consentimiento)
    {
        if (!await ConfirmarAsync("Revocar autorización", $"La cadena {consentimiento.Cadena} dejará de consultar tu perfil deportivo. ¿Continuar?", "Revocar")) return;

        if (await IntentarAsync(() => Api.PutAsync<Consentimiento>($"/api/portabilidad/consentimientos/{consentimiento.Id}/revocar")))
        {
            await AvisarAsync("Autorización revocada", $"La autorización para {consentimiento.Cadena} fue revocada.");
            await IntentarAsync(CargarAsync);
        }
    }

    private async Task VerConsultasAsync(int id, VerticalStackLayout destino, Button boton)
    {
        if (destino.Children.Count > 0)
        {
            destino.Clear();
            boton.Text = "Ver consultas";
            return;
        }

        List<ConsultaPerfil>? lista = null;
        if (!await IntentarAsync(async () => lista = await Api.GetAsync<List<ConsultaPerfil>>($"/api/portabilidad/consentimientos/{id}/consultas"))) return;

        if (lista!.Count == 0)
            destino.Add(new Label { Text = "Esta autorización todavía no registra consultas.", Style = Estilo("Detalle") });
        else
            foreach (var consulta in lista)
                destino.Add(new Label { Text = $"{consulta.ConsultadoPor} · {consulta.Fecha}", Style = Estilo("Detalle") });

        boton.Text = "Ocultar consultas";
    }

    private List<string> DatosSeleccionados() =>
    [
        .. (perfil.IsChecked ? new[] { DatosDisponibles[0].Codigo } : []),
        .. (restricciones.IsChecked ? new[] { DatosDisponibles[1].Codigo } : []),
        .. (historial.IsChecked ? new[] { DatosDisponibles[2].Codigo } : []),
        .. (metricas.IsChecked ? new[] { DatosDisponibles[3].Codigo } : []),
    ];

    private static string DatosTexto(IEnumerable<string> datos) => string.Join(" · ", datos.Select(d => DatosDisponibles.FirstOrDefault(x => x.Codigo == d).Texto ?? d));
}
