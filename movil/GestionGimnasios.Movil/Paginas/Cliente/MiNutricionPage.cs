using GestionGimnasios.Movil.Modelos;

namespace GestionGimnasios.Movil.Paginas.Cliente;

/// <summary>Plan nutricional que preparó el nutricionista o entrenador del socio (CU-27).</summary>
public class MiNutricionPage() : PaginaTarjetas("Plan nutricional")
{
    protected override async Task<IEnumerable<View>> Armar()
    {
        var plan = await Api.GetAsync<PlanNutricional?>("/api/personal/mi-plan");
        if (plan is null)
            return [Vacio("Aún no tienes un plan nutricional. Solicita un nutricionista en Más › Entrenadores y nutricionistas.")];

        var vistas = new List<View>
        {
            Tarjeta("Objetivo diario", plan.Macros, $"Preparado por {plan.Asesor}", $"Actualizado el {Hora.FechaHora(plan.ActualizadoEn)}", plan.Notas),
        };
        foreach (var dia in Dias.Where(plan.PlanComidas.ContainsKey))
        {
            var comidas = plan.PlanComidas[dia];
            vistas.Add(Tarjeta(Textos.Dia(dia), Comidas.Where(c => !string.IsNullOrWhiteSpace(comidas.GetValueOrDefault(c)))
                .Select(c => $"{Comida(c)}: {comidas[c]}").ToArray()));
        }
        return vistas;
    }

    // Mismo formato que la app original: días en mayúsculas y cuatro comidas por día.
    public static readonly string[] Dias = ["LUNES", "MARTES", "MIERCOLES", "JUEVES", "VIERNES", "SABADO", "DOMINGO"];
    public static readonly string[] Comidas = ["desayuno", "almuerzo", "merienda", "cena"];

    public static string Comida(string clave) => clave.ToLowerInvariant() switch
    {
        "desayuno" => "Desayuno",
        "media_manana" or "merienda_manana" => "Media mañana",
        "almuerzo" => "Almuerzo",
        "merienda" => "Merienda",
        "cena" => "Cena",
        _ => char.ToUpper(clave[0]) + clave[1..].Replace('_', ' '),
    };
}
