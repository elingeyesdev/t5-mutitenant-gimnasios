using GestionGimnasios.Web.Controllers;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;

namespace GestionGimnasios.Web.Tests;

/// <summary>Datos incompletos o imposibles que los formularios ya no aceptan.</summary>
public class ValidacionesFormulariosTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_texto_obligatorio_no_puede_quedar_vacio_ni_con_solo_espacios(string? valor)
    {
        var e = Assert.Throws<ErrorNegocio>(() => Reglas.Obligatorio(valor, "La dirección"));
        Assert.Equal("Completa la dirección.", e.Message);
    }

    [Fact]
    public void Un_texto_obligatorio_se_guarda_sin_espacios_sobrantes() =>
        Assert.Equal("Av. Busch 300", Reglas.Obligatorio("  Av. Busch 300 ", "La dirección"));

    [Fact]
    public void Un_texto_demasiado_largo_se_rechaza_antes_de_llegar_a_la_base() =>
        Assert.Throws<ErrorNegocio>(() => Reglas.Obligatorio(new string('a', 151), "El nombre", 150));

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("77-abc-123")]
    public void Un_telefono_con_letras_o_muy_corto_se_rechaza(string telefono) =>
        Assert.Throws<ErrorNegocio>(() => Reglas.Telefono(telefono));

    [Theory]
    [InlineData("71234567")]
    [InlineData("+591 712 34567")]
    [InlineData("3-3456789")]
    public void Un_telefono_boliviano_se_acepta(string telefono) => Assert.Equal(telefono, Reglas.Telefono(telefono));

    [Fact]
    public void Una_fecha_de_nacimiento_futura_se_rechaza() =>
        Assert.Throws<ErrorNegocio>(() => Reglas.FechaNacimiento(Reloj.Hoy.AddDays(1)));

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=RCxikBS5Qy8&t=3s")]
    [InlineData("https://youtu.be/RCxikBS5Qy8")]
    [InlineData("https://www.youtube.com/shorts/RCxikBS5Qy8")]
    [InlineData("RCxikBS5Qy8")]
    public void Del_enlace_de_youtube_se_guarda_solo_el_identificador(string enlace) =>
        Assert.Equal("RCxikBS5Qy8", EjerciciosService.VideoDeYoutube(enlace));

    [Fact]
    public void Un_video_de_youtube_que_no_es_enlace_ni_identificador_se_rechaza() =>
        Assert.Throws<ErrorNegocio>(() => EjerciciosService.VideoDeYoutube("esto no es un video"));

    [Fact]
    public void Las_filas_vacias_de_la_rutina_se_ignoran_pero_las_que_tienen_datos_se_conservan()
    {
        var filas = RutinasController.FilasConDatos(
        [
            new RutinaEjercicioForm { EjercicioId = 26, Series = 3, Repeticiones = "10" },
            new RutinaEjercicioForm(),
            new RutinaEjercicioForm { Series = 4, Repeticiones = "12" },
        ]);
        Assert.Equal(2, filas.Count);
        Assert.Equal(0, filas[1].EjercicioId); // la validación le pedirá elegir el ejercicio
    }

    [Fact]
    public void Una_serie_con_repeticiones_o_peso_negativos_se_rechaza()
    {
        Assert.Throws<ErrorNegocio>(() => EntrenamientoService.ValidarSerie(new SerieForm { NumeroSerie = 1, Repeticiones = -10 }));
        Assert.Throws<ErrorNegocio>(() => EntrenamientoService.ValidarSerie(new SerieForm { NumeroSerie = 1, PesoKg = -50 }));
    }

    [Fact]
    public void Una_medida_corporal_imposible_se_rechaza()
    {
        Assert.Throws<ErrorNegocio>(() => EntrenamientoService.ValidarMetrica(new MetricaForm { PesoKg = -70 }));
        Assert.Throws<ErrorNegocio>(() => EntrenamientoService.ValidarMetrica(new MetricaForm { GrasaPct = 150 }));
        EntrenamientoService.ValidarMetrica(new MetricaForm { PesoKg = 72.5m, CinturaCm = 80 });
    }

    [Fact]
    public void Un_plan_nutricional_sin_ningun_contenido_esta_vacio()
    {
        Assert.True(PersonalService.PlanVacio(new PlanNutricional { PlanComidas = "{}" }));
        Assert.False(PersonalService.PlanVacio(new PlanNutricional { PautaAntesEntrenar = "Fruta antes de entrenar" }));
        Assert.False(PersonalService.PlanVacio(new PlanNutricional { KcalDiarias = 2200 }));
    }
}
