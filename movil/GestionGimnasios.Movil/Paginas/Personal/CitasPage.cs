using System.Globalization;
using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Personal;

/// <summary>Agenda de citas del nutricionista con sus pacientes (CU-29).</summary>
public class CitasPage() : PaginaTarjetas("Citas")
{
    private static readonly string[] Estados = ["PENDIENTE", "CONFIRMADA", "ATENDIDA", "CANCELADA", "NO_ASISTIO"];
    private static readonly string[] Tipos = ["CONSULTA", "SEGUIMIENTO", "EVALUACION"];

    protected override async Task<IEnumerable<View>> Armar()
    {
        var citas = await Api.GetAsync<List<Cita>>("/api/personal/citas");
        var nueva = new Button { Text = "Agendar cita", Margin = new Thickness(0, 0, 0, 10) };
        nueva.Clicked += OnNueva;
        var vistas = new List<View> { nueva };

        var hoy = Hora.Hoy;
        var proximas = citas.Where(c => c.Fecha >= hoy).OrderBy(c => c.Fecha).ThenBy(c => c.Hora).ToList();
        vistas.Add(Seccion("Próximas"));
        if (proximas.Count == 0) vistas.Add(Vacio("No tienes citas programadas."));
        foreach (var c in proximas) vistas.Add(TarjetaCita(c));

        var pasadas = citas.Where(c => c.Fecha < hoy).OrderByDescending(c => c.Fecha).Take(20).ToList();
        if (pasadas.Count > 0)
        {
            vistas.Add(Seccion("Anteriores"));
            foreach (var c in pasadas) vistas.Add(TarjetaCita(c));
        }
        return vistas;
    }

    private Border TarjetaCita(Cita c)
    {
        var t = Tarjeta(c.Paciente, c.Cuando, c.EstadoTexto, c.Notas);
        ((Label)((Layout)t.Content!)[2]).TextColor = c.ColorEstado;
        return ConBoton(t, "Cambiar estado", async () =>
        {
            var elegido = await DisplayActionSheetAsync("Estado de la cita", "Cancelar", null, Estados.Select(Textos.Estado).ToArray());
            var i = Array.IndexOf(Estados.Select(Textos.Estado).ToArray(), elegido);
            if (i < 0) return;
            if (await IntentarAsync(() => Api.PutAsync<Cita>($"/api/personal/citas/{c.Id}/estado", new { estado = Estados[i] })))
                await RecargarAsync();
        });
    }

    private async void OnNueva(object? sender, EventArgs e)
    {
        List<Asesoria> pacientes = [];
        if (!await IntentarAsync(async () => pacientes = await Api.GetAsync<List<Asesoria>>("/api/personal/asesorias/activas"))) return;
        if (pacientes.Count == 0)
        {
            await AvisarAsync("Citas", "Solo puedes agendar citas con pacientes que tengan una asesoría activa contigo.");
            return;
        }
        var nombre = await DisplayActionSheetAsync("Paciente", "Cancelar", null, pacientes.Select(p => p.Cliente).ToArray());
        var paciente = pacientes.FirstOrDefault(p => p.Cliente == nombre);
        if (paciente is null) return;

        var fechaTexto = await DisplayPromptAsync("Fecha", "Formato dd/mm/aaaa", "Siguiente", "Cancelar",
            initialValue: Hora.Hoy.AddDays(1).ToString("dd/MM/yyyy"), keyboard: Keyboard.Text);
        if (!DateOnly.TryParseExact(fechaTexto?.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            if (fechaTexto is not null) await AvisarAsync("Fecha", "Usa el formato dd/mm/aaaa.");
            return;
        }
        var horaTexto = await DisplayPromptAsync("Hora", "Formato HH:mm (24 horas)", "Siguiente", "Cancelar", initialValue: "09:00");
        if (!TimeOnly.TryParseExact(horaTexto?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
        {
            if (horaTexto is not null) await AvisarAsync("Hora", "Usa el formato HH:mm.");
            return;
        }
        var tipo = await DisplayActionSheetAsync("Tipo de cita", "Cancelar", null, Tipos.Select(Textos.Estado).ToArray());
        var iTipo = Array.IndexOf(Tipos.Select(Textos.Estado).ToArray(), tipo);
        if (iTipo < 0) return;

        var datos = new { pacienteId = paciente.ClienteId, fecha, hora, tipo = Tipos[iTipo] };
        if (await IntentarAsync(() => Api.PostAsync<Cita>("/api/personal/citas", datos)))
            await RecargarAsync();
    }
}
