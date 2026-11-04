using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Consulta y canje de beneficios del socio (RF-03, HU-48, CU-37).</summary>
public class BeneficiosPage() : PaginaTarjetas("Beneficios y canjes")
{
    protected override async Task<IEnumerable<View>> Armar()
    {
        var beneficios = await Api.GetAsync<List<BeneficioDisponible>>("/api/beneficios/disponibles");
        var canjes = await Api.GetAsync<List<CanjeBeneficio>>("/api/beneficios/canjes/mios");
        var vistas = new List<View> { Seccion("Beneficios disponibles") };

        if (beneficios.Count == 0)
            vistas.Add(Vacio("No hay beneficios disponibles para tu cadena."));
        else
            vistas.AddRange(beneficios.Select(ArmarBeneficio));

        vistas.Add(Seccion("Historial de canjes"));
        if (canjes.Count == 0)
            vistas.Add(Vacio("Todavía no tienes canjes registrados."));
        else
            vistas.AddRange(canjes.Select(c => Tarjeta(
                c.Beneficio,
                $"{c.Cadena} · {c.TipoTexto}",
                $"Código: {c.Codigo}",
                $"Canjeado: {c.Fecha}",
                $"Estado: {c.EstadoTexto}",
                c.FechaEntrega)));

        return vistas;
    }

    private Border ArmarBeneficio(BeneficioDisponible beneficio)
    {
        var tarjeta = Tarjeta(
            beneficio.Nombre,
            $"{beneficio.Cadena} · {beneficio.TipoTexto}",
            beneficio.Descripcion,
            beneficio.Vigencia,
            beneficio.Condiciones is null ? null : $"Condiciones: {beneficio.Condiciones}",
            beneficio.ReglasTexto,
            $"Canjes realizados: {beneficio.CanjesUsados}",
            beneficio.CuposTexto);

        var contenido = (VerticalStackLayout)tarjeta.Content!;
        contenido.Add(new Label
        {
            Text = beneficio.ElegibilidadTexto,
            TextColor = beneficio.ColorElegibilidad,
            Style = Estilo("Insignia"),
        });

        if (beneficio.Elegible)
        {
            var canjear = new Button { Text = "Canjear beneficio", Style = Estilo("Chico"), Margin = new Thickness(0, 6, 0, 0) };
            canjear.Clicked += async (_, _) => await CanjearAsync(beneficio, canjear);
            contenido.Add(canjear);
        }

        return tarjeta;
    }

    private async Task CanjearAsync(BeneficioDisponible beneficio, Button boton)
    {
        if (Ocupado || !boton.IsEnabled) return;
        boton.IsEnabled = false;
        try
        {
            if (!await ConfirmarAsync("Confirmar canje", $"¿Canjear el beneficio {beneficio.Nombre}?", "Canjear")) return;

            CanjeBeneficio? canje = null;
            if (await IntentarAsync(async () => canje = await Api.PostAsync<CanjeBeneficio>($"/api/beneficios/{beneficio.Id}/canjear")))
            {
                await AvisarAsync("Canje registrado", $"Tu código es {canje!.Codigo}. Muéstralo en recepción.");
                await RecargarAsync();
            }
        }
        finally
        {
            boton.IsEnabled = true;
        }
    }
}
