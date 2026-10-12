using System.Collections.ObjectModel;
using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Personal;

/// <summary>El entrenador arma una rutina con ejercicios del catálogo y se la asigna al alumno (CU-16).</summary>
public class RutinaFormPage : PaginaBase
{
    private sealed record Item(Ejercicio Ejercicio, int Series, string Repeticiones, decimal? PesoKg, string? Dia, int Descanso)
    {
        public string Texto => $"{Ejercicio.Nombre}: {Series} × {Repeticiones}{(PesoKg is null ? "" : $" · {PesoKg:0.#} kg")}{(Dia is null ? "" : $" · {Textos.Dia(Dia)}")}";
    }

    private static readonly string[] Dificultades = ["PRINCIPIANTE", "INTERMEDIO", "AVANZADO"];
    private static readonly string[] Dias = ["", "LUNES", "MARTES", "MIERCOLES", "JUEVES", "VIERNES", "SABADO", "DOMINGO"];

    private readonly int clienteId;
    private readonly ObservableCollection<Item> items = [];
    private readonly Entry nombre = new() { Placeholder = "Ej.: Fuerza tren superior" };
    private readonly Editor descripcion = new() { Placeholder = "Indicaciones generales (opcional)", AutoSize = EditorAutoSizeOption.TextChanges };
    private readonly Picker dificultad = new() { ItemsSource = Dificultades.Select(Textos.Dificultad).ToList(), SelectedIndex = 0 };
    private readonly Entry semanas = new() { Keyboard = Keyboard.Numeric, Text = "4" };
    private readonly Picker ejercicio = new() { Title = "Ejercicio del catálogo" };
    private readonly Entry series = new() { Keyboard = Keyboard.Numeric, Text = "3", Placeholder = "Series" };
    private readonly Entry reps = new() { Text = "10-12", Placeholder = "Repeticiones" };
    private readonly Entry peso = new() { Keyboard = Keyboard.Numeric, Placeholder = "Peso kg" };
    private readonly Entry descanso = new() { Keyboard = Keyboard.Numeric, Text = "60", Placeholder = "Descanso s" };
    private readonly Picker dia = new() { Title = "Día", ItemsSource = Dias.Select(d => d == "" ? "Cualquier día" : Textos.Dia(d)).ToList(), SelectedIndex = 0 };
    private readonly VerticalStackLayout lista = new();

    public RutinaFormPage(int clienteId, string cliente)
    {
        this.clienteId = clienteId;
        Title = $"Rutina para {cliente}";
        ejercicio.ItemDisplayBinding = new Binding(nameof(Ejercicio.Nombre));
        var agregar = new Button { Text = "Agregar ejercicio", Style = Estilo("Secundario") };
        agregar.Clicked += OnAgregar;
        var guardar = new Button { Text = "Guardar y asignar", Margin = new Thickness(0, 12, 0, 0) };
        guardar.Clicked += OnGuardar;
        items.CollectionChanged += (_, _) => Dibujar();

        var fila1 = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 6 };
        fila1.Add(series);
        fila1.Add(reps, 1);
        fila1.Add(peso, 2);
        var fila2 = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 6 };
        fila2.Add(descanso);
        fila2.Add(dia, 1);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 4,
                Children =
                {
                    new Label { Text = "Nombre" }, nombre,
                    new Label { Text = "Dificultad" }, dificultad,
                    new Label { Text = "Duración (semanas)" }, semanas,
                    descripcion,
                    Seccion("Ejercicios"),
                    lista,
                    new Border
                    {
                        Style = Estilo("Tarjeta"),
                        Content = new VerticalStackLayout
                        {
                            Spacing = 6,
                            Children =
                            {
                                ejercicio,
                                new Label { Text = "Series · repeticiones · peso", Style = Estilo("Detalle") }, fila1,
                                new Label { Text = "Descanso (segundos) · día", Style = Estilo("Detalle") }, fila2,
                                agregar,
                            },
                        },
                    },
                    guardar,
                },
            },
        };
        Dibujar();
    }

    protected override async Task CargarAsync()
    {
        if (ejercicio.ItemsSource is null)
            ejercicio.ItemsSource = await Api.GetAsync<List<Ejercicio>>("/api/ejercicios");
    }

    private void Dibujar()
    {
        lista.Clear();
        if (items.Count == 0) lista.Add(Vacio("Agrega al menos un ejercicio."));
        foreach (var i in items)
        {
            var item = i;
            lista.Add(ConBoton(Tarjeta(item.Texto, $"Descanso {item.Descanso} s"), "Quitar", () =>
            {
                items.Remove(item);
                return Task.CompletedTask;
            }, "Peligroso"));
        }
    }

    private async void OnAgregar(object? sender, EventArgs e)
    {
        if (ejercicio.SelectedItem is not Ejercicio ej)
        {
            await AvisarAsync("Ejercicio", "Elige un ejercicio del catálogo.");
            return;
        }
        var d = dia.SelectedIndex > 0 ? Dias[dia.SelectedIndex] : null;
        items.Add(new Item(ej, Entero(series.Text) ?? 3, Texto(reps.Text) ?? "10", LeerDecimal(peso.Text), d, Entero(descanso.Text) ?? 60));
        ejercicio.SelectedItem = null;
        peso.Text = "";
    }

    private async void OnGuardar(object? sender, EventArgs e)
    {
        if (Texto(nombre.Text) is null || items.Count == 0)
        {
            await AvisarAsync("Rutina", "Indica un nombre y agrega al menos un ejercicio.");
            return;
        }
        var datos = new
        {
            nombre = nombre.Text.Trim(),
            descripcion = Texto(descripcion.Text),
            dificultad = Dificultades[Math.Max(0, dificultad.SelectedIndex)],
            duracionSemanas = Entero(semanas.Text),
            asignadaAId = clienteId,
            ejercicios = items.Select((x, i) => new
            {
                ejercicioId = x.Ejercicio.Id, orden = i + 1, series = x.Series, repeticiones = x.Repeticiones,
                pesoKg = x.PesoKg, diaSemana = x.Dia, descansoSeg = x.Descanso,
            }).ToList(),
        };
        if (await IntentarAsync(() => Api.PostAsync<Rutina>("/api/rutinas", datos)))
        {
            await AvisarAsync("Rutina asignada", "El alumno ya puede verla y entrenar con ella.");
            await Navigation.PopAsync();
        }
    }
}
