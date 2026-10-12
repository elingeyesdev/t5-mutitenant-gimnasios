using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Paginas.Cliente;

namespace GestionGimnasios.Movil.Paginas.Personal;

/// <summary>El asesor define macros y el menú de cada día del alumno (CU-27).</summary>
public class PlanNutricionalFormPage : PaginaBase
{
    private readonly int clienteId;
    private readonly Dictionary<string, Dictionary<string, string>> comidas;
    private readonly Entry kcal = new() { Keyboard = Keyboard.Numeric };
    private readonly Entry proteina = new() { Keyboard = Keyboard.Numeric };
    private readonly Entry carbohidratos = new() { Keyboard = Keyboard.Numeric };
    private readonly Entry grasa = new() { Keyboard = Keyboard.Numeric };
    private readonly Editor notas = new() { AutoSize = EditorAutoSizeOption.TextChanges, Placeholder = "Indicaciones generales" };
    private readonly Picker dia = new() { ItemsSource = MiNutricionPage.Dias.Select(Textos.Dia).ToList() };
    private readonly Dictionary<string, Editor> editores = [];
    private int diaActual = -1;

    public PlanNutricionalFormPage(int clienteId, string cliente, PlanNutricional? plan)
    {
        this.clienteId = clienteId;
        Title = $"Plan de {cliente}";
        comidas = plan?.PlanComidas.ToDictionary(d => d.Key, d => new Dictionary<string, string>(d.Value)) ?? [];
        kcal.Text = plan?.KcalDiarias?.ToString();
        proteina.Text = Numero(plan?.ProteinaG);
        carbohidratos.Text = Numero(plan?.CarbohidratosG);
        grasa.Text = Numero(plan?.GrasaG);
        notas.Text = plan?.Notas;

        var menu = new VerticalStackLayout { Spacing = 4 };
        foreach (var c in MiNutricionPage.Comidas)
        {
            editores[c] = new Editor { AutoSize = EditorAutoSizeOption.TextChanges, Placeholder = MiNutricionPage.Comida(c) };
            menu.Add(new Label { Text = MiNutricionPage.Comida(c), Style = Estilo("Detalle") });
            menu.Add(editores[c]);
        }
        dia.SelectedIndexChanged += (_, _) => CambiarDia();
        var guardar = new Button { Text = "Guardar plan", Margin = new Thickness(0, 12, 0, 0) };
        guardar.Clicked += OnGuardar;

        var macros = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)], RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)], ColumnSpacing = 8 };
        macros.Add(new Label { Text = "Kcal diarias" });
        macros.Add(kcal, 0, 1);
        macros.Add(new Label { Text = "Proteína (g)" }, 1);
        macros.Add(proteina, 1, 1);
        macros.Add(new Label { Text = "Carbohidratos (g)" }, 0, 2);
        macros.Add(carbohidratos, 0, 3);
        macros.Add(new Label { Text = "Grasa (g)" }, 1, 2);
        macros.Add(grasa, 1, 3);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 4,
                Children = { Seccion("Objetivo diario"), macros, notas, Seccion("Menú por día"), dia, menu, guardar },
            },
        };
        dia.SelectedIndex = 0;
    }

    /// <summary>Guarda en memoria lo escrito para el día anterior y muestra el día elegido.</summary>
    private void CambiarDia()
    {
        GuardarDiaActual();
        diaActual = dia.SelectedIndex;
        var actual = comidas.GetValueOrDefault(MiNutricionPage.Dias[diaActual]);
        foreach (var (c, editor) in editores) editor.Text = actual?.GetValueOrDefault(c);
    }

    private void GuardarDiaActual()
    {
        if (diaActual < 0) return;
        var valores = editores.Where(e => !string.IsNullOrWhiteSpace(e.Value.Text)).ToDictionary(e => e.Key, e => e.Value.Text.Trim());
        var clave = MiNutricionPage.Dias[diaActual];
        if (valores.Count == 0) comidas.Remove(clave);
        else comidas[clave] = valores;
    }

    private async void OnGuardar(object? sender, EventArgs e)
    {
        GuardarDiaActual();
        var datos = new
        {
            kcalDiarias = Entero(kcal.Text),
            proteinaG = LeerDecimal(proteina.Text),
            carbohidratosG = LeerDecimal(carbohidratos.Text),
            grasaG = LeerDecimal(grasa.Text),
            notas = Texto(notas.Text),
            planComidas = comidas,
        };
        if (await IntentarAsync(() => Api.PutAsync<PlanNutricional>($"/api/personal/clientes/{clienteId}/plan", datos)))
        {
            await AvisarAsync("Plan nutricional", "El plan fue guardado y el alumno ya puede verlo.");
            await Navigation.PopAsync();
        }
    }
}
