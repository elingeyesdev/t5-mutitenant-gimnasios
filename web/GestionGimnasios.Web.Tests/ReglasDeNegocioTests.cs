using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;

namespace GestionGimnasios.Web.Tests;

/// <summary>Reglas de negocio puras (sin base de datos) migradas del proyecto base.</summary>
public class ReglasDeNegocioTests
{
    [Theory]
    [InlineData(null, 7)]
    [InlineData(0, 7)]
    [InlineData(1, 8)]
    [InlineData(3, 3)]
    [InlineData(7, 2)]
    public void Umbral_de_inactividad_depende_de_la_meta_semanal(int? meta, int dias) =>
        Assert.Equal(dias, PersonalService.UmbralInactividad(meta));

    [Theory]
    [InlineData(70, 175, 22.9, "Peso normal")]
    [InlineData(50, 180, 15.4, "Bajo peso")]
    [InlineData(80, 170, 27.7, "Sobrepeso")]
    [InlineData(90, 170, 31.1, "Obesidad")]
    public void Imc_se_calcula_y_clasifica(double peso, double altura, double imc, string categoria)
    {
        var r = EntrenamientoService.Imc((decimal)peso, (decimal)altura);
        Assert.Equal((decimal)imc, r.Imc);
        Assert.Equal(categoria, r.Categoria);
    }

    [Fact]
    public void Imc_sin_datos_no_se_calcula() => Assert.Null(EntrenamientoService.Imc(null, 170).Imc);

    [Fact]
    public void Distancia_entre_Santa_Cruz_y_La_Paz_es_de_unos_550_km()
    {
        Assert.Equal(0, SedesService.DistanciaKm(-17.7833, -63.1821, -17.7833, -63.1821), 6);
        Assert.InRange(SedesService.DistanciaKm(-17.7833, -63.1821, -16.4955, -68.1336), 530, 560);
    }

    [Theory]
    [InlineData("miércoles", "MIERCOLES")]
    [InlineData(" Sábado ", "SABADO")]
    [InlineData("lun", "LUNES")]
    [InlineData("DOMINGO", "DOMINGO")]
    public void Dia_de_clase_se_normaliza(string dia, string esperado) =>
        Assert.Equal(esperado, ActividadesService.NormalizarDia(dia));

    [Fact]
    public void Dia_de_clase_no_valido_es_error_400()
    {
        var e = Assert.Throws<ErrorNegocio>(() => ActividadesService.NormalizarDia("feriado"));
        Assert.Equal(400, e.Estado);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, "LUNES")]
    [InlineData(DayOfWeek.Sunday, "DOMINGO")]
    public void Semana_empieza_el_lunes(DayOfWeek dia, string esperado) => Assert.Equal(esperado, Dias.Largo(dia));

    [Fact]
    public void Contrasena_se_guarda_con_hash_y_se_verifica()
    {
        var u = new Usuario { PasswordHash = ServicioAutenticacion.Hashear("Aaron123*") };
        Assert.NotEqual("Aaron123*", u.PasswordHash);
        Assert.True(ServicioAutenticacion.Verificar(u, "Aaron123*"));
        Assert.False(ServicioAutenticacion.Verificar(u, "otra"));
    }

    [Fact]
    public void Sucursal_abierta_todo_el_dia_esta_abierta_y_en_feriado_cerrada()
    {
        HorarioSucursal Dia(string d, bool feriado) => new() { DiaSemana = d, Abre = TimeOnly.MinValue, Cierra = TimeOnly.MaxValue, EsFeriado = feriado };
        Assert.True(SedesService.AbiertaAhora(Dias.Cortos.Select(d => Dia(d, false))));
        Assert.False(SedesService.AbiertaAhora(Dias.Cortos.Select(d => Dia(d, true))));
        Assert.False(SedesService.AbiertaAhora([]));
    }
}
