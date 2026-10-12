using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Personal;

/// <summary>Resumen del día para recepción, gerencia y administración (CU-30). Los reportes completos están en el panel web.</summary>
public class ResumenPage() : PaginaTarjetas("Resumen")
{
    protected override async Task<IEnumerable<View>> Armar()
    {
        var r = await Api.GetAsync<ResumenPanel>("/api/reportes/resumen?periodo=week");
        var u = Sesion.Usuario!;
        // Las cifras de la sede solo aplican al personal con sucursal; gerente y administrador ven su alcance completo.
        var hoy = u.SucursalId is null ? null : await Api.GetAsync<EstadisticasSucursalHoy>("/api/sucursales/estadisticas-hoy");
        var vistas = new List<View>
        {
            new Label { Text = $"Hola, {u.Nombre}", Style = Estilo("Titulo") },
            new Label { Text = $"{u.RolLegible} · {u.Lugar}", Style = Estilo("Detalle"), Margin = new Thickness(0, 0, 0, 10) },
            Indicadores(("Dentro ahora", hoy is null ? r.PersonasDentro.ToString() : $"{r.PersonasDentro}/{hoy.Capacidad}"), ("Ingresos hoy", r.IngresosHoy.ToString())),
            Indicadores(("Reservas hoy", r.ReservasHoy.ToString()), hoy is null ? ("Sedes", "Todas") : ("Por llegar", hoy.Pendientes.ToString())),
            Indicadores(("Socios con plan", r.SociosConSuscripcion.ToString()), ("Usuarios activos", r.UsuariosActivos.ToString())),
        };
        foreach (var serie in new[] { r.Ingresos, r.Reservas, r.Usuarios })
        {
            vistas.Add(Seccion(serie.Titulo));
            vistas.Add(new Border { Style = Estilo("Tarjeta"), Content = Barras(serie.Puntos) });
        }
        vistas.Add(new Label
        {
            Text = "Los reportes detallados e imprimibles están en el panel web.",
            Style = Estilo("Detalle"),
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8),
        });
        return vistas;
    }

    private static Grid Indicadores(params (string Etiqueta, string Valor)[] datos)
    {
        var g = new Grid { ColumnSpacing = 10 };
        for (var i = 0; i < datos.Length; i++)
        {
            g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            g.Add(new Border
            {
                Style = Estilo("Tarjeta"),
                Content = new VerticalStackLayout
                {
                    Children =
                    {
                        new Label { Text = datos[i].Valor, Style = Estilo("Numero") },
                        new Label { Text = datos[i].Etiqueta, Style = Estilo("Detalle") },
                    },
                },
            }, i);
        }
        return g;
    }

    /// <summary>Barras horizontales por día (sin librerías de gráficos).</summary>
    private static View Barras(List<PuntoSerie> puntos)
    {
        if (puntos.Count == 0 || puntos.All(p => p.Valor == 0)) return new Label { Text = "Sin datos en el período.", Style = Estilo("Detalle") };
        var max = Math.Max(1, puntos.Max(p => p.Valor));
        var lista = new VerticalStackLayout { Spacing = 4 };
        foreach (var p in puntos)
        {
            var fila = new Grid { ColumnDefinitions = [new(70), new(GridLength.Star), new(36)], ColumnSpacing = 6 };
            fila.Add(new Label { Text = p.Etiqueta, FontSize = 12, VerticalOptions = LayoutOptions.Center });
            fila.Add(new ProgressBar { Progress = p.Valor / max, ProgressColor = Colores.Primario, VerticalOptions = LayoutOptions.Center }, 1);
            fila.Add(new Label { Text = p.Valor.ToString("0"), FontSize = 12, HorizontalTextAlignment = TextAlignment.End }, 2);
            lista.Add(fila);
        }
        return lista;
    }
}
