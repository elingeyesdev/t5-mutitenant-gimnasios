using System.Diagnostics;
using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>
/// Ejecución de un entrenamiento (CU-17): cronómetro, registro de cada serie y descanso con cuenta
/// regresiva. Al terminar se guarda la sesión con todas sus series en la API.
/// </summary>
public class EntrenarPage : PaginaBase
{
    /// <summary>Ejercicio en curso dentro de la sesión (de la rutina o agregado en un entrenamiento libre).</summary>
    private sealed class Bloque(int ejercicioId, int? rutinaEjercicioId, string nombre, string tipoRegistro, int descansoSeg)
    {
        public int EjercicioId { get; } = ejercicioId;
        public int? RutinaEjercicioId { get; } = rutinaEjercicioId;
        public string Nombre { get; } = nombre;
        public bool PorTiempo { get; } = tipoRegistro is "TIEMPO_DISTANCIA" or "SOLO_TIEMPO";
        public bool ConPeso { get; } = tipoRegistro == "PESO_REPETICIONES";
        public int DescansoSeg { get; } = descansoSeg;
        public int Series { get; set; }
    }

    private readonly Rutina? rutina;
    private readonly Stopwatch reloj = Stopwatch.StartNew();
    private readonly List<object> series = [];
    private readonly Label tiempo = new() { FontSize = 30, FontFamily = "OpenSansSemibold", TextColor = Colores.TextoSobrePrimario };
    private readonly Label descanso = new() { FontSize = 15, TextColor = Colores.TextoSobrePrimario };
    private readonly Label resumen = new() { FontSize = 13, TextColor = Colores.TextoSobrePrimario };
    private readonly VerticalStackLayout bloques = new() { Spacing = 0 };
    private readonly Picker selector = new() { Title = "Elige un ejercicio para agregar" };
    private readonly Entry deporte = new() { Placeholder = "Ej.: cardio en cinta, fútbol, natación" };
    private readonly Editor notas = new() { Placeholder = "¿Cómo te sentiste? (opcional)", AutoSize = EditorAutoSizeOption.TextChanges };
    private IDispatcherTimer? timer;
    private int descansoRestante;
    private bool guardado;

    public EntrenarPage(Rutina? rutina)
    {
        this.rutina = rutina;
        Title = rutina?.Nombre ?? "Entrenamiento libre";
        selector.ItemDisplayBinding = new Binding(nameof(Ejercicio.Nombre));

        var cabecera = new Border
        {
            Style = Estilo("TarjetaDestacada"),
            Content = new VerticalStackLayout { Children = { new Label { Text = "Tiempo de entrenamiento", TextColor = Colores.TextoSobrePrimario, FontSize = 13 }, tiempo, descanso, resumen } },
        };
        var terminar = new Button { Text = "Terminar y guardar", Margin = new Thickness(0, 12, 0, 0) };
        terminar.Clicked += OnTerminar;

        var cuerpo = new VerticalStackLayout { Padding = 16, Spacing = 4, Children = { cabecera } };
        if (rutina is null)
        {
            var agregar = new Button { Text = "Agregar ejercicio", Style = Estilo("Secundario") };
            agregar.Clicked += (_, _) => AgregarElegido();
            cuerpo.Add(new Label { Text = "Actividad", Style = Estilo("Seccion") });
            cuerpo.Add(deporte);
            cuerpo.Add(new Label { Text = "Ejercicios (opcional)", Style = Estilo("Seccion") });
            cuerpo.Add(selector);
            cuerpo.Add(agregar);
        }
        else
        {
            foreach (var e in rutina.Ejercicios.OrderBy(e => e.Orden))
                bloques.Add(CrearBloque(new Bloque(e.EjercicioId, e.Id, e.Ejercicio, e.TipoRegistro, e.DescansoSeg ?? 60), e.Prescripcion, e.Repeticiones, e.PesoKg));
        }
        cuerpo.Add(bloques);
        cuerpo.Add(new Label { Text = "Notas", Style = Estilo("Seccion") });
        cuerpo.Add(notas);
        cuerpo.Add(terminar);
        Content = new ScrollView { Content = cuerpo };
        ActualizarResumen();
    }

