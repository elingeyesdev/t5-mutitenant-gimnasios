using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Demostración visual y pautas del plan nutricional para un ejercicio de la rutina.</summary>
public class SimulacionEjercicioPage : PaginaBase
{
    private static readonly IReadOnlyDictionary<string, string> AnimacionesLocales =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [TextoEjercicio.Normalizar("Sentadilla Libre")] = "simulacion_sentadilla.gif",
            [TextoEjercicio.Normalizar("Press de Banca Plano")] = "simulacion_press_banca_plano.gif",
            [TextoEjercicio.Normalizar("Press Inclinado con Barra")] = "simulacion_press_inclinado_barra.gif",
            [TextoEjercicio.Normalizar("Press Inclinado con Mancuernas")] = "simulacion_press_inclinado_mancuernas.gif",
            [TextoEjercicio.Normalizar("Flexiones de Brazo (Push-ups)")] = "simulacion_flexiones_brazo.gif",
            [TextoEjercicio.Normalizar("Sentadilla Frontal")] = "simulacion_sentadilla_frontal.gif",
            [TextoEjercicio.Normalizar("Sentadilla en Máquina Smith")] = "simulacion_sentadilla_smith.gif",
            [TextoEjercicio.Normalizar("Prensa de Piernas 45°")] = "simulacion_prensa_piernas_45.gif",
            [TextoEjercicio.Normalizar("Sentadilla Hack")] = "simulacion_sentadilla_hack.gif",
            [TextoEjercicio.Normalizar("Extensión de Cuádriceps")] = "simulacion_extension_cuadriceps.gif",
            [TextoEjercicio.Normalizar("Curl de Femoral Tumbado")] = "simulacion_curl_femoral_tumbado.gif",
            [TextoEjercicio.Normalizar("Aperturas en Máquina (Peck Deck)")] = "simulacion_aperturas_peck_deck.gif",
            [TextoEjercicio.Normalizar("Fondos en Paralelas (Pecho)")] = "simulacion_fondos_paralelas_pecho.gif",
            [TextoEjercicio.Normalizar("Cruces en Polea Baja")] = "simulacion_cruces_polea_baja.gif",
            [TextoEjercicio.Normalizar("Cruces en Polea Alta")] = "simulacion_cruces_polea_alta.gif",
            [TextoEjercicio.Normalizar("Curl de Femoral Sentado")] = "simulacion_curl_femoral_sentado.gif",
            [TextoEjercicio.Normalizar("Peso Muerto Rumano con Barra")] = "simulacion_peso_muerto_rumano_barra.gif",
            [TextoEjercicio.Normalizar("Peso Muerto Rumano con Mancuernas")] = "simulacion_peso_muerto_rumano_mancuernas.gif",
            [TextoEjercicio.Normalizar("Abducción de Cadera en Máquina")] = "simulacion_abduccion_cadera_maquina.gif",
            [TextoEjercicio.Normalizar("Aducción de Cadera en Máquina")] = "simulacion_adduccion_cadera_maquina.gif",
            [TextoEjercicio.Normalizar("Elevación de Talones Sentado")] = "simulacion_elevacion_talones_sentado.gif",
            [TextoEjercicio.Normalizar("Aperturas con Mancuernas")] = "simulacion_aperturas_mancuernas.gif",
            [TextoEjercicio.Normalizar("Pullover con Mancuerna")] = "simulacion_pullover_mancuerna.gif",
            [TextoEjercicio.Normalizar("Zancadas (Lunges) Estáticas")] = "simulacion_zancadas_estaticas.gif",
            [TextoEjercicio.Normalizar("Sentadilla Búlgara")] = "simulacion_sentadilla_bulgara.gif",
            [TextoEjercicio.Normalizar("Elevación de Talones de Pie")] = "simulacion_elevacion_talones_pie.gif",
            [TextoEjercicio.Normalizar("Dominadas Libres (Pronas)")] = "simulacion_dominadas_libres.gif",
            [TextoEjercicio.Normalizar("Dominadas Supinas (Chin-ups)")] = "simulacion_dominadas_supinas.gif",
            [TextoEjercicio.Normalizar("Dominadas Asistidas")] = "simulacion_dominadas_asistidas.gif",
            [TextoEjercicio.Normalizar("Jalón al Pecho Agarre Abierto")] = "simulacion_jalon_pecho_abierto.gif",
            [TextoEjercicio.Normalizar("Remo con Barra")] = "simulacion_remo_barra.gif",
            [TextoEjercicio.Normalizar("Peso Muerto Convencional")] = "simulacion_peso_muerto_convencional.gif",
            [TextoEjercicio.Normalizar("Remo Gironda (Sentado en Polea)")] = "simulacion_remo_gironda.gif",
            [TextoEjercicio.Normalizar("Remo con Mancuerna a 1 Mano")] = "simulacion_remo_mancuerna_una_mano.gif",
            [TextoEjercicio.Normalizar("Remo en Máquina (Chest Supported)")] = "simulacion_remo_maquina_chest_supported.gif",
            [TextoEjercicio.Normalizar("Pulldown con Brazo Recto")] = "simulacion_pulldown_brazo_recto.gif",
            [TextoEjercicio.Normalizar("Press Arnold")] = "simulacion_press_arnold.gif",
            [TextoEjercicio.Normalizar("Elevaciones Laterales con Mancuernas")] = "simulacion_elevaciones_laterales_mancuernas.gif",
            [TextoEjercicio.Normalizar("Elevaciones Laterales en Polea")] = "simulacion_elevaciones_laterales_polea.gif",
            [TextoEjercicio.Normalizar("Elevaciones Frontales")] = "simulacion_elevaciones_frontales.gif",
            [TextoEjercicio.Normalizar("Press Declinado con Barra")] = "simulacion_press_declinado_barra.gif",
            [TextoEjercicio.Normalizar("Press Militar con Barra")] = "simulacion_press_militar_barra.gif",
            [TextoEjercicio.Normalizar("Press Militar con Mancuernas")] = "simulacion_press_militar_mancuernas.gif",
            [TextoEjercicio.Normalizar("Encogimientos con Barra (Trapecios)")] = "simulacion_encogimientos_barra.gif",
            [TextoEjercicio.Normalizar("Encogimientos con Mancuernas")] = "simulacion_encogimientos_mancuernas.gif",
            [TextoEjercicio.Normalizar("Curl de Bíceps con Barra Recta")] = "simulacion_curl_biceps_barra_recta.gif",
            [TextoEjercicio.Normalizar("Curl de Bíceps con Barra EZ")] = "simulacion_curl_biceps_barra_ez.gif",
            [TextoEjercicio.Normalizar("Curl de Bíceps Alterno")] = "simulacion_curl_biceps_alterno.gif",
            [TextoEjercicio.Normalizar("Curl Martillo")] = "simulacion_curl_martillo.gif",
            [TextoEjercicio.Normalizar("Curl Predicador en Máquina")] = "simulacion_curl_predicador_maquina.gif",
            [TextoEjercicio.Normalizar("Extensión de Tríceps en Polea Alta")] = "simulacion_extension_triceps_polea_alta.gif",
            [TextoEjercicio.Normalizar("Extensión de Tríceps con Cuerda")] = "simulacion_extension_triceps_cuerda.gif",
            [TextoEjercicio.Normalizar("Press Francés con Barra EZ")] = "simulacion_press_frances_barra_ez.gif",
            [TextoEjercicio.Normalizar("Patada de Tríceps con Mancuerna")] = "simulacion_patada_triceps_mancuerna.gif",
            [TextoEjercicio.Normalizar("Bicicleta Estática")] = "simulacion_bicicleta_estatica.gif",
            [TextoEjercicio.Normalizar("Salto a la Comba")] = "simulacion_salto_comba.gif",
            [TextoEjercicio.Normalizar("Burpees")] = "simulacion_burpees.gif",
            [TextoEjercicio.Normalizar("Kettlebell Swing")] = "simulacion_kettlebell_swing.gif",
            [TextoEjercicio.Normalizar("Mountain Climbers")] = "simulacion_mountain_climbers.gif",
            [TextoEjercicio.Normalizar("Crunches (Encogimientos)")] = "simulacion_crunches.gif",
            [TextoEjercicio.Normalizar("Russian Twist (Giros Rusos)")] = "simulacion_russian_twist.gif",
            [TextoEjercicio.Normalizar("Elevación de Piernas Tumbado")] = "simulacion_elevacion_piernas_tumbado.gif",
            [TextoEjercicio.Normalizar("Pájaros (Posteriores con Mancuerna)")] = "simulacion_pajaros_mancuernas.gif",
            [TextoEjercicio.Normalizar("Pájaros en Máquina (Peck Deck Inverso)")] = "simulacion_pajaros_maquina.gif",
            [TextoEjercicio.Normalizar("Crunches en Polea Alta")] = "simulacion_crunches_polea_alta.gif",
            [TextoEjercicio.Normalizar("Hip Thrust con Barra")] = "simulacion_hip_thrust_barra.gif",
            [TextoEjercicio.Normalizar("Jalón al Pecho Agarre Cerrado")] = "simulacion_jalon_pecho_cerrado.gif",
            [TextoEjercicio.Normalizar("Extensión Tras Nuca con Mancuerna")] = "simulacion_extension_tras_nuca_mancuerna.gif",
            [TextoEjercicio.Normalizar("Bicicleta de Spinning")] = "simulacion_bicicleta_estatica.gif",
            [TextoEjercicio.Normalizar("Elíptica")] = "simulacion_eliptica.gif",
            [TextoEjercicio.Normalizar("Saltos al Cajón (Box Jumps)")] = "simulacion_saltos_cajon.gif",
            [TextoEjercicio.Normalizar("Rueda Abdominal (Ab Wheel)")] = "simulacion_rueda_abdominal.gif",
            [TextoEjercicio.Normalizar("Jumping Jacks")] = "simulacion_jumping_jacks.gif",
            [TextoEjercicio.Normalizar("Battle Ropes")] = "simulacion_battle_ropes.gif",
        };

    private static readonly IReadOnlyDictionary<string, string> ImagenesLocales =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [TextoEjercicio.Normalizar("Plancha Abdominal (Plank)")] = "simulacion_plancha_abdominal.png",
            [TextoEjercicio.Normalizar("Plancha Lateral")] = "simulacion_plancha_lateral.png",
        };

    private readonly int ejercicioId;
    private readonly RefreshView refresco;
    private readonly VerticalStackLayout cuerpo = new() { Padding = 16, Spacing = 0 };
    private readonly ActivityIndicator indicador = new()
    {
        IsVisible = false,
        IsRunning = false,
        HorizontalOptions = LayoutOptions.Center,
        Margin = new Thickness(0, 12),
    };

    public SimulacionEjercicioPage(int ejercicioId)
    {
        this.ejercicioId = ejercicioId;
        Title = "Ejercicio";
        refresco = new RefreshView
        {
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Children = { indicador, cuerpo },
                },
            },
        };
        refresco.Refreshing += async (_, _) =>
        {
            await IntentarAsync(CargarAsync);
            refresco.IsRefreshing = false;
        };
        Content = refresco;
    }

    protected override async Task CargarAsync()
    {
        indicador.IsVisible = indicador.IsRunning = true;
        try
        {
            var simulacion = await Api.GetAsync<Simulacion>($"/api/simulaciones/ejercicios/{ejercicioId}");
            Title = simulacion.Ejercicio;
            var vistas = new List<View>
            {
                Tarjeta(simulacion.Ejercicio, simulacion.Descripcion, simulacion.GrupoMuscular,
                    simulacion.Equipo is null ? null : $"Equipo: {simulacion.Equipo}",
                    $"Dificultad: {Textos.Dificultad(simulacion.Dificultad)}"),
                Seccion("Demostración"),
                CrearDemostracion(simulacion),
                Seccion("Pautas del plan nutricional"),
            };
            vistas.AddRange(CrearPautas(simulacion));

            cuerpo.Clear();
            foreach (var vista in vistas) cuerpo.Add(vista);
        }
        finally
        {
            indicador.IsVisible = indicador.IsRunning = false;
        }
    }

    private static View CrearDemostracion(Simulacion simulacion)
    {
        if (AnimacionesLocales.TryGetValue(TextoEjercicio.Normalizar(simulacion.Ejercicio), out var archivo))
        {
            var animacion = new Image
            {
                Source = ImageSource.FromFile(archivo),
                HeightRequest = 220,
                Aspect = Aspect.AspectFit,
            };
            animacion.Loaded += async (_, _) =>
            {
                animacion.IsAnimationPlaying = false;
                await Task.Delay(100);
                animacion.IsAnimationPlaying = true;
            };
            SemanticProperties.SetDescription(animacion, $"Animación de {simulacion.Ejercicio}");
            return new Border
            {
                Style = Estilo("Tarjeta"),
                Content = animacion,
            };
        }

        if (ImagenesLocales.TryGetValue(TextoEjercicio.Normalizar(simulacion.Ejercicio), out var imagenLocal))
        {
            var imagen = new Image
            {
                Source = ImageSource.FromFile(imagenLocal),
                HeightRequest = 220,
                Aspect = Aspect.AspectFit,
            };
            SemanticProperties.SetDescription(imagen, $"Imagen de {simulacion.Ejercicio}");
            return new Border
            {
                Style = Estilo("Tarjeta"),
                Content = imagen,
            };
        }

        if (TryUri(simulacion.ImagenUrl, out var imagenUri))
        {
            var imagen = new Image { Source = ImageSource.FromUri(imagenUri), HeightRequest = 220, Aspect = Aspect.AspectFit };
            SemanticProperties.SetDescription(imagen, $"Imagen de {simulacion.Ejercicio}");
            return new Border { Style = Estilo("Tarjeta"), Content = imagen };
        }

        if (simulacion.TipoSimulacion.Equals("VIDEO", StringComparison.OrdinalIgnoreCase) &&
            IdYoutubeValido(simulacion.YoutubeVideoId))
        {
            var tarjeta = new Border
            {
                Style = Estilo("Tarjeta"),
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children = { Vacio("Este ejercicio se muestra mediante un video externo."), },
                },
            };
            var abrir = new Button { Text = "Ver video del ejercicio", Style = Estilo("Secundario"), HorizontalOptions = LayoutOptions.Start };
            abrir.Clicked += async (_, _) =>
            {
                try
                {
                    var uri = new Uri($"https://www.youtube.com/watch?v={simulacion.YoutubeVideoId}");
                    if (!await Launcher.Default.OpenAsync(uri))
                        await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Video", "No se pudo abrir el video.", "Aceptar");
                }
                catch (Exception ex) when (ex is UriFormatException or InvalidOperationException)
                {
                    await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Video", "No se pudo abrir el video.", "Aceptar");
                }
            };
            ((VerticalStackLayout)tarjeta.Content).Add(abrir);
            return tarjeta;
        }

        return new Border
        {
            Style = Estilo("Tarjeta"),
            Content = Vacio("Demostración visual pendiente de incorporar."),
        };
    }

    private static bool IdYoutubeValido(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id.Length is >= 6 and <= 64 && id.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');

    private static bool TryUri(string? valor, out Uri uri)
    {
        uri = null!;
        var url = GestionGimnasios.Movil.Servicios.Api.UrlCompleta(valor);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var creada)) return false;
        uri = creada;
        return creada.Scheme is "http" or "https";
    }

    private static IEnumerable<View> CrearPautas(Simulacion simulacion)
    {
        if (simulacion.Nutricion is null)
            return new View[] { Vacio(simulacion.AvisoNutricion ?? "No hay pautas nutricionales disponibles para este ejercicio.") };

        var p = simulacion.Nutricion;
        var vistas = new List<View>
        {
            Tarjeta("Objetivo diario", p.Macros, $"Preparado por {p.Profesional}", $"Actualizado el {Hora.FechaHora(p.ActualizadoEn)}"),
        };
        if (!string.IsNullOrWhiteSpace(p.AntesDeEntrenar)) vistas.Add(Tarjeta("Antes de entrenar", p.AntesDeEntrenar));
        if (!string.IsNullOrWhiteSpace(p.DespuesDeEntrenar)) vistas.Add(Tarjeta("Después de entrenar", p.DespuesDeEntrenar));
        if (!string.IsNullOrWhiteSpace(p.Hidratacion)) vistas.Add(Tarjeta("Hidratación", p.Hidratacion));
        return vistas;
    }
}
