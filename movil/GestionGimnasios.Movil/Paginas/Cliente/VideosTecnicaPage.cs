using System.Globalization;
using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Envío de videos de ejercicios y consulta de la devolución del entrenador (CU-35).</summary>
public class VideosTecnicaPage : PaginaBase
{
    private const long MaximoBytes = 50 * 1024 * 1024;
    private static readonly Dictionary<string, string> TiposVideo = new()
    {
        [".mp4"] = "video/mp4", [".mov"] = "video/quicktime", [".webm"] = "video/webm",
    };

    private readonly VerticalStackLayout cuerpo = new() { Spacing = 0 };
    private readonly ActivityIndicator indicador = new() { IsVisible = false, IsRunning = false, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 12) };
    private readonly RefreshView refresco;

    public VideosTecnicaPage()
    {
        Title = "Revisión de técnica";
        var contenido = new VerticalStackLayout { Padding = 16, Spacing = 0 };
        contenido.Add(indicador);
        contenido.Add(cuerpo);
        refresco = new RefreshView { Content = new ScrollView { Content = contenido } };
        refresco.Refreshing += async (_, _) =>
        {
            await IntentarAsync(CargarAsync);
            refresco.IsRefreshing = false;
        };
        Content = refresco;
    }

    protected override async Task CargarAsync()
    {
        indicador.IsVisible = indicador.IsRunning = true;
        try
        {
            var ejercicios = TextoEjercicio.Ordenar(await Api.GetAsync<List<Ejercicio>>("/api/ejercicios"), null);
            var videos = await Api.GetAsync<List<VideoTecnica>>("/api/videos-tecnica/mios");
            var vistas = new List<View>
            {
                Tarjeta("Revisión de técnica", "Envía un video de un ejercicio para que tu entrenador revise tu técnica.",
                    "La devolución aparecerá cuando el entrenador termine la revisión.")
            };

            vistas.Add(Seccion("Enviar video"));
            if (ejercicios.Count == 0)
                vistas.Add(Vacio("No hay ejercicios disponibles para seleccionar."));

            Ejercicio? ejercicioSeleccionado = null;
            var ejercicioTexto = new Label { Text = "Ningún ejercicio seleccionado", Style = Estilo("Detalle") };
            var comentario = new Editor { Placeholder = "Comentario para tu entrenador (opcional)", AutoSize = EditorAutoSizeOption.TextChanges, MaxLength = 500 };
            var videoTexto = new Label { Text = "No seleccionaste un video", Style = Estilo("Detalle"), Margin = new Thickness(0, 4) };
            byte[]? video = null;
            string? nombre = null;
            string? tipo = null;

            var seleccionarEjercicio = new Button { Text = "Seleccionar ejercicio", Style = Estilo("Secundario"), HorizontalOptions = LayoutOptions.Start };
            seleccionarEjercicio.Clicked += async (_, _) =>
            {
                var seleccionado = await SelectorEjercicioPage.ElegirAsync(Navigation, ejercicios);
                if (seleccionado is null) return;
                ejercicioSeleccionado = seleccionado;
                ejercicioTexto.Text = $"Ejercicio seleccionado: {seleccionado.Nombre}";
            };
            vistas.Add(ejercicioTexto);
            vistas.Add(seleccionarEjercicio);
            vistas.Add(comentario);
            vistas.Add(videoTexto);
            var seleccionar = new Button { Text = "Seleccionar video", Style = Estilo("Secundario"), HorizontalOptions = LayoutOptions.Start };
            seleccionar.Clicked += async (_, _) =>
            {
                var seleccionado = await ElegirVideoAsync(false);
                if (seleccionado is null) return;
                (video, nombre, tipo) = seleccionado.Value;
                videoTexto.Text = $"Video seleccionado: {nombre}";
            };
            vistas.Add(seleccionar);

            var grabar = new Button { Text = "Grabar video", Style = Estilo("Secundario"), HorizontalOptions = LayoutOptions.Start };
            grabar.Clicked += async (_, _) =>
            {
                var seleccionado = await ElegirVideoAsync(true);
                if (seleccionado is null) return;
                (video, nombre, tipo) = seleccionado.Value;
                videoTexto.Text = $"Video grabado: {nombre}";
            };
            vistas.Add(grabar);

            var enviar = new Button { Text = "Enviar video", IsEnabled = ejercicios.Count > 0, Margin = new Thickness(0, 8, 0, 0) };
            enviar.Clicked += async (_, _) =>
            {
                if (Ocupado) return;
                if (ejercicioSeleccionado is null)
                {
                    await AvisarAsync("Video", "Selecciona un ejercicio.");
                    return;
                }
                if (video is null || nombre is null || tipo is null)
                {
                    await AvisarAsync("Video", "Selecciona o graba un video antes de enviarlo.");
                    return;
                }
                var texto = Texto(comentario.Text);
                if (texto?.Length > 500)
                {
                    await AvisarAsync("Video", "El comentario no puede superar los 500 caracteres.");
                    return;
                }

                enviar.IsEnabled = false;
                try
                {
                    if (await IntentarAsync(async () =>
                    {
                        using var contenido = new MemoryStream(video);
                        await Api.SubirAsync<VideoTecnica>("/api/videos-tecnica", contenido, nombre, tipo,
                            ("ejercicioId", ejercicioSeleccionado.Id.ToString(CultureInfo.InvariantCulture)), ("comentario", texto ?? ""));
                        await AvisarAsync("Video enviado", "Tu video quedó pendiente de revisión por tu entrenador.");
                        await CargarAsync();
                    }))
                    {
                        video = null;
                        nombre = null;
                        tipo = null;
                    }
                }
                finally
                {
                    enviar.IsEnabled = ejercicios.Count > 0;
                }
            };
            vistas.Add(enviar);

            vistas.Add(Seccion("Mis videos"));
            if (videos.Count == 0)
                vistas.Add(Vacio("Todavía no has enviado videos de técnica."));
            foreach (var videoEnviado in videos)
            {
                var tarjeta = Tarjeta(videoEnviado.Ejercicio, $"Enviado: {videoEnviado.FechaEnvio}",
                    $"Estado: {videoEnviado.EstadoTexto}", videoEnviado.FechaRevision,
                    string.IsNullOrWhiteSpace(videoEnviado.Comentario) ? null : $"Tu comentario: {videoEnviado.Comentario}");
                var contenidoTarjeta = (VerticalStackLayout)tarjeta.Content!;
                if (videoEnviado.Estado == "REVISADO")
                {
                    contenidoTarjeta.Add(new Label { Text = $"Valoración: {videoEnviado.ValoracionTexto}", TextColor = Colores.Exito, FontFamily = "OpenSansSemibold", FontSize = 13 });
                    contenidoTarjeta.Add(new Label { Text = $"Observación: {videoEnviado.Observacion}", Style = Estilo("Detalle") });
                }
                else
                {
                    contenidoTarjeta.Add(new Label { Text = "La valoración aparecerá después de la revisión del entrenador.", Style = Estilo("Detalle") });
                }
                vistas.Add(tarjeta);
            }

            cuerpo.Clear();
            foreach (var vista in vistas) cuerpo.Add(vista);
        }
        finally
        {
            indicador.IsVisible = indicador.IsRunning = false;
        }
    }

    private async Task<(byte[] Contenido, string Nombre, string Tipo)?> ElegirVideoAsync(bool grabar)
    {
        try
        {
            if (grabar)
            {
                var permisoCamara = await Permissions.RequestAsync<Permissions.Camera>();
                var permisoMicrofono = await Permissions.RequestAsync<Permissions.Microphone>();
                if (permisoCamara != PermissionStatus.Granted || permisoMicrofono != PermissionStatus.Granted)
                {
                    await AvisarAsync("Permisos", "Necesitas permitir cámara y micrófono para grabar un video.");
                    return null;
                }
            }

            FileResult? archivo;
            if (grabar)
            {
                archivo = await MediaPicker.Default.CaptureVideoAsync(new MediaPickerOptions { Title = "Graba el ejercicio" });
            }
            else
            {
                var archivos = await MediaPicker.Default.PickVideosAsync(new MediaPickerOptions { Title = "Selecciona el video del ejercicio" });
                archivo = archivos.Count == 0 ? null : archivos[0];
            }
            if (archivo is null) return null;

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!TiposVideo.TryGetValue(extension, out var tipo))
            {
                await AvisarAsync("Video", "Formato no permitido. Usa MP4, MOV o WEBM.");
                return null;
            }

            await using var origen = await archivo.OpenReadAsync();
            using var memoria = new MemoryStream();
            var buffer = new byte[81920];
            int leidos;
            while ((leidos = await origen.ReadAsync(buffer)) > 0)
            {
                if (memoria.Length + leidos > MaximoBytes)
                {
                    await AvisarAsync("Video", "El video supera el límite de 50 MB.");
                    return null;
                }
                memoria.Write(buffer, 0, leidos);
            }
            return (memoria.ToArray(), archivo.FileName, tipo);
        }
        catch (Exception ex) when (ex is not ErrorApi)
        {
            await AvisarAsync("Video", "No se pudo seleccionar o grabar el video.");
            return null;
        }
    }
}
