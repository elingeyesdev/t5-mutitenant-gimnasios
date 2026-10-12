using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Plan y pagos del socio (CU-13). Las altas y pagos los registra recepción en el panel web.</summary>
public class MiPlanPage() : PaginaTarjetas("Mi plan")
{
    protected override async Task<IEnumerable<View>> Armar()
    {
        var vistas = new List<View>();
        var suscripciones = await Api.GetAsync<List<Suscripcion>>("/api/suscripciones/mias");
        var activa = suscripciones.FirstOrDefault(s => s.Estado == "ACTIVA");

        vistas.Add(Seccion("Plan actual"));
        if (activa is null)
            vistas.Add(Tarjeta("Sin plan activo", "Acércate a la recepción de tu gimnasio para activar o renovar tu plan."));
        else
            vistas.Add(Tarjeta(activa.Plan, activa.Precio, activa.Vigencia, activa.Restante,
                $"Gimnasio principal: {activa.SucursalPrincipal ?? "—"}",
                activa.RenovacionAutomatica ? "Renovación automática activada" : null));

        var cadena = Servicios.Sesion.Usuario?.CadenaId;
        var planes = await Api.GetAsync<List<Plan>>("/api/suscripciones/planes" + (cadena is null ? "" : $"?cadenaId={cadena}"));
        if (planes.Count > 0)
        {
            vistas.Add(Seccion("Planes disponibles"));
            vistas.AddRange(planes.Select(p => Tarjeta(p.Nombre, $"Bs {p.PrecioMensual:0.00} / mes", p.Descripcion,
                p.Beneficios.Count > 0 ? "• " + string.Join("\n• ", p.Beneficios) : null)));
        }

        vistas.Add(Seccion("Historial de pagos"));
        var pagos = suscripciones.SelectMany(s => s.Pagos.Select(p => (s.Plan, Pago: p))).OrderByDescending(x => x.Pago.Fecha).ToList();
        if (pagos.Count == 0) vistas.Add(Vacio("Sin pagos registrados."));
        vistas.AddRange(pagos.Select(x => Tarjeta(x.Plan, x.Pago.Texto, Textos.Estado(x.Pago.Estado))));
        return vistas;
    }
}
