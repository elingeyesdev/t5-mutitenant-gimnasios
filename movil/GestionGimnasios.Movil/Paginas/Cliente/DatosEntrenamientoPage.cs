using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Objetivos de entrenamiento, restricciones físicas y contactos de emergencia (CU-20, CU-21).</summary>
public class DatosEntrenamientoPage : PaginaBase
{
    // Mismos códigos que el perfil deportivo de la app original (ObjetivoDeportivo).
    private static readonly string[] Objetivos = ["PERDIDA_PESO", "GANANCIA_MUSCULAR", "MANTENIMIENTO", "RESISTENCIA"];
    private static readonly string[] ObjetivosTexto = ["Pérdida de peso", "Ganancia muscular", "Mantenimiento", "Resistencia"];
    private static readonly string[] NivelesExp = ["PRINCIPIANTE", "INTERMEDIO", "AVANZADO"];
    private static readonly string[] NivelesTexto = ["Principiante", "Intermedio", "Avanzado"];

    private readonly Picker objetivo = new() { ItemsSource = ObjetivosTexto };
    private readonly Picker nivel = new() { ItemsSource = NivelesTexto };
    private readonly Entry dias = new() { Keyboard = Keyboard.Numeric, Placeholder = "Días por semana" };
    private readonly Entry tipos = new() { Placeholder = "Ej.: pesas, cardio, funcional" };
    private readonly Entry areas = new() { Placeholder = "Ej.: piernas, abdomen" };

    private readonly Switch restriccionActiva = new();
    private readonly Entry restriccionTipo = new() { Placeholder = "Ej.: lesión de rodilla" };
    private readonly Editor restriccionDescripcion = new() { AutoSize = EditorAutoSizeOption.TextChanges, Placeholder = "Descripción" };
    private readonly Entry zonas = new() { Placeholder = "Zonas afectadas, separadas por coma" };
    private readonly Entry evitar = new() { Placeholder = "Movimientos a evitar, separados por coma" };

    private readonly VerticalStackLayout contactos = new();
    private bool cargado;

    public DatosEntrenamientoPage()
    {
        Title = "Datos de entrenamiento";
        var guardarPerfil = new Button { Text = "Guardar objetivos" };
        guardarPerfil.Clicked += OnGuardarPerfil;
        var guardarRestriccion = new Button { Text = "Guardar restricción" };
        guardarRestriccion.Clicked += OnGuardarRestriccion;
        var agregarContacto = new Button { Text = "Agregar contacto", Style = Estilo("Secundario") };
        agregarContacto.Clicked += OnAgregarContacto;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 4,
                Children =
                {
                    Seccion("Objetivos"),
                    new Label { Text = "Objetivo principal" }, objetivo,
                    new Label { Text = "Nivel de experiencia" }, nivel,
                    new Label { Text = "Días disponibles por semana" }, dias,
                    new Label { Text = "Tipos de entrenamiento preferidos" }, tipos,
                    new Label { Text = "Áreas prioritarias" }, areas,
                    guardarPerfil,
                    Seccion("Restricción física"),
                    new Label { Text = "Tu entrenador la verá al preparar tu rutina.", Style = Estilo("Detalle") },
                    new HorizontalStackLayout { Spacing = 8, Children = { new Label { Text = "Tengo una restricción activa", VerticalOptions = LayoutOptions.Center }, restriccionActiva } },
                    restriccionTipo, restriccionDescripcion, zonas, evitar,
                    guardarRestriccion,
                    Seccion("Contactos de emergencia"),
                    contactos,
                    agregarContacto,
                },
            },
        };
    }

    protected override async Task CargarAsync()
    {
        if (!cargado)
        {
            var p = await Api.GetAsync<PerfilEntrenamiento?>("/api/entrenamiento/perfil");
            objetivo.SelectedIndex = Array.IndexOf(Objetivos, p?.ObjetivoPrincipal);
            nivel.SelectedIndex = Array.IndexOf(NivelesExp, p?.NivelExperiencia);
            dias.Text = p?.DiasDisponibles?.ToString();
            tipos.Text = p is null ? null : string.Join(", ", p.TiposPreferidos);
            areas.Text = p is null ? null : string.Join(", ", p.AreasPrioritarias);

            var r = await Api.GetAsync<Restriccion?>("/api/entrenamiento/restriccion");
            restriccionActiva.IsToggled = r?.Activa ?? false;
            restriccionTipo.Text = r?.Tipo;
            restriccionDescripcion.Text = r?.Descripcion;
            zonas.Text = r is null ? null : string.Join(", ", r.AreasAfectadas);
            evitar.Text = r is null ? null : string.Join(", ", r.MovimientosEvitar);
            cargado = true;
        }
        await CargarContactosAsync();
    }

    private async Task CargarContactosAsync()
    {
        var lista = await Api.GetAsync<List<Contacto>>("/api/entrenamiento/contactos");
        contactos.Clear();
        if (lista.Count == 0) contactos.Add(Vacio("Sin contactos registrados."));
        foreach (var c in lista)
        {
            var id = c.Id;
            contactos.Add(ConBoton(Tarjeta(c.NombreCompleto, c.Detalle), "Quitar", async () =>
            {
                if (await IntentarAsync(() => Api.DeleteAsync($"/api/entrenamiento/contactos/{id}")))
                    await IntentarAsync(CargarContactosAsync);
            }, "Peligroso"));
        }
    }

    private static string? Codigo(string[] codigos, Picker p) => p.SelectedIndex >= 0 ? codigos[p.SelectedIndex] : null;

    private static List<string> Lista(string? texto) =>
        (texto ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private async void OnGuardarPerfil(object? sender, EventArgs e)
    {
        var datos = new PerfilEntrenamiento(Codigo(Objetivos, objetivo), Codigo(NivelesExp, nivel), Lista(tipos.Text), Lista(areas.Text), Entero(dias.Text));
        if (await IntentarAsync(() => Api.PutAsync<PerfilEntrenamiento>("/api/entrenamiento/perfil", datos)))
            await AvisarAsync("Objetivos", "Tus objetivos fueron guardados.");
    }

    private async void OnGuardarRestriccion(object? sender, EventArgs e)
    {
        var datos = new Restriccion(Texto(restriccionTipo.Text) ?? "", Texto(restriccionDescripcion.Text), Lista(zonas.Text), Lista(evitar.Text), true, restriccionActiva.IsToggled);
        if (await IntentarAsync(() => Api.PutAsync<Restriccion>("/api/entrenamiento/restriccion", datos)))
            await AvisarAsync("Restricción", "La restricción fue guardada y la verá tu entrenador.");
    }

    private async void OnAgregarContacto(object? sender, EventArgs e)
    {
        var nombre = await DisplayPromptAsync("Contacto de emergencia", "Nombre completo");
        if (string.IsNullOrWhiteSpace(nombre)) return;
        var telefono = await DisplayPromptAsync("Contacto de emergencia", "Teléfono", keyboard: Keyboard.Telephone);
        if (string.IsNullOrWhiteSpace(telefono)) return;
        var relacion = await DisplayActionSheetAsync("Relación", "Cancelar", null, "Padre/Madre", "Pareja", "Hermano/a", "Hijo/a", "Amigo/a", "Otro");
        if (relacion is null or "Cancelar") return;
        var datos = new Contacto(0, nombre.Trim(), telefono.Trim(), relacion, false);
        if (await IntentarAsync(() => Api.PostAsync<Contacto>("/api/entrenamiento/contactos", datos)))
            await IntentarAsync(CargarContactosAsync);
    }
}
