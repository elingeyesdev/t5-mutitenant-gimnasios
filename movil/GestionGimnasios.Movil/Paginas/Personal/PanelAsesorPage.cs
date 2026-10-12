using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Personal;

/// <summary>Panel del entrenador y del nutricionista: solicitudes, alumnos y alertas de inactividad (CU-26).</summary>
public class PanelAsesorPage() : PaginaTarjetas("Panel")
{
    protected override async Task<IEnumerable<View>> Armar()
    {
        var panel = await Api.GetAsync<PanelAsesor>("/api/personal/panel");
        var pendientes = await Api.GetAsync<List<Asesoria>>("/api/personal/asesorias/pendientes");
        var activas = await Api.GetAsync<List<Asesoria>>("/api/personal/asesorias/activas");
        var actividad = await Api.GetAsync<List<ActividadAlumno>>("/api/personal/asesorias/actividad");
        var u = Sesion.Usuario!;

        var vistas = new List<View>
        {
            new Label { Text = $"Hola, {u.Nombre}", Style = Estilo("Titulo") },
            new Label { Text = $"{u.RolLegible} · {u.Lugar}", Style = Estilo("Detalle"), Margin = new Thickness(0, 0, 0, 10) },
            Tarjeta($"{panel.ClientesActivos} alumnos activos",
                $"{panel.SolicitudesPendientes} solicitudes pendientes",
                u.Rol == "NUTRICIONISTA" ? $"{panel.CitasHoy} citas hoy" : null,
                $"{actividad.Count(a => a.Inactivo)} alumnos inactivos"),
            Seccion("Solicitudes pendientes"),
        };
        if (pendientes.Count == 0) vistas.Add(Vacio("No tienes solicitudes nuevas."));
        foreach (var s in pendientes)
        {
            var t = Tarjeta(s.Cliente, s.ClienteTexto, $"Solicitó el {Hora.FechaHora(s.CreadaEn)}");
            ConBoton(t, "Aceptar", async () =>
            {
                if (await IntentarAsync(() => Api.PutAsync<Asesoria>($"/api/personal/asesorias/{s.Id}/aceptar"))) await RecargarAsync();
            }, null);
            vistas.Add(ConBoton(t, "Rechazar", async () =>
            {
                if (await ConfirmarAsync("Rechazar", $"¿Rechazar la solicitud de {s.Cliente}?", "Rechazar") &&
                    await IntentarAsync(() => Api.PutAsync<Asesoria>($"/api/personal/asesorias/{s.Id}/rechazar")))
                    await RecargarAsync();
            }, "Peligroso"));
        }

        vistas.Add(Seccion("Mis alumnos"));
        if (activas.Count == 0) vistas.Add(Vacio("Aún no tienes alumnos."));
        foreach (var s in activas)
        {
            var a = actividad.FirstOrDefault(x => x.AsesoriaId == s.Id);
            var t = Tarjeta(s.Cliente, a?.Detalle, a is null ? null : a.Alerta,
                s.MetaSesionesSemana is null ? "Sin meta semanal" : $"Meta: {s.MetaSesionesSemana} entrenamientos por semana");
            if (a?.Inactivo == true) ((Label)((Layout)t.Content!)[2]).TextColor = Colores.Peligro;
            vistas.Add(ConBoton(t, "Ver ficha", () => IrAsync(new FichaClientePage(s)), null));
        }
        return vistas;
    }
}
