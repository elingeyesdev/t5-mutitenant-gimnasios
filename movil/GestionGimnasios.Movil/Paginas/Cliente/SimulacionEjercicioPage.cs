using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Demostración visual y pautas del plan nutricional para un ejercicio de la rutina.</summary>
public class SimulacionEjercicioPage : PaginaBase
{
    private static readonly IReadOnlyDictionary<string, string> AnimacionesLocales =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sentadilla Libre"] = "simulacion_sentadilla.gif",
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
        if (AnimacionesLocales.TryGetValue(simulacion.Ejercicio.Trim(), out var archivo))
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

        return new Border
        {
            Style = Estilo("Tarjeta"),
            Content = Vacio("Este ejercicio todavía no tiene una animación local disponible."),
        };
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
