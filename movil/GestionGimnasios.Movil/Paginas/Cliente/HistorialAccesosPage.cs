using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Ingresos y salidas del socio registrados en recepción (CU-14).</summary>
public class HistorialAccesosPage() : PaginaTarjetas("Mis ingresos")
{
    protected override async Task<IEnumerable<View>> Armar()
    {
        var accesos = await Api.GetAsync<List<Acceso>>("/api/accesos/historial?limite=60");
        if (accesos.Count == 0) return [Vacio("Aún no registras ingresos al gimnasio.")];
        var minutos = accesos.Where(a => a.MinutosDentro is not null).Select(a => a.MinutosDentro!.Value).ToList();
        var vistas = new List<View>
        {
            Tarjeta($"{accesos.Count} visitas", minutos.Count == 0 ? null : $"Promedio de {minutos.Average():0} minutos por visita"),
            Seccion("Detalle"),
        };
        vistas.AddRange(accesos.Select(a => Tarjeta(a.Sucursal, a.Texto, a.EstadoTexto)));
        return vistas;
    }
}