    protected override async Task CargarAsync()
    {
        timer ??= Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick -= OnTick;
        timer.Tick += OnTick;
        timer.Start();
        DeviceDisplay.Current.KeepScreenOn = true;
        if (rutina is null && selector.ItemsSource is null)
            selector.ItemsSource = await Api.GetAsync<List<Ejercicio>>("/api/ejercicios");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DeviceDisplay.Current.KeepScreenOn = false;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        tiempo.Text = reloj.Elapsed.ToString(@"hh\:mm\:ss");
        if (descansoRestante > 0)
        {
            descansoRestante--;
            descanso.Text = descansoRestante > 0 ? $"Descanso: {descansoRestante} s" : "¡A la siguiente serie!";
            if (descansoRestante == 0)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); }
                catch (FeatureNotSupportedException) { }
            }
        }
    }

    private void AgregarElegido()
    {
        if (selector.SelectedItem is not Ejercicio e) return;
        bloques.Add(CrearBloque(new Bloque(e.Id, null, e.Nombre, e.TipoRegistro, 60), e.Detalle, null, null));
        selector.SelectedItem = null;
    }

    private View CrearBloque(Bloque b, string detalle, string? repsSugeridas, decimal? pesoSugerido)
    {
        var hechas = new Label { Text = "0 series registradas", Style = Estilo("Detalle") };
        var cantidad = new Entry
        {
            Keyboard = Keyboard.Numeric,
            Placeholder = b.PorTiempo ? "Minutos" : "Repeticiones",
            Text = b.PorTiempo ? null : new string((repsSugeridas ?? "").TakeWhile(char.IsDigit).ToArray()),
        };
        var extra = new Entry
        {
            Keyboard = Keyboard.Numeric,
            Placeholder = b.PorTiempo ? "Metros" : "Peso (kg)",
            Text = b.PorTiempo ? null : Numero(pesoSugerido),
            IsVisible = b.PorTiempo || b.ConPeso,
        };
        var completar = new Button { Text = "Serie completada", Style = Estilo("Chico") };
        completar.Clicked += (_, _) =>
        {
            var valor = Entero(cantidad.Text);
            if (valor is not > 0)
            {
                _ = AvisarAsync("Serie", b.PorTiempo ? "Indica los minutos de la serie." : "Indica cuántas repeticiones hiciste.");
                return;
            }
            b.Series++;
            series.Add(new
            {
                numeroSerie = b.Series,
                ejercicioId = b.EjercicioId,
                rutinaEjercicioId = b.RutinaEjercicioId,
                repeticiones = b.PorTiempo ? (int?)null : valor,
                pesoKg = b.ConPeso ? LeerDecimal(extra.Text) : null,
                duracionSeg = b.PorTiempo ? valor * 60 : (int?)null,
                distanciaM = b.PorTiempo ? Entero(extra.Text) : null,
                descansoSeg = b.DescansoSeg,
            });
            hechas.Text = $"{b.Series} series registradas";
            hechas.TextColor = Colores.Exito;
            descansoRestante = b.DescansoSeg;
            descanso.Text = $"Descanso: {descansoRestante} s";
            ActualizarResumen();
        };

        var fila = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 8 };
        fila.Add(cantidad);
        fila.Add(extra, 1);
        fila.Add(completar, 2);
        return new Border
        {
            Style = Estilo("Tarjeta"),
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children = { new Label { Text = b.Nombre, Style = Estilo("Nombre") }, new Label { Text = detalle, Style = Estilo("Detalle") }, fila, hechas },
            },
        };
    }

    private void ActualizarResumen() => resumen.Text = $"{series.Count} series registradas en total";

    private async void OnTerminar(object? sender, EventArgs e)
    {
        var duracion = (int)reloj.Elapsed.TotalSeconds;
        if (series.Count == 0 && duracion < 60)
        {
            await AvisarAsync("Entrenamiento", "Registra al menos una serie o entrena más de un minuto antes de guardar.");
            return;
        }
        if (!await ConfirmarAsync("Terminar", $"¿Guardar el entrenamiento de {Hora.Duracion(duracion)} con {series.Count} series?", "Guardar")) return;
        var datos = new
        {
            rutinaId = rutina?.Id,
            tipoDeporte = rutina is null ? Texto(deporte.Text) ?? "Entrenamiento libre" : null,
            duracionSeg = duracion,
            notas = Texto(notas.Text),
            series,
        };
        if (await IntentarAsync(() => Api.PostAsync<SesionEntrenamiento>("/api/entrenamiento/sesiones", datos)))
        {
            guardado = true;
            timer?.Stop();
            await AvisarAsync("¡Buen trabajo!", "Tu entrenamiento quedó guardado. Lo verás en tu progreso y tu entrenador también.");
            await Navigation.PopAsync();
        }
    }

    protected override bool OnBackButtonPressed()
    {
        if (guardado || series.Count == 0) return base.OnBackButtonPressed();
        Dispatcher.Dispatch(async () =>
        {
            if (await ConfirmarAsync("Salir", "Tienes series sin guardar. ¿Salir sin guardar el entrenamiento?", "Salir"))
            {
                guardado = true;
                await Navigation.PopAsync();
            }
        });
        return true;
    }
}
