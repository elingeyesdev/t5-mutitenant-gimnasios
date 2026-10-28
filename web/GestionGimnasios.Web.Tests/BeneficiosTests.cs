using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;

namespace GestionGimnasios.Web.Tests;

/// <summary>Reglas de canje de beneficios (RF-03, HU-48).</summary>
public class BeneficiosTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 28);

    private static Beneficio Cupon() => new()
    {
        Nombre = "Bebida gratis", VigenteDesde = Hoy.AddDays(-5), VigenteHasta = Hoy.AddDays(5),
        LimitePorSocio = 1, CuposTotales = 10, AsistenciasMinimas = 4, RequierePlanActivo = true,
    };

    private static ContextoCanje Socio(bool plan = true, int asistencias = 6, int mios = 0, int totales = 3, DateOnly? hoy = null) =>
        new(hoy ?? Hoy, plan, asistencias, mios, totales);

    [Fact]
    public void Socio_que_cumple_todo_puede_canjear() => Assert.Null(BeneficiosService.MotivoNoElegible(Cupon(), Socio()));

    [Fact]
    public void Cupon_ya_usado_no_se_duplica() =>
        Assert.Equal("Ya canjeaste este beneficio.", BeneficiosService.MotivoNoElegible(Cupon(), Socio(mios: 1)));

    [Fact]
    public void Con_varios_canjes_por_socio_se_respeta_el_limite()
    {
        var b = Cupon();
        b.LimitePorSocio = 3;
        Assert.Null(BeneficiosService.MotivoNoElegible(b, Socio(mios: 2)));
        Assert.Equal("Ya usaste los 3 canjes permitidos.", BeneficiosService.MotivoNoElegible(b, Socio(mios: 3)));
    }

    [Fact]
    public void Cupon_agotado_se_rechaza() =>
        Assert.Equal("El beneficio está agotado.", BeneficiosService.MotivoNoElegible(Cupon(), Socio(totales: 10)));

    [Fact]
    public void Cupon_vencido_o_futuro_se_rechaza()
    {
        Assert.Equal("El beneficio está vencido.", BeneficiosService.MotivoNoElegible(Cupon(), Socio(hoy: Hoy.AddDays(6))));
        Assert.StartsWith("Disponible desde", BeneficiosService.MotivoNoElegible(Cupon(), Socio(hoy: Hoy.AddDays(-6))));
    }

    [Fact]
    public void Sin_plan_activo_o_sin_asistencias_no_es_elegible()
    {
        Assert.Equal("Necesitas un plan activo en la cadena.", BeneficiosService.MotivoNoElegible(Cupon(), Socio(plan: false)));
        Assert.Contains("llevas 2", BeneficiosService.MotivoNoElegible(Cupon(), Socio(asistencias: 2)));
    }

    [Fact]
    public void Beneficio_desactivado_no_se_canjea()
    {
        var b = Cupon();
        b.Activo = false;
        Assert.Equal("El beneficio ya no está disponible.", BeneficiosService.MotivoNoElegible(b, Socio()));
    }

    [Fact]
    public void Reglas_muestran_vigencia_y_condiciones()
    {
        var reglas = BeneficiosService.Reglas(Cupon());
        Assert.Equal("Válido del 23/10/2026 al 02/11/2026", reglas[0]);
        Assert.Contains("Requiere un plan activo en la cadena", reglas);
        Assert.Contains("Mínimo 4 ingresos en los últimos 30 días", reglas);
        Assert.Contains("Un canje por socio", reglas);
        Assert.Contains("10 canjes disponibles en total", reglas);
    }

    [Theory]
    [InlineData("", "DESCUENTO", 0, 1, null)]
    [InlineData("Vale", "REGALO", 0, 1, null)]
    [InlineData("Vale", "VALE", -1, 1, null)]
    [InlineData("Vale", "VALE", 0, 0, null)]
    [InlineData("Vale", "VALE", 0, 1, 0)]
    public void Formulario_invalido_es_error_400(string nombre, string tipo, int diasVigencia, int limite, int? cupos)
    {
        var f = new BeneficioForm
        {
            Nombre = nombre, Tipo = tipo, VigenteDesde = Hoy, VigenteHasta = Hoy.AddDays(diasVigencia), LimitePorSocio = limite, CuposTotales = cupos,
        };
        Assert.Equal(400, Assert.Throws<ErrorNegocio>(() => BeneficiosService.Validar(f)).Estado);
    }
}
