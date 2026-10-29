using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;

namespace GestionGimnasios.Web.Tests;

/// <summary>Criterios de detección de inactividad (RF-04, HU-49).</summary>
public class AlertasTests
{
    [Theory]
    [InlineData(0, null)]
    [InlineData(6, null)]
    [InlineData(7, NivelesAlerta.Medio)]
    [InlineData(13, NivelesAlerta.Medio)]
    [InlineData(14, NivelesAlerta.Alto)]
    [InlineData(60, NivelesAlerta.Alto)]
    public void Nivel_depende_de_los_umbrales_de_la_cadena(int dias, string? nivel) =>
        Assert.Equal(nivel, AlertasService.Nivel(dias, diasAviso: 7, diasRiesgo: 14));

    [Fact]
    public void Otra_cadena_con_otros_umbrales_da_otro_nivel()
    {
        Assert.Null(AlertasService.Nivel(10, diasAviso: 15, diasRiesgo: 30));
        Assert.Equal(NivelesAlerta.Alto, AlertasService.Nivel(10, diasAviso: 3, diasRiesgo: 10));
    }

    [Fact]
    public void Dias_sin_actividad_se_cuentan_en_hora_local()
    {
        // 29/10 a las 02:00 UTC todavía es 28/10 en Bolivia (UTC-4).
        var ultima = new DateTime(2026, 10, 29, 2, 0, 0, DateTimeKind.Utc);
        Assert.Equal(0, AlertasService.DiasSinActividad(ultima, new DateOnly(2026, 10, 28)));
        Assert.Equal(7, AlertasService.DiasSinActividad(ultima, new DateOnly(2026, 11, 4)));
    }

    [Theory]
    [InlineData(0, 14)]
    [InlineData(7, 7)]
    [InlineData(10, 5)]
    [InlineData(7, 400)]
    public void Umbrales_invalidos_son_error_400(int aviso, int riesgo)
    {
        var e = Assert.Throws<ErrorNegocio>(() => AlertasService.ValidarRegla(new ReglaInactividadForm { DiasAviso = aviso, DiasRiesgo = riesgo }));
        Assert.Equal(400, e.Estado);
    }

    [Fact]
    public void Umbrales_por_defecto_son_validos() => AlertasService.ValidarRegla(new ReglaInactividadForm());
}
