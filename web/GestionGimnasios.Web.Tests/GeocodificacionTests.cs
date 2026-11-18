using System.Diagnostics;
using System.Net;
using System.Text.Json;
using GestionGimnasios.Web.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace GestionGimnasios.Web.Tests;

/// <summary>La búsqueda de direcciones respeta la política de uso de Nominatim sin salir a internet.</summary>
public class GeocodificacionTests
{
    private const string Lugar = """[{"lat":"-17.7654321","lon":"-63.1956789","display_name":"Av. Busch"}]""";

    /// <summary>Servicio de direcciones simulado: cuenta las consultas y guarda la última.</summary>
    private sealed class ServicioFalso(HttpStatusCode estado, string cuerpo) : HttpMessageHandler
    {
        public int Consultas;
        public HttpRequestMessage? Ultima;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Consultas);
            Ultima = request;
            return Task.FromResult(new HttpResponseMessage(estado) { Content = new StringContent(cuerpo) });
        }
    }

    private static GeocodificacionService Crear(ServicioFalso falso, OpcionesGeocodificacion? opciones = null, TurnoGeocodificacion? turno = null)
    {
        opciones ??= new OpcionesGeocodificacion();
        var http = new HttpClient(falso);
        GeocodificacionService.Configurar(http, opciones);
        return new GeocodificacionService(http, new MemoryCache(new MemoryCacheOptions()),
            turno ?? new TurnoGeocodificacion(TimeSpan.Zero, TimeSpan.FromSeconds(1)), opciones,
            NullLogger<GeocodificacionService>.Instance);
    }

    [Fact]
    public async Task El_turno_deja_al_menos_un_intervalo_entre_consultas_de_toda_la_aplicacion()
    {
        var turno = new TurnoGeocodificacion(TimeSpan.FromMilliseconds(300), TimeSpan.FromSeconds(5));
        var reloj = Stopwatch.StartNew();
        var turnos = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => turno.EsperarAsync()));
        Assert.All(turnos, Assert.True);
        Assert.True(reloj.ElapsedMilliseconds >= 550, $"Tres consultas tardaron solo {reloj.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task Si_la_cola_es_demasiado_larga_se_responde_no_disponible_en_vez_de_esperar()
    {
        var turno = new TurnoGeocodificacion(TimeSpan.FromMilliseconds(800), TimeSpan.FromMilliseconds(100));
        var turnos = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => turno.EsperarAsync()));
        Assert.Contains(false, turnos);
    }

    [Fact]
    public async Task La_misma_busqueda_se_responde_desde_la_cache_sin_volver_a_consultar()
    {
        var falso = new ServicioFalso(HttpStatusCode.OK, Lugar);
        var geo = Crear(falso);
        var primera = await geo.BuscarAsync("Av. Busch 300", "Santa Cruz");
        var segunda = await geo.BuscarAsync("  av. busch 300 ", "santa cruz");
        Assert.Equal(1, falso.Consultas);
        Assert.Equal(new LugarEncontrado(-17.7654321, -63.1956789), primera.Valor);
        Assert.Equal(primera, segunda);
    }

    [Fact]
    public async Task Cada_consulta_se_identifica_con_el_user_agent_de_la_aplicacion()
    {
        var falso = new ServicioFalso(HttpStatusCode.OK, Lugar);
        await Crear(falso).BuscarAsync("Av. Busch 300", null);
        Assert.Contains("GestionGimnasios", falso.Ultima!.Headers.UserAgent.ToString());
        Assert.StartsWith("https://nominatim.openstreetmap.org/search?", falso.Ultima.RequestUri!.ToString());
        Assert.Contains("countrycodes=bo", falso.Ultima.RequestUri.Query);
    }

    [Fact]
    public async Task Si_el_servicio_pide_esperar_se_pausa_y_no_se_le_vuelve_a_consultar()
    {
        var falso = new ServicioFalso(HttpStatusCode.TooManyRequests, "");
        var geo = Crear(falso);
        Assert.False((await geo.BuscarAsync("Av. Busch 300", null)).Disponible);
        Assert.False((await geo.BuscarAsync("Av. Cristo Redentor", null)).Disponible);
        Assert.Equal(1, falso.Consultas);
    }

    [Fact]
    public async Task Desactivada_por_configuracion_no_consulta_ningun_servicio()
    {
        var falso = new ServicioFalso(HttpStatusCode.OK, Lugar);
        var geo = Crear(falso, new OpcionesGeocodificacion { Habilitada = false });
        Assert.False((await geo.BuscarAsync("Av. Busch 300", null)).Disponible);
        Assert.False((await geo.DireccionAsync(-17.76, -63.19)).Disponible);
        Assert.Equal(0, falso.Consultas);
    }

    [Fact]
    public async Task El_proveedor_se_cambia_solo_con_la_configuracion()
    {
        var falso = new ServicioFalso(HttpStatusCode.OK, Lugar);
        await Crear(falso, new OpcionesGeocodificacion { Url = "https://geo.ejemplo.bo/" }).BuscarAsync("Av. Busch 300", null);
        Assert.Equal("geo.ejemplo.bo", falso.Ultima!.RequestUri!.Host);
    }

    [Fact]
    public async Task Textos_demasiado_cortos_no_se_buscan()
    {
        var falso = new ServicioFalso(HttpStatusCode.OK, Lugar);
        var r = await Crear(falso).BuscarAsync("ab", null);
        Assert.True(r.Disponible);
        Assert.Null(r.Valor);
        Assert.Equal(0, falso.Consultas);
    }

    [Fact]
    public async Task Clics_muy_cercanos_reutilizan_la_misma_direccion()
    {
        var falso = new ServicioFalso(HttpStatusCode.OK,
            """{"address":{"road":"Avenida Busch","house_number":"300","neighbourhood":"Equipetrol","city":"Santa Cruz de la Sierra"}}""");
        var geo = Crear(falso);
        var r = await geo.DireccionAsync(-17.765432, -63.195678);
        await geo.DireccionAsync(-17.765441, -63.195671);
        Assert.Equal(1, falso.Consultas);
        Assert.Equal(new DireccionSugerida("Avenida Busch 300, Equipetrol", "Santa Cruz de la Sierra"), r.Valor);
    }

    [Fact]
    public void Una_respuesta_sin_resultados_no_inventa_un_lugar()
    {
        Assert.Null(GeocodificacionService.LeerLugar(JsonDocument.Parse("[]").RootElement));
        Assert.Null(GeocodificacionService.LeerDireccion(JsonDocument.Parse("""{"error":"Unable to geocode"}""").RootElement));
    }
}
