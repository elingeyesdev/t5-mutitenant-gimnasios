using System.Buffers.Binary;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;

namespace GestionGimnasios.Web.Tests;

/// <summary>Análisis preliminar del video y su validación por el entrenador (RF-01, CU-35, HU-46).</summary>
public class AnalisisVideoTests
{
    // ── Lectura de MP4 / MOV ─────────────────────────────────────────────

    [Fact]
    public void Lee_duracion_y_resolucion_de_un_mp4_con_moov_al_final()
    {
        var m = LectorMetadatosVideo.Leer(new MemoryStream(Mp4(segundos: 12.5, ancho: 1280, alto: 720)), ".mp4");
        Assert.NotNull(m);
        Assert.Equal(12.5, m.DuracionSeg);
        Assert.Equal(1280, m.Ancho);
        Assert.Equal(720, m.Alto);
    }

    [Fact]
    public void Video_de_celular_grabado_girado_se_lee_vertical()
    {
        var m = LectorMetadatosVideo.Leer(new MemoryStream(Mp4(8, 1920, 1080, girado: true)), ".mov");
        Assert.Equal((1080, 1920), (m!.Ancho, m.Alto));
    }

    [Fact]
    public void Solo_cuenta_la_pista_de_video_y_no_la_de_audio()
    {
        var m = LectorMetadatosVideo.Leer(new MemoryStream(Mp4(10, 640, 480, conAudioPrimero: true)), ".mp4");
        Assert.Equal((640, 480), (m!.Ancho, m.Alto));
    }

    [Fact]
    public void Mp4_sin_moov_no_tiene_datos() =>
        Assert.Null(LectorMetadatosVideo.Leer(new MemoryStream(Caja("ftyp", "isom"u8.ToArray())), ".mp4"));

    // ── Lectura de WebM ──────────────────────────────────────────────────

    [Fact]
    public void Lee_duracion_y_resolucion_de_un_webm()
    {
        var m = LectorMetadatosVideo.Leer(new MemoryStream(Webm(duracionMs: 6400, ancho: 720, alto: 1280)), ".webm");
        Assert.NotNull(m);
        Assert.Equal(6.4, m.DuracionSeg!.Value, 3);
        Assert.Equal((720, 1280), (m.Ancho, m.Alto));
    }

    [Fact]
    public void Webm_grabado_en_el_navegador_puede_no_indicar_la_duracion()
    {
        var m = LectorMetadatosVideo.Leer(new MemoryStream(Webm(null, 640, 360)), ".webm");
        Assert.Null(m!.DuracionSeg);
        Assert.Equal((640, 360), (m.Ancho, m.Alto));
    }

    [Fact]
    public void Archivo_dañado_no_rompe_el_envio()
    {
        var ruta = Path.GetTempFileName();
        File.WriteAllBytes(ruta, [0, 0, 0, 24, .. "ftypmp42"u8.ToArray(), 0xFF, 0xFF, 0xFF, 0xFF, .. "moov"u8.ToArray(), 1, 2, 3]);
        try
        {
            Assert.Equal(ResultadosAnalisisVideo.NoDisponible, AnalizadorVideo.Analizar(ruta, ".mp4").Resultado);
        }
        finally
        {
            File.Delete(ruta);
        }
    }

    // ── Reglas del análisis ──────────────────────────────────────────────

    [Fact]
    public void Video_con_buena_duracion_y_resolucion_es_apto()
    {
        var a = AnalizadorVideo.Evaluar(new MetadatosVideo(25, 1080, 1920));
        Assert.Equal(ResultadosAnalisisVideo.Apto, a.Resultado);
        Assert.Null(a.Texto);
    }

    [Theory]
    [InlineData(2, 1080, 1920, "Dura 2 s")]
    [InlineData(200, 1080, 1920, "Dura 3:20")]
    [InlineData(20, 320, 240, "Resolución baja (320×240)")]
    public void Video_corto_largo_o_de_baja_resolucion_tiene_observaciones(double segundos, int ancho, int alto, string observacion)
    {
        var a = AnalizadorVideo.Evaluar(new MetadatosVideo(segundos, ancho, alto));
        Assert.Equal(ResultadosAnalisisVideo.ConObservaciones, a.Resultado);
        Assert.Contains(observacion, a.Texto);
    }

    [Fact]
    public void Sin_datos_del_archivo_el_analisis_no_esta_disponible()
    {
        Assert.Equal(ResultadosAnalisisVideo.NoDisponible, AnalizadorVideo.Evaluar(null).Resultado);
        Assert.Equal(ResultadosAnalisisVideo.NoDisponible, AnalizadorVideo.Evaluar(new MetadatosVideo(null, null, null)).Resultado);
    }

    [Fact]
    public void Duracion_legible() => Assert.Equal("1:05", AnalizadorVideo.Duracion(65.4));

    // ── Validación humana ────────────────────────────────────────────────

    [Fact]
    public void Con_analisis_el_entrenador_debe_confirmarlo_o_corregirlo() =>
        Assert.Equal(400, Assert.Throws<ErrorNegocio>(() =>
            VideosTecnicaService.ValidarAnalisis(ResultadosAnalisisVideo.Apto, new RevisionVideoForm())).Estado);

    [Fact]
    public void Confirmar_el_analisis_no_guarda_correccion() =>
        Assert.Equal((true, null), VideosTecnicaService.ValidarAnalisis(ResultadosAnalisisVideo.Apto,
            new RevisionVideoForm { AnalisisConfirmado = true, CorreccionAnalisis = "texto que no aplica" }));

