using GestionGimnasios.Movil.Paginas.Cliente;
using GestionGimnasios.Movil.Paginas.Personal;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Comun;

/// <summary>Menú "Más": accesos al resto de pantallas según el rol, perfil y cierre de sesión.</summary>
public class MasPage : PaginaBase
{
    private readonly Label sinLeer = new() { Style = Estilo("Insignia"), TextColor = Color.FromArgb("#C92A2A") };

    public MasPage()
    {
        Title = "Más";
        var u = Sesion.Usuario!;
        var lista = new VerticalStackLayout { Padding = 16, Spacing = 0 };

        lista.Add(new Border
        {
            Style = Estilo("TarjetaDestacada"),
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label { Text = u.NombreCompleto, FontSize = 18, FontFamily = "OpenSansSemibold", TextColor = Colors.White },
                    new Label { Text = u.Email, TextColor = Colors.White, FontSize = 13 },
                    new Label { Text = $"{u.RolLegible} · {u.Lugar}", TextColor = Colors.White, FontSize = 13 },
                },
            },
        });

        var opciones = new List<(string Seccion, string Texto, Func<Page> Pagina)>();
        if (Sesion.EsCliente)
        {
            opciones.Add(("Mi gimnasio", "Carnet virtual", () => new CarnetPage()));
            opciones.Add(("Mi gimnasio", "Mi plan y pagos", () => new MiPlanPage()));
            opciones.Add(("Mi gimnasio", "Mi historial de ingresos", () => new HistorialAccesosPage()));
            opciones.Add(("Entrenamiento", "Mi progreso y récords", () => new ProgresoPage()));
            opciones.Add(("Entrenamiento", "Medidas corporales", () => new MetricasPage()));
            opciones.Add(("Entrenamiento", "Plan nutricional", () => new MiNutricionPage()));
            opciones.Add(("Entrenamiento", "Entrenadores y nutricionistas", () => new AsesoresPage()));
            opciones.Add(("Entrenamiento", "Objetivos, restricciones y contactos", () => new DatosEntrenamientoPage()));
            opciones.Add(("Comunicación", "Mensajes", () => new ConversacionesPage()));
        }
        else if (Sesion.Nivel >= 2)
        {
            opciones.Add(("Mi trabajo", "Mis turnos", () => new TurnosPage()));
            if (Sesion.Nivel is >= 4 and < 10) opciones.Add(("Mi trabajo", "Auditoría de accesos", () => new AuditoriaPage()));
            if (Sesion.Nivel is 3) opciones.Add(("Mi trabajo", "Clases de hoy", () => new AgendaPage()));
        }
        opciones.Add(("Comunicación", "Notificaciones", () => new NotificacionesPage()));
        opciones.Add(("Cuenta", "Mi perfil", () => new PerfilPage()));

        foreach (var grupo in opciones.GroupBy(o => o.Seccion))
        {
            lista.Add(new Label { Text = grupo.Key, Style = Estilo("Seccion") });
            var tarjeta = new VerticalStackLayout();
            foreach (var o in grupo)
            {
                var fila = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)], Padding = new Thickness(4, 12) };
                fila.Add(new Label { Text = o.Texto, FontSize = 15, VerticalOptions = LayoutOptions.Center });
                fila.Add(o.Texto == "Notificaciones" ? sinLeer : new Label { Text = "›", FontSize = 20, TextColor = Colors.Gray }, 1);
                var crear = o.Pagina;
                var toque = new TapGestureRecognizer();
                toque.Tapped += async (_, _) => await IrAsync(crear());
                fila.GestureRecognizers.Add(toque);
                tarjeta.Add(fila);
            }
            lista.Add(new Border { Style = Estilo("Tarjeta"), Padding = new Thickness(12, 0), Content = tarjeta });
        }

        var salir = new Button { Text = "Cerrar sesión", Style = Estilo("Peligroso"), Margin = new Thickness(0, 12, 0, 0) };
        salir.Clicked += async (_, _) =>
        {
            if (await ConfirmarAsync("Cerrar sesión", "¿Quieres salir de tu cuenta?", "Salir")) App.CerrarSesion();
        };
        lista.Add(salir);
        lista.Add(new Label { Text = $"Servidor: {Sesion.Servidor}", Style = Estilo("Detalle"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 16) });
        Content = new ScrollView { Content = lista };
    }

    protected override async Task CargarAsync()
    {
        var r = await Api.GetAsync<System.Text.Json.JsonElement>("/api/notificaciones/sin-leer");
        var n = r.GetProperty("cantidad").GetInt32();
        sinLeer.Text = n > 0 ? $"{n} sin leer" : "›";
        sinLeer.TextColor = n > 0 ? Color.FromArgb("#C92A2A") : Colors.Gray;
    }
}
