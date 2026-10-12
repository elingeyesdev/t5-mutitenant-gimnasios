using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Personal;

/// <summary>Personas dentro de la sede y últimos accesos, con salida manual (CU-15).</summary>
public class AuditoriaPage() : PaginaTarjetas("Auditoría de accesos")
{
    protected override async Task<IEnumerable<View>> Armar()
    {
        var dentro = await Api.GetAsync<PaginaApi<Acceso>>("/api/accesos?estado=ACTIVO&limite=100");
        var ultimos = await Api.GetAsync<PaginaApi<Acceso>>("/api/accesos?estado=COMPLETADO&limite=30");
        var vistas = new List<View> { Seccion($"Dentro ahora ({dentro.Total})") };
        if (dentro.Datos.Count == 0) vistas.Add(Vacio("No hay nadie dentro."));
        foreach (var a in dentro.Datos)
        {
            var id = a.Id;
            vistas.Add(ConBoton(Tarjeta($"{a.Usuario} · {Textos.Rol(a.Rol)}", a.Email, a.Texto), "Registrar salida", async () =>
            {
                if (await ConfirmarAsync("Salida", $"¿Registrar la salida de {a.Usuario}?", "Registrar") &&
                    await IntentarAsync(() => Api.PutAsync<Acceso>($"/api/accesos/{id}/salida")))
                    await RecargarAsync();
            }));
        }
        vistas.Add(Seccion("Últimas salidas"));
        vistas.AddRange(ultimos.Datos.Select(a => Tarjeta($"{a.Usuario} · {Textos.Rol(a.Rol)}", a.Texto, $"Método: {a.Metodo}")));
        return vistas;
    }
}