    [Fact]
    public void Corregir_el_analisis_exige_explicar_que()
    {
        Assert.Equal(400, Assert.Throws<ErrorNegocio>(() => VideosTecnicaService.ValidarAnalisis(ResultadosAnalisisVideo.ConObservaciones,
            new RevisionVideoForm { AnalisisConfirmado = false, CorreccionAnalisis = "  " })).Estado);
        Assert.Equal((false, "Aunque es corto, se ve la repetición completa"), VideosTecnicaService.ValidarAnalisis(
            ResultadosAnalisisVideo.ConObservaciones,
            new RevisionVideoForm { AnalisisConfirmado = false, CorreccionAnalisis = " Aunque es corto, se ve la repetición completa " }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ResultadosAnalisisVideo.NoDisponible)]
    public void Sin_analisis_no_hay_nada_que_validar(string? resultado) =>
        Assert.Equal((null, null), VideosTecnicaService.ValidarAnalisis(resultado, new RevisionVideoForm()));

    [Fact]
    public void El_socio_no_ve_el_analisis_ni_su_validacion()
    {
        var v = new VideoTecnica
        {
            Estado = EstadosVideoTecnica.Revisado, ResultadoAnalisis = ResultadosAnalisisVideo.ConObservaciones, AnalisisPreliminar = "Dura 2 s",
            AnalisisConfirmado = false, CorreccionAnalisis = "Se ve bien", Valoracion = ValoracionesTecnica.Adecuada, Observacion = "Bien hecho",
            RevisadoEn = DateTime.UtcNow,
        };
        var d = VideosTecnicaService.ADevolucion(v);
        Assert.Equal("Bien hecho", d.Observacion);
        Assert.DoesNotContain(typeof(VideoSocioDto).GetProperties(), p => p.Name.Contains("Analisis") || p.Name.Contains("Correccion"));
    }

    // ── Archivos de prueba armados byte a byte ───────────────────────────

    private static byte[] Caja(string tipo, params byte[][] contenido)
    {
        var cuerpo = contenido.SelectMany(c => c).ToArray();
        var caja = new byte[8 + cuerpo.Length];
        BinaryPrimitives.WriteUInt32BigEndian(caja, (uint)caja.Length);
        System.Text.Encoding.ASCII.GetBytes(tipo).CopyTo(caja, 4);
        cuerpo.CopyTo(caja, 8);
        return caja;
    }

    private static byte[] Mp4(double segundos, int ancho, int alto, bool girado = false, bool conAudioPrimero = false)
    {
        const uint escala = 1000;
        var mvhd = new byte[100];
        BinaryPrimitives.WriteUInt32BigEndian(mvhd.AsSpan(12), escala);
        BinaryPrimitives.WriteUInt32BigEndian(mvhd.AsSpan(16), (uint)(segundos * escala));

        var moov = new List<byte[]> { Caja("mvhd", mvhd) };
        if (conAudioPrimero) moov.Add(Pista("soun", 0, 0, false));
        moov.Add(Pista("vide", ancho, alto, girado));
        return [.. Caja("ftyp", "isom"u8.ToArray()), .. Caja("mdat", new byte[64]), .. Caja("moov", [.. moov])];
    }

    private static byte[] Pista(string manejador, int ancho, int alto, bool girado)
    {
        var tkhd = new byte[84];
        var matriz = tkhd.AsSpan(40);
        BinaryPrimitives.WriteInt32BigEndian(matriz, girado ? 0 : 0x10000);
        BinaryPrimitives.WriteInt32BigEndian(matriz[4..], girado ? 0x10000 : 0);
        BinaryPrimitives.WriteInt32BigEndian(matriz[12..], girado ? -0x10000 : 0);
        BinaryPrimitives.WriteInt32BigEndian(matriz[16..], girado ? 0 : 0x10000);
        BinaryPrimitives.WriteUInt32BigEndian(tkhd.AsSpan(76), (uint)ancho << 16);
        BinaryPrimitives.WriteUInt32BigEndian(tkhd.AsSpan(80), (uint)alto << 16);
        var hdlr = new byte[24];
        System.Text.Encoding.ASCII.GetBytes(manejador).CopyTo(hdlr, 8);
        return Caja("trak", Caja("tkhd", tkhd), Caja("mdia", Caja("hdlr", hdlr)));
    }

    private static byte[] Ebml(uint id, params byte[][] contenido)
    {
        var cuerpo = contenido.SelectMany(c => c).ToArray();
        var bytesId = BitConverter.GetBytes(id).Reverse().SkipWhile(b => b == 0).ToArray();
        // Tamaño en 8 bytes (marcador 0x01): válido para cualquier largo.
        var tamano = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(tamano, (ulong)cuerpo.Length);
        tamano[0] = 0x01;
        return [.. bytesId, .. tamano, .. cuerpo];
    }

    private static byte[] Uint(uint id, ulong valor) =>
        Ebml(id, BitConverter.GetBytes(valor).Reverse().SkipWhile(b => b == 0).DefaultIfEmpty((byte)0).ToArray());

    private static byte[] Webm(double? duracionMs, int ancho, int alto)
    {
        var info = new List<byte[]> { Uint(0x2AD7B1, 1_000_000) };
        if (duracionMs is not null)
        {
            var d = new byte[8];
            BinaryPrimitives.WriteDoubleBigEndian(d, duracionMs.Value);
            info.Add(Ebml(0x4489, d));
        }
        var pista = Ebml(0xAE, Uint(0xD7, 1), Uint(0x83, 1), Ebml(0xE0, Uint(0xB0, (ulong)ancho), Uint(0xBA, (ulong)alto)));
        return
        [
            .. Ebml(0x1A45DFA3, Ebml(0x4282, "webm"u8.ToArray())),
            .. Ebml(0x18538067, Ebml(0x1549A966, [.. info]), Ebml(0x1654AE6B, pista), Ebml(0x1F43B675, new byte[16])),
        ];
    }
}
