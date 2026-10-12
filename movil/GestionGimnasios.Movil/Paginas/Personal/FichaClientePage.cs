using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Paginas.Comun;

namespace GestionGimnasios.Movil.Paginas.Personal;

/// <summary>Ficha del alumno para su asesor: datos, restricciones, progreso, rutina y plan nutricional (CU-27, CU-28).</summary>
public class FichaClientePage(Asesoria asesoria) : PaginaTarjetas(asesoria.Cliente)
{
    private Asesoria asesoria = asesoria;

    protected override async Task<IEnumerable<View>> Armar()
    {
        var id = asesoria.ClienteId;
        var ficha = await Api.GetAsync<FichaCliente>($"/api/personal/clientes/{id}/ficha");
        var rutinas = await Api.GetAsync<List<Rutina>>($"/api/rutinas/usuario/{id}");
        var plan = await Api.GetAsync<PlanNutricional?>($"/api/personal/clientes/{id}/plan");
        var p = ficha.Perfil;

        var acciones = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 8, Margin = new Thickness(0, 0, 0, 10) };
        var mensaje = new Button { Text = "Mensaje" };
        mensaje.Clicked += async (_, _) => await IntentarAsync(() => ChatPage.AbrirConAsync(Navigation, id, asesoria.Cliente));
        var meta = new Button { Text = "Meta semanal", Style = Estilo("Secundario") };
        meta.Clicked += OnMeta;
        acciones.Add(mensaje);
        acciones.Add(meta, 1);

        var vistas = new List<View>
        {
            acciones,
            Tarjeta($"{p.Nombres} {p.Apellidos}", asesoria.ClienteTexto,
                p.FechaNacimiento is null ? null : $"Nacimiento: {p.FechaNacimiento:dd/MM/yyyy}",
                p.AlturaCm is null ? null : $"Altura: {p.AlturaCm:0} cm",
                "Experiencia: " + (p.NivelExperiencia is null ? "sin indicar" : Textos.Dificultad(p.NivelExperiencia)),
                p.CondicionesMedicas is null ? null : $"Condiciones médicas: {p.CondicionesMedicas}",
                asesoria.MetaSesionesSemana is null ? "Sin meta semanal" : $"Meta: {asesoria.MetaSesionesSemana} entrenamientos por semana"),
        };

        if (ficha.Restriccion is { Activa: true } r)
        {
            var t = Tarjeta($"⚠ Restricción: {r.Tipo}", r.Descripcion,
                r.AreasAfectadas.Count == 0 ? null : "Zonas: " + string.Join(", ", r.AreasAfectadas),
                r.MovimientosEvitar.Count == 0 ? null : "Evitar: " + string.Join(", ", r.MovimientosEvitar));
            t.Stroke = Colores.Alerta;
            vistas.Add(t);
        }
        if (ficha.Entrenamiento is { } e)
            vistas.Add(Tarjeta("Objetivos", e.ObjetivoPrincipal?.Replace('_', ' '), e.DiasDisponibles is null ? null : $"{e.DiasDisponibles} días por semana",
                e.TiposPreferidos.Count == 0 ? null : "Prefiere: " + string.Join(", ", e.TiposPreferidos)));
        if (ficha.UltimaMetrica is { } m)
            vistas.Add(Tarjeta("Última medición", m.Fecha, m.Resumen, Textos.Imc(m.PesoKg, p.AlturaCm)));

        vistas.Add(Seccion("Rutina"));
        var activa = rutinas.FirstOrDefault(x => x.Activa);
        if (activa is null) vistas.Add(Vacio("Sin rutina asignada."));
        else vistas.Add(Tarjeta(activa.Nombre, activa.Detalle, string.Join("\n", activa.Ejercicios.OrderBy(x => x.Orden).Select(x => $"• {x.Ejercicio}: {x.Prescripcion}"))));
        var asignar = new Button { Text = activa is null ? "Crear y asignar rutina" : "Reemplazar rutina", Style = Estilo("Secundario") };
        asignar.Clicked += async (_, _) => await IrAsync(new RutinaFormPage(id, asesoria.Cliente));
        vistas.Add(asignar);

        vistas.Add(Seccion("Plan nutricional"));
        vistas.Add(plan is null ? Vacio("Sin plan nutricional.") : Tarjeta("Objetivo diario", plan.Macros, $"Actualizado el {Hora.FechaHora(plan.ActualizadoEn)}", plan.Notas));
        var editar = new Button { Text = plan is null ? "Crear plan nutricional" : "Editar plan nutricional", Style = Estilo("Secundario") };
        editar.Clicked += async (_, _) => await IrAsync(new PlanNutricionalFormPage(id, asesoria.Cliente, plan));
        vistas.Add(editar);

        vistas.Add(Seccion("Últimos entrenamientos"));
        if (ficha.UltimasSesiones.Count == 0) vistas.Add(Vacio("Aún no registra entrenamientos."));
        vistas.AddRange(ficha.UltimasSesiones.Select(s => Tarjeta(s.Actividad, s.Texto, Hora.Duracion(s.DuracionSeg))));

        var terminar = new Button { Text = "Terminar asesoría", Style = Estilo("Peligroso"), Margin = new Thickness(0, 12, 0, 0) };
        terminar.Clicked += async (_, _) =>
        {
            if (await ConfirmarAsync("Terminar", $"¿Terminar la asesoría con {asesoria.Cliente}?", "Terminar") &&
                await IntentarAsync(() => Api.PutAsync<Asesoria>($"/api/personal/asesorias/{asesoria.Id}/cancelar")))
                await Navigation.PopAsync();
        };
        vistas.Add(terminar);
        return vistas;
    }

    private async void OnMeta(object? sender, EventArgs e)
    {
        var texto = await DisplayPromptAsync("Meta semanal", "¿Cuántos entrenamientos por semana? Si no los cumple, se marcará inactivo y recibirás una alerta.",
            "Guardar", "Cancelar", initialValue: asesoria.MetaSesionesSemana?.ToString() ?? "3", maxLength: 1, keyboard: Keyboard.Numeric);
        if (Entero(texto) is not int meta) return;
        Asesoria? nueva = null;
        if (await IntentarAsync(async () => nueva = await Api.PutAsync<Asesoria>($"/api/personal/asesorias/{asesoria.Id}/meta", new { metaSesionesSemana = meta })))
        {
            asesoria = nueva!;
            await RecargarAsync();
        }
    }
}
