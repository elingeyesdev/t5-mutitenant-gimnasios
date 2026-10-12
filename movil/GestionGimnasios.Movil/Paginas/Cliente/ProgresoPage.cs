using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Historial de entrenamientos y récords de fuerza (CU-18).</summary>
public class ProgresoPage() : PaginaTarjetas("Mi progreso")
{
    protected override async Task<IEnumerable<View>> Armar()
    {
        var sesiones = await Api.GetAsync<List<SesionEntrenamiento>>("/api/entrenamiento/sesiones?limite=50");
        var records = await Api.GetAsync<List<RecordFuerza>>("/api/entrenamiento/records");

        var desde = DateTime.UtcNow.AddDays(-30);
        var recientes = sesiones.Where(s => s.Inicio >= desde).ToList();
        var vistas = new List<View>
        {
            Tarjeta("Últimos 30 días",
                $"{recientes.Count} entrenamientos",
                $"{recientes.Sum(s => s.Series.Count)} series registradas",
                $"{Hora.Duracion(recientes.Sum(s => s.DuracionSeg ?? 0))} entrenando"),
            Seccion("Récords de fuerza"),
        };
        if (records.Count == 0) vistas.Add(Vacio("Registra series con peso para ver tus récords."));
        foreach (var r in records.OrderByDescending(r => r.Maximo).Take(15))
        {
            var t = Tarjeta(r.Ejercicio, r.GrupoMuscular, r.Texto);
            ((Layout)t.Content!).Add(Barras(r.Historial.TakeLast(8).ToList()));
            vistas.Add(t);
        }

        vistas.Add(Seccion("Entrenamientos"));
        if (sesiones.Count == 0) vistas.Add(Vacio("Aún no registraste entrenamientos."));
        foreach (var s in sesiones)
        {
            var detalle = s.Series.GroupBy(x => x.Ejercicio ?? "Ejercicio")
                .Select(g => $"{g.Key}: {g.Count()} series, máx. {g.Max(x => x.PesoKg) ?? 0:0.#} kg");
            vistas.Add(Tarjeta(s.Titulo, s.Detalle, s.Notas, string.Join("\n", detalle)));
        }
        return vistas;
    }

    /// <summary>Evolución del peso máximo en barras simples (sin librerías de gráficos).</summary>
    private static View Barras(List<PuntoRecord> puntos)
    {
        var grilla = new Grid { HeightRequest = 70, ColumnSpacing = 4, Margin = new Thickness(0, 8, 0, 0) };
        if (puntos.Count == 0) return grilla;
        var max = (double)Math.Max(1, puntos.Max(p => p.PesoMaxKg));
        for (var i = 0; i < puntos.Count; i++)
        {
            grilla.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var alto = Math.Max(4, 54 * (double)puntos[i].PesoMaxKg / max);
            var columna = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.End,
                Children =
                {
                    new BoxView { HeightRequest = alto, Color = Colores.Primario, CornerRadius = 3 },
                    new Label { Text = puntos[i].Fecha.ToString("dd/MM"), FontSize = 9, HorizontalOptions = LayoutOptions.Center, TextColor = Colores.GrisTexto },
                },
            };
            grilla.Add(columna, i);
        }
        return grilla;
    }
}
