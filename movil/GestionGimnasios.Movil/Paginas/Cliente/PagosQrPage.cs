using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Renovación de suscripciones mediante orden QR y registro de comprobantes.</summary>
public class PagosQrPage() : PaginaTarjetas("Pagos con QR")
{
    protected override async Task<IEnumerable<View>> Armar()
    {
        var suscripciones = await Api.GetAsync<List<Suscripcion>>("/api/suscripciones/mias");
        var ordenes = await Api.GetAsync<List<OrdenPagoQr>>("/api/pagos-qr/mias");
        var vistas = new List<View>
        {
            Tarjeta("Pago con QR", "El QR es demostrativo y no realiza un cobro real.", "La suscripción se renueva únicamente cuando recepción confirma el pago.")
        };

        vistas.Add(Seccion("Renovar suscripción"));
        var disponibles = suscripciones.Where(s => s.Estado != "CANCELADA").ToList();
        if (disponibles.Count == 0)
        {
            vistas.Add(Vacio("No tienes suscripciones disponibles para renovar."));
        }
        else
        {
            foreach (var suscripcion in disponibles)
            {
                var meses = new Picker
                {
                    Title = "Meses a renovar",
                    ItemsSource = Enumerable.Range(1, 12).ToList(),
                    SelectedIndex = 0,
                    HorizontalOptions = LayoutOptions.Start,
                    WidthRequest = 180,
                };
                var tarjeta = Tarjeta(suscripcion.Plan, suscripcion.Vigencia, suscripcion.Precio, suscripcion.Restante);
                ((Layout)tarjeta.Content!).Add(meses);
                ConBoton(tarjeta, "Generar orden QR", () => CrearOrdenAsync(suscripcion, meses));
                vistas.Add(tarjeta);
            }
        }

        vistas.Add(Seccion("Historial de órdenes"));
        if (ordenes.Count == 0)
        {
            vistas.Add(Vacio("Todavía no tienes órdenes de pago."));
        }
        else
        {
            foreach (var orden in ordenes)
            {
                var tarjeta = Tarjeta(orden.Plan, orden.MontoTexto, $"{orden.Meses} {(orden.Meses == 1 ? "mes" : "meses")} · Creada: {orden.CreadaTexto}",
                    $"Vence la orden: {orden.ExpiraTexto}", orden.VenceSuscripcionTexto);
                ((VerticalStackLayout)tarjeta.Content!).Add(new Label { Text = orden.EstadoTexto, TextColor = orden.ColorEstado, FontFamily = "OpenSansSemibold", FontSize = 13 });
                ConBoton(tarjeta, orden.Estado == "PENDIENTE" ? "Ver QR y comprobante" : "Ver detalle", () => IrAsync(new PagoQrDetallePage(orden.Id)));
                vistas.Add(tarjeta);
            }
        }

        return vistas;
    }

    private async Task CrearOrdenAsync(Suscripcion suscripcion, Picker meses)
    {
        if (Ocupado) return;
        var cantidad = Convert.ToInt32(meses.SelectedItem ?? 1);
        if (!await ConfirmarAsync("Generar orden", $"Se generará una orden por {cantidad} {(cantidad == 1 ? "mes" : "meses")} para {suscripcion.Plan}.")) return;

        OrdenPagoQr? orden = null;
        if (await IntentarAsync(async () => orden = await Api.PostAsync<OrdenPagoQr>("/api/pagos-qr", new { suscripcionId = suscripcion.Id, meses = cantidad })) && orden is not null)
            await IrAsync(new PagoQrDetallePage(orden.Id));
    }
}
