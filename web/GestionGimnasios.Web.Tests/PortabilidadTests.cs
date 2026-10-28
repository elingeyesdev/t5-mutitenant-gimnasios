using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;

namespace GestionGimnasios.Web.Tests;

/// <summary>Reglas del consentimiento del perfil portable (RF-02, HU-47).</summary>
public class PortabilidadTests
{
    [Fact]
    public void Datos_se_normalizan_sin_repetidos_y_en_orden_fijo()
    {
        var datos = PortabilidadService.ValidarDatos([" metricas", "PERFIL", "perfil"]);
        Assert.Equal([DatosPortables.Perfil, DatosPortables.Metricas], datos);
    }

    [Fact]
    public void Consentimiento_sin_datos_es_error_400()
    {
        var e = Assert.Throws<ErrorNegocio>(() => PortabilidadService.ValidarDatos([]));
        Assert.Equal(400, e.Estado);
    }

    [Fact]
    public void Dato_no_permitido_es_error_400()
    {
        var e = Assert.Throws<ErrorNegocio>(() => PortabilidadService.ValidarDatos(["PERFIL", "CONTRASENA"]));
        Assert.Equal(400, e.Estado);
        Assert.Contains("CONTRASENA", e.Message);
    }

    [Fact]
    public void Consentimiento_revocado_deja_de_estar_vigente()
    {
        var vigente = new ConsentimientoDto(1, 1, "Socio", 2, "Cadena", [DatosPortables.Perfil], DateTime.UtcNow, null, 0, null);
        Assert.True(vigente.Vigente);
        Assert.False((vigente with { RevocadoEn = DateTime.UtcNow }).Vigente);
    }
}
