using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Detalle de una orden QR, incluyendo el código y el comprobante del socio.</summary>
public class PagoQrDetallePage : PaginaBase
{
    private const long MaximoBytes = 5 * 1024 * 1024;
    private readonly int ordenId;
    private readonly VerticalStackLayout cuerpo = new() { Padding = 16, Spacing = 0 };
    private readonly RefreshView refresco;

    public PagoQrDetallePage(int id)
    {
        ordenId = id;
        Title = "Detalle del pago QR";
        refresco = new RefreshView { Content = new ScrollView { Content = cuerpo } };
        refresco.Refreshing += async (_, _) =>
        {
            await IntentarAsync(CargarAsync);
            refresco.IsRefreshing = false;
        };
        Content = refresco;
    }

    protected override Task CargarAsync() => CargarDetalleAsync();

    private async Task CargarDetalleAsync()
    {
        var orden = await Api.GetAsync<OrdenPagoQr>($"/api/pagos-qr/{ordenId}");
        cuerpo.Clear();
        cuerpo.Add(Seccion("Orden"));
        cuerpo.Add(Tarjeta(orden.Plan, $"Código: {orden.Codigo}", orden.MontoTexto,
            $"Estado: {orden.EstadoTexto}", $"Creada: {orden.CreadaTexto}",
            $"Vencimiento de la orden: {orden.ExpiraTexto}",
            orden.VenceSuscripcionTexto));

        if (orden.Estado == "PENDIENTE")
        {
            cuerpo.Add(Seccion("QR demostrativo"));
            cuerpo.Add(new Label
            {
                Text = "Este QR solo identifica la orden. No realiza un cobro real; espera la confirmación de recepción para renovar tu suscripción.",
                Style = Estilo("Detalle"),
                Margin = new Thickness(0, 0, 0, 10),
            });
            if (!string.IsNullOrWhiteSpace(orden.QrPngBase64))
            {
                var bytes = Convert.FromBase64String(orden.QrPngBase64);
                cuerpo.Add(new Image
                {
                    Source = ImageSource.FromStream(() => new MemoryStream(bytes)),
                    Aspect = Aspect.AspectFit,
                    HeightRequest = 240,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 0, 0, 12),
                });
            }

            cuerpo.Add(Seccion("Comprobante"));
            cuerpo.Add(new Label { Text = "Registra la referencia obligatoria. La imagen es opcional y debe ser JPG, PNG o WEBP de hasta 5 MB.", Style = Estilo("Detalle") });
            var referencia = new Entry { Placeholder = "Número o referencia del comprobante", Text = orden.ComprobanteReferencia };
            cuerpo.Add(referencia);
            var archivoTexto = new Label { Text = orden.TieneImagenComprobante ? "Ya hay una imagen adjunta" : "No se adjuntó imagen", Style = Estilo("Detalle"), Margin = new Thickness(0, 4) };
            cuerpo.Add(archivoTexto);
            byte[]? imagen = null;
            string? nombre = null;
            string? tipo = null;

            var adjuntar = new Button { Text = "Adjuntar imagen", Style = Estilo("Secundario"), HorizontalOptions = LayoutOptions.Start };
            adjuntar.Clicked += async (_, _) =>
            {
                var seleccion = await SeleccionarImagenAsync();
                if (seleccion is null) return;
                (imagen, nombre, tipo) = seleccion.Value;
                archivoTexto.Text = $"Imagen seleccionada: {nombre}";
            };
            cuerpo.Add(adjuntar);

            var enviar = new Button { Text = "Registrar comprobante", Margin = new Thickness(0, 8, 0, 0) };
            enviar.Clicked += async (_, _) =>
            {
                if (Ocupado) return;
                var texto = Texto(referencia.Text);
                if (texto is null)
                {
                    await AvisarAsync("Comprobante", "Indica el número o la referencia del comprobante.");
                    return;
                }

                enviar.IsEnabled = false;
                try
                {
                    await IntentarAsync(async () =>
                    {
                        using var stream = imagen is null ? null : new MemoryStream(imagen);
                        await Api.SubirAsync<OrdenPagoQr>($"/api/pagos-qr/{ordenId}/comprobante", stream,
                            nombre ?? "comprobante.jpg", tipo ?? "image/jpeg", ("Referencia", texto));
                        await CargarDetalleAsync();
                    });
                }
                finally
                {
                    enviar.IsEnabled = true;
                }
            };
            cuerpo.Add(enviar);
        }
        else
        {
            cuerpo.Add(Tarjeta("Comprobante", orden.ComprobanteTexto,
                orden.TieneImagenComprobante ? "Imagen adjunta" : "Sin imagen adjunta",
                orden.MotivoRechazo is null ? null : $"Motivo: {orden.MotivoRechazo}"));
        }
    }

    private async Task<(byte[] Contenido, string Nombre, string Tipo)?> SeleccionarImagenAsync()
    {
        try
        {
            var archivo = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Selecciona el comprobante", FileTypes = FilePickerFileType.Images });
            if (archivo is null) return null;
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var tipo = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null,
            };
            if (tipo is null)
            {
                await AvisarAsync("Comprobante", "Formato no permitido. Selecciona una imagen JPG, PNG o WEBP.");
                return null;
            }

            await using var origen = await archivo.OpenReadAsync();
            using var memoria = new MemoryStream();
            await origen.CopyToAsync(memoria);
            if (memoria.Length > MaximoBytes)
            {
                await AvisarAsync("Comprobante", "La imagen supera el límite de 5 MB.");
                return null;
            }
            return (memoria.ToArray(), archivo.FileName, tipo);
        }
        catch (Exception ex) when (ex is not ErrorApi)
        {
            await AvisarAsync("Comprobante", "No se pudo seleccionar la imagen.");
            return null;
        }
    }
}
