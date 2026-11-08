using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;

namespace GestionGimnasios.Web.Tests;

/// <summary>Reglas de la revisión de técnica por video (RF-01, HU-46).</summary>
public class VideosTecnicaTests
{
    private static readonly byte[] Mp4 = [0, 0, 0, 24, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'m', (byte)'p', (byte)'4', (byte)'2'];
    private static readonly byte[] Webm = [0x1A, 0x45, 0xDF, 0xA3, 0, 0, 0, 0, 0, 0, 0, 0];

    private static VideoTecnica Video(string estado) => new()
    {
        Estado = estado, AnalisisPreliminar = "Texto interno", Valoracion = ValoracionesTecnica.Mejorable,
        Observacion = "Baja más la cadera", RevisadoEn = DateTime.UtcNow,
    };

    [Fact]
    public void Socio_no_recibe_la_valoracion_antes_de_la_revision()
    {
        var d = VideosTecnicaService.ADevolucion(Video(EstadosVideoTecnica.Pendiente));
        Assert.Null(d.Valoracion);
        Assert.Null(d.Observacion);
        Assert.Null(d.RevisadoEn);
    }

    [Fact]
    public void Socio_recibe_la_devolucion_aprobada()
    {
        var d = VideosTecnicaService.ADevolucion(Video(EstadosVideoTecnica.Revisado));
        Assert.Equal(ValoracionesTecnica.Mejorable, d.Valoracion);
        Assert.Equal("Baja más la cadera", d.Observacion);
    }

    [Fact]
    public void Lo_que_ve_el_socio_no_incluye_el_analisis_preliminar() =>
        Assert.DoesNotContain(typeof(VideoSocioDto).GetProperties(), p => p.Name.Contains("Analisis"));

    [Theory]
    [InlineData(".mp4", true)]
    [InlineData(".mov", true)]
    [InlineData(".webm", false)]
    public void Firma_de_mp4_y_mov(string extension, bool valida) =>
        Assert.Equal(valida, VideosTecnicaService.FirmaValida(extension, Mp4));

    [Fact]
    public void Firma_de_webm() => Assert.True(VideosTecnicaService.FirmaValida(".webm", Webm));

    [Fact]
    public void Archivo_con_extension_de_video_pero_otro_contenido_se_rechaza() =>
        Assert.False(VideosTecnicaService.FirmaValida(".mp4", "MZ este es un programa"u8));

    [Fact]
    public void Archivo_muy_corto_se_rechaza() => Assert.False(VideosTecnicaService.FirmaValida(".mp4", new byte[4]));

    [Theory]
    [InlineData("", "Bien")]
    [InlineData("OTRA", "Bien")]
    [InlineData(ValoracionesTecnica.Adecuada, "   ")]
    public void Revision_incompleta_es_error_400(string valoracion, string observacion) =>
        Assert.Equal(400, Assert.Throws<ErrorNegocio>(() =>
            VideosTecnicaService.ValidarRevision(new RevisionVideoForm { Valoracion = valoracion, Observacion = observacion })).Estado);

    [Fact]
    public void Comentario_muy_largo_es_error_400() =>
        Assert.Equal(400, Assert.Throws<ErrorNegocio>(() => VideosTecnicaService.ValidarComentario(new string('a', 501))).Estado);

    [Fact]
    public void Comentario_en_blanco_queda_vacio() => Assert.Null(VideosTecnicaService.ValidarComentario("   "));
}
