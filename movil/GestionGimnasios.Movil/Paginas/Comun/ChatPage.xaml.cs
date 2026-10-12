using System.Collections.ObjectModel;
using GestionGimnasios.Movil.Modelos;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil.Paginas.Comun;

/// <summary>
/// Chat entre socio y asesor. Sin Socket.io/SignalR (no figuran en el PDF), los mensajes nuevos se
/// consultan cada 4 segundos mientras la pantalla está abierta.
/// </summary>
public partial class ChatPage : PaginaBase
{
    private readonly int conversacionId;
    private readonly ObservableCollection<Mensaje> mensajes = [];
    private IDispatcherTimer? sondeo;

    public ChatPage(int conversacionId, string otroUsuario)
    {
        InitializeComponent();
        this.conversacionId = conversacionId;
        Title = otroUsuario;
        Lista.ItemsSource = mensajes;
    }

    /// <summary>Abre (o crea) la conversación con otro usuario y muestra el chat.</summary>
    public static async Task AbrirConAsync(INavigation nav, int usuarioId, string nombre)
    {
        var c = await Sesion.Api.PostAsync<Conversacion>("/api/mensajes/conversaciones", new { usuarioId });
        await nav.PushAsync(new ChatPage(c.Id, nombre));
    }

    protected override async Task CargarAsync()
    {
        await TraerNuevosAsync();
        sondeo ??= Dispatcher.CreateTimer();
        sondeo.Interval = TimeSpan.FromSeconds(4);
        sondeo.Tick -= OnSondeo;
        sondeo.Tick += OnSondeo;
        sondeo.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        sondeo?.Stop();
    }

    private async void OnSondeo(object? sender, EventArgs e)
    {
        try { await TraerNuevosAsync(); }
        catch (ErrorApi) { /* sin conexión momentánea: se reintenta en el siguiente ciclo */ }
    }

    private async Task TraerNuevosAsync()
    {
        var ultimo = mensajes.LastOrDefault()?.Id;
        var ruta = $"/api/mensajes/conversaciones/{conversacionId}" + (ultimo is null ? "" : $"?despuesDe={ultimo}");
        var nuevos = await Api.GetAsync<List<Mensaje>>(ruta);
        foreach (var m in nuevos.Where(n => mensajes.All(x => x.Id != n.Id))) mensajes.Add(m);
        if (nuevos.Any(n => !n.EsMio)) await Api.PutAsync($"/api/mensajes/conversaciones/{conversacionId}/leida");
    }

    private async void OnEnviar(object? sender, EventArgs e)
    {
        var contenido = Entrada.Text?.Trim();
        if (string.IsNullOrEmpty(contenido)) return;
        Entrada.Text = "";
        await IntentarAsync(async () =>
        {
            var m = await Api.PostAsync<Mensaje>($"/api/mensajes/conversaciones/{conversacionId}/mensajes", new { contenido });
            if (mensajes.All(x => x.Id != m.Id)) mensajes.Add(m);
        });
    }
}
