using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Personal;

/// <summary>Clases del instructor con sus inscritos y registro de asistencia (CU-23, CU-24).</summary>
public class AgendaPage() : PaginaTarjetas("Mis clases")
{
    private bool soloHoy = true;

    protected override async Task<IEnumerable<View>> Armar()
    {
        var clases = await Api.GetAsync<List<ClaseAgenda>>($"/api/personal/agenda?soloHoy={soloHoy.ToString().ToLowerInvariant()}");
        var cambiar = new Button { Text = soloHoy ? "Ver toda la semana" : "Ver solo hoy", Style = Estilo("Secundario"), Margin = new Thickness(0, 0, 0, 10) };
        cambiar.Clicked += async (_, _) =>
        {
            soloHoy = !soloHoy;
            await RecargarAsync();
        };
        var vistas = new List<View> { cambiar };
        if (clases.Count == 0) vistas.Add(Vacio(soloHoy ? "No tienes clases hoy." : "No tienes clases asignadas."));

        foreach (var c in clases.OrderBy(c => c.Inicio))
        {
            var t = Tarjeta(c.Actividad, c.Detalle, c.Cupo);
            var contenido = (Layout)t.Content!;
            if (c.Participantes.Count == 0) contenido.Add(new Label { Text = "Sin inscritos.", Style = Estilo("Detalle") });
            foreach (var p in c.Participantes)
            {
                var fila = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)], Padding = new Thickness(0, 4) };
                fila.Add(new VerticalStackLayout
                {
                    Children =
                    {
                        new Label { Text = p.NombreCompleto, FontSize = 14 },
                        new Label { Text = p.EstadoTexto, TextColor = p.ColorEstado, FontSize = 12 },
                    },
                });
                if (p.Pendiente)
                {
                    var asistio = new Button { Text = "Asistió", Style = Estilo("Chico"), VerticalOptions = LayoutOptions.Center };
                    var horario = c.HorarioId;
                    asistio.Clicked += async (_, _) =>
                    {
                        if (await IntentarAsync(() => Api.PostAsync($"/api/actividades/horarios/{horario}/asistencia", new { usuarioId = p.UsuarioId })))
                            await RecargarAsync();
                    };
                    fila.Add(asistio, 1);
                }
                contenido.Add(fila);
            }
            vistas.Add(t);
        }

        var estadisticas = await Api.GetAsync<List<EstadisticaClase>>("/api/personal/estadisticas-asistencia");
        if (estadisticas.Count > 0)
        {
            vistas.Add(Seccion("Asistencia de los últimos 30 días"));
            vistas.AddRange(estadisticas.Select(e => Tarjeta(e.Actividad, e.Texto)));
        }
        return vistas;
    }
}
