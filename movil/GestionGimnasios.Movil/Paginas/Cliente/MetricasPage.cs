using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Medidas corporales, IMC y su evolución (CU-19).</summary>
public class MetricasPage() : PaginaTarjetas("Medidas corporales")
{
    protected override async Task<IEnumerable<View>> Armar()
    {
        var metricas = await Api.GetAsync<List<Metrica>>("/api/metricas");
        var ultima = metricas.OrderByDescending(m => m.RegistradaEn).FirstOrDefault();
        var conPeso = metricas.Where(m => m.PesoKg is not null).OrderBy(m => m.RegistradaEn).ToList();
        var altura = Sesion.Usuario?.Perfil?.AlturaCm;

        var registrar = new Button { Text = "Registrar medidas de hoy", Margin = new Thickness(0, 0, 0, 10) };
        registrar.Clicked += async (_, _) => await IrAsync(new NuevaMetricaPage(altura));
        var vistas = new List<View> { registrar };

        if (ultima is null)
        {
            vistas.Add(Vacio("Aún no registras medidas."));
            return vistas;
        }
        var cambio = conPeso.Count >= 2 ? conPeso[^1].PesoKg - conPeso[0].PesoKg : null;
        vistas.Add(Tarjeta("Última medición", ultima.Fecha, ultima.Resumen,
            Textos.Imc(ultima.PesoKg ?? conPeso.LastOrDefault()?.PesoKg, altura),
            cambio is null ? null : $"Cambio de peso desde tu primera medición: {cambio:+0.#;-0.#;0} kg"));

        var circunferencias = string.Join(" · ", new[]
        {
            ultima.PechoCm is null ? null : $"pecho {ultima.PechoCm:0.#}",
            ultima.CaderaCm is null ? null : $"cadera {ultima.CaderaCm:0.#}",
            ultima.BrazoCm is null ? null : $"brazo {ultima.BrazoCm:0.#}",
            ultima.MusloCm is null ? null : $"muslo {ultima.MusloCm:0.#}",
            ultima.PantorrillaCm is null ? null : $"pantorrilla {ultima.PantorrillaCm:0.#}",
        }.Where(x => x is not null));
        if (circunferencias.Length > 0) vistas.Add(Tarjeta("Circunferencias (cm)", circunferencias));

        vistas.Add(Seccion("Historial"));
        foreach (var m in metricas.OrderByDescending(m => m.RegistradaEn))
        {
            var t = Tarjeta(m.Fecha, m.Resumen, m.Notas);
            var id = m.Id;
            vistas.Add(ConBoton(t, "Eliminar", async () =>
            {
                if (await ConfirmarAsync("Eliminar", "¿Eliminar esta medición?", "Eliminar") &&
                    await IntentarAsync(() => Api.DeleteAsync($"/api/metricas/{id}")))
                    await RecargarAsync();
            }, "Peligroso"));
        }
        return vistas;
    }
}

/// <summary>Formulario para registrar peso, composición y circunferencias.</summary>
public class NuevaMetricaPage : PaginaBase
{
    private readonly Dictionary<string, Entry> campos = [];

    public NuevaMetricaPage(decimal? altura)
    {
        Title = "Registrar medidas";
        var cuerpo = new VerticalStackLayout { Padding = 16, Spacing = 4 };
        cuerpo.Add(new Label { Text = "Completa solo los datos que tengas. El IMC se calcula con tu peso y tu altura.", Style = Estilo("Detalle") });
        foreach (var (clave, etiqueta, valor) in new (string, string, decimal?)[]
                 {
                     ("pesoKg", "Peso (kg)", null), ("alturaCm", "Altura (cm)", altura), ("grasaPct", "Grasa corporal (%)", null),
                     ("masaMuscularKg", "Masa muscular (kg)", null), ("cinturaCm", "Cintura (cm)", null), ("pechoCm", "Pecho (cm)", null),
                     ("caderaCm", "Cadera (cm)", null), ("brazoCm", "Brazo (cm)", null), ("musloCm", "Muslo (cm)", null),
                     ("pantorrillaCm", "Pantorrilla (cm)", null),
                 })
        {
            cuerpo.Add(new Label { Text = etiqueta, Margin = new Thickness(0, 6, 0, 0) });
            var entrada = new Entry { Keyboard = Keyboard.Numeric, Text = Numero(valor) };
            campos[clave] = entrada;
            cuerpo.Add(entrada);
        }
        var notas = new Editor { Placeholder = "Notas (opcional)", AutoSize = EditorAutoSizeOption.TextChanges };
        cuerpo.Add(notas);
        var guardar = new Button { Text = "Guardar", Margin = new Thickness(0, 12, 0, 0) };
        guardar.Clicked += async (_, _) =>
        {
            decimal? V(string c) => LeerDecimal(campos[c].Text);
            if (campos.Keys.All(c => V(c) is null))
            {
                await AvisarAsync("Medidas", "Ingresa al menos un dato.");
                return;
            }
            var datos = new
            {
                pesoKg = V("pesoKg"), alturaCm = V("alturaCm"), grasaPct = V("grasaPct"), masaMuscularKg = V("masaMuscularKg"),
                cinturaCm = V("cinturaCm"), pechoCm = V("pechoCm"), caderaCm = V("caderaCm"), brazoCm = V("brazoCm"),
                musloCm = V("musloCm"), pantorrillaCm = V("pantorrillaCm"), notas = Texto(notas.Text),
            };
            if (await IntentarAsync(async () =>
                {
                    await Api.PostAsync<Metrica>("/api/metricas", datos);
                    await Sesion.ActualizarUsuarioAsync();
                }))
                await Navigation.PopAsync();
        };
        cuerpo.Add(guardar);
        Content = new ScrollView { Content = cuerpo };
    }
}
