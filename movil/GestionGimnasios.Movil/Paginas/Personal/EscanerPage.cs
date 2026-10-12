using System.Net;
using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Personal;

/// <summary>
/// Control de acceso en recepción (CU-14, CU-15). Reemplaza el escáner de QR del original: se escribe
/// el número de socio del carnet (ingreso/salida) o el código de 8 caracteres de una reserva.
/// </summary>
public class EscanerPage : PaginaBase
{
    private readonly Entry codigo = new()
    {
        Placeholder = "Nº de socio o código",
        FontSize = 22,
        HorizontalTextAlignment = TextAlignment.Center,
        Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeCharacter),
        ReturnType = ReturnType.Search,
    };
    private readonly VerticalStackLayout resultado = new();
    private readonly VerticalStackLayout recientes = new();

    public EscanerPage()
    {
        Title = "Control de acceso";
        var verificar = new Button { Text = "Verificar" };
        verificar.Clicked += async (_, _) => await VerificarAsync();
        codigo.Completed += async (_, _) => await VerificarAsync();
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 8,
                Children =
                {
                    new Label { Text = Sesion.Usuario?.Lugar, Style = Estilo("Detalle") },
                    new Border
                    {
                        Style = Estilo("Tarjeta"),
                        Content = new VerticalStackLayout
                        {
                            Spacing = 8,
                            Children =
                            {
                                new Label { Text = "Carnet: número de socio (solo dígitos). Reserva: código de 8 letras y números.", Style = Estilo("Detalle") },
                                codigo,
                                verificar,
                            },
                        },
                    },
                    resultado,
                    Seccion("Últimos movimientos en tu sede"),
                    recientes,
                },
            },
        };
    }

    protected override async Task CargarAsync()
    {
        var pagina = await Api.GetAsync<PaginaApi<Acceso>>("/api/accesos?limite=8");
        recientes.Clear();
        if (pagina.Datos.Count == 0) recientes.Add(Vacio("Sin movimientos todavía."));
        foreach (var a in pagina.Datos) recientes.Add(Tarjeta($"{a.Usuario} · {Textos.Rol(a.Rol)}", a.Texto, a.EstadoTexto));
    }

    private async Task VerificarAsync()
    {
        var texto = codigo.Text?.Trim().ToUpperInvariant() ?? "";
        if (texto.Length == 0) return;
        resultado.Clear();
        if (texto.All(char.IsDigit)) await CarnetAsync(texto);
        else await ReservaAsync(texto, forzar: false);
    }

    private async Task CarnetAsync(string numero)
    {
        VistaPreviaAcceso? v = null;
        if (!await IntentarAsync(async () => v = await Api.PostAsync<VistaPreviaAcceso>("/api/accesos/vista-previa", new { codigo = numero }))) return;
        var tarjeta = Tarjeta(v!.NombreCompleto, $"{Textos.Rol(v.Rol)} · {v.Sucursal}", v.Estado);
        resultado.Add(ConBoton(tarjeta, v.Accion, async () =>
        {
            Acceso? a = null;
            if (await IntentarAsync(async () => a = await Api.PostAsync<Acceso>("/api/accesos/registrar", new { codigo = numero })))
            {
                resultado.Clear();
                resultado.Add(Exito(a!.Salida is null ? $"Ingreso registrado: {a.Usuario}" : $"Salida registrada: {a.Usuario} ({a.MinutosDentro} min)"));
                codigo.Text = "";
                await IntentarAsync(CargarAsync);
            }
        }, null));
    }

    private async Task ReservaAsync(string texto, bool forzar)
    {
        try
        {
            Ocupado = true;
            var r = await Api.PostAsync<ResultadoIngreso>("/api/reservas/ingreso", new { codigo = texto, forzar });
            resultado.Add(Exito(r.Mensaje));
            resultado.Add(Tarjeta(r.Reserva.Usuario, r.Reserva.Actividad, r.Reserva.Cuando, r.Reserva.Sucursal));
            codigo.Text = "";
        }
        catch (ErrorApi e) when (e.Estado == HttpStatusCode.Conflict && !forzar)
        {
            // La reserva es de otro día: recepción decide si igual registra la asistencia.
            Ocupado = false;
            if (await ConfirmarAsync("Reserva de otra fecha", e.Message + "\n\n¿Registrar el ingreso de todos modos?", "Registrar"))
                await ReservaAsync(texto, forzar: true);
        }
        catch (ErrorApi e) when (e.Estado != HttpStatusCode.Unauthorized)
        {
            await AvisarAsync("Código no válido", e.Message);
        }
        finally
        {
            Ocupado = false;
        }
    }

    private static Border Exito(string texto) => new()
    {
        Style = Estilo("Tarjeta"),
        BackgroundColor = Color.FromArgb("#EBF7EE"),
        Stroke = Colores.Exito,
        Content = new Label { Text = "✓ " + texto, TextColor = Colores.Exito, FontFamily = "OpenSansSemibold", FontSize = 16 },
    };
}
