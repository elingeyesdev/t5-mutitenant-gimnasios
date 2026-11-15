using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;

namespace GestionGimnasios.Web.Tests;

/// <summary>Simulaciones de ejercicios con el plan nutricional (RF-06, CU-40, HU-51).</summary>
public class NutricionTests
{
    private static PlanNutricionalDto Plan(int? kcal = null, string? antes = null, string? despues = null, string? agua = null) =>
        new(1, 7, "Lucía Rojas", 9, "Pedro Sanz", kcal, kcal is null ? null : 120m, kcal is null ? null : 250m, kcal is null ? null : 60m,
            "Notas generales", [], new DateTime(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc), antes, despues, agua);

    [Fact]
    public void Sin_plan_asignado_no_hay_pautas() => Assert.Null(SimulacionesService.Pautas(null));

    [Fact]
    public void Plan_sin_objetivo_ni_pautas_no_muestra_nada() => Assert.Null(SimulacionesService.Pautas(Plan()));

    [Fact]
    public void Las_pautas_son_exactamente_las_que_escribio_el_profesional()
    {
        var p = SimulacionesService.Pautas(Plan(2200, "Fruta una hora antes", null, "500 ml durante la sesión"))!;
        Assert.Equal("Lucía Rojas", p.Profesional);
        Assert.Equal(2200, p.KcalDiarias);
        Assert.Equal("Fruta una hora antes", p.AntesDeEntrenar);
        Assert.Null(p.DespuesDeEntrenar);
        Assert.Equal("500 ml durante la sesión", p.Hidratacion);
    }

    [Fact]
    public void Una_sola_pauta_ya_se_muestra() =>
        Assert.Equal("Proteína después", SimulacionesService.Pautas(Plan(despues: "Proteína después"))!.DespuesDeEntrenar);

    [Theory]
    [InlineData("RCxikBS5Qy8", "/img/press.png", TiposSimulacion.Video)]
    [InlineData(null, "/img/press.png", TiposSimulacion.Imagen)]
    [InlineData(" ", null, TiposSimulacion.NoDisponible)]
    public void Tipo_de_simulacion_segun_el_contenido_del_ejercicio(string? youtube, string? imagen, string tipo) =>
        Assert.Equal(tipo, SimulacionesService.TipoDe(youtube, imagen));

    [Fact]
    public void Pauta_vacia_se_borra_y_se_recortan_espacios()
    {
        Assert.Null(PersonalService.ValidarPauta("   ", "antes de entrenar"));
        Assert.Equal("Fruta", PersonalService.ValidarPauta("  Fruta ", "antes de entrenar"));
    }

    [Fact]
    public void Pauta_muy_larga_es_error_400() =>
        Assert.Equal(400, Assert.Throws<ErrorNegocio>(() => PersonalService.ValidarPauta(new string('a', 501), "de hidratación")).Estado);

    [Fact]
    public void El_menu_del_panel_descarta_dias_y_comidas_vacios()
    {
        var limpio = PersonalService.LimpiarComidas(new()
        {
            ["LUNES"] = new() { ["desayuno"] = " Avena ", ["cena"] = "" },
            ["MARTES"] = new() { ["almuerzo"] = "  " },
        });
        Assert.Equal(["LUNES"], limpio.Keys);
        Assert.Equal(new Dictionary<string, string> { ["desayuno"] = "Avena" }, limpio["LUNES"]);
    }

    [Fact]
    public void Comida_muy_larga_es_error_400() =>
        Assert.Equal(400, Assert.Throws<ErrorNegocio>(() =>
            PersonalService.LimpiarComidas(new() { ["LUNES"] = new() { ["cena"] = new string('a', 301) } })).Estado);
}
