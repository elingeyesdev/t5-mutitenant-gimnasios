using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Paginas.Comun;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Catálogo de entrenadores y nutricionistas y solicitudes de asesoría del socio (CU-25).</summary>
public class AsesoresPage() : PaginaTarjetas("Asesores")
{
    protected override async Task<IEnumerable<View>> Armar()
    {
        var solicitudes = await Api.GetAsync<List<Asesoria>>("/api/personal/asesorias/mias");
        var catalogo = await Api.GetAsync<List<Asesor>>("/api/personal/catalogo");
        var vistas = new List<View> { Seccion("Mis asesorías") };

        var vigentes = solicitudes.Where(s => s.Cancelable).ToList();
        if (vigentes.Count == 0) vistas.Add(Vacio("No tienes asesorías. Elige un asesor del catálogo."));
        foreach (var s in vigentes)
        {
            var t = Tarjeta(s.AsesorTexto, s.SucursalAsesor, Textos.Estado(s.Estado),
                s.MetaSesionesSemana is null ? null : $"Meta: {s.MetaSesionesSemana} entrenamientos por semana");
            if (s.Estado == "ACTIVA")
                ConBoton(t, "Enviar mensaje", () => IntentarAsync(() => ChatPage.AbrirConAsync(Navigation, s.AsesorId, s.Asesor)), "Chico");
            vistas.Add(ConBoton(t, s.Estado == "PENDIENTE" ? "Cancelar solicitud" : "Terminar asesoría", async () =>
            {
                if (await ConfirmarAsync("Asesoría", $"¿Cancelar la asesoría con {s.Asesor}?", "Sí, cancelar") &&
                    await IntentarAsync(() => Api.PutAsync<Asesoria>($"/api/personal/asesorias/{s.Id}/cancelar")))
                    await RecargarAsync();
            }, "Peligroso"));
        }

        vistas.Add(Seccion("Catálogo de asesores"));
        if (catalogo.Count == 0) vistas.Add(Vacio("No hay asesores disponibles en tu gimnasio."));
        var ocupado = vigentes.Count > 0;
        foreach (var a in catalogo)
        {
            var t = Tarjeta(a.NombreCompleto, a.Detalle, a.Especialidad);
            if (!ocupado)
                ConBoton(t, "Solicitar asesoría", async () =>
                {
                    if (await ConfirmarAsync("Solicitar", $"¿Enviar solicitud a {a.NombreCompleto}?", "Enviar") &&
                        await IntentarAsync(() => Api.PostAsync<Asesoria>("/api/personal/asesorias", new { asesorId = a.Id })))
                    {
                        await AvisarAsync("Solicitud enviada", "Te avisaremos cuando el asesor la acepte.");
                        await RecargarAsync();
                    }
                });
            vistas.Add(t);
        }
        return vistas;
    }
}
