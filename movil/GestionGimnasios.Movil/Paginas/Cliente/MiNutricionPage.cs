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
        var pautas = new (string Titulo, string? Texto)[]
        {
            ("Antes de entrenar", plan.PautaAntesEntrenar),
            ("Después de entrenar", plan.PautaDespuesEntrenar),
            ("Hidratación", plan.Hidratacion),
        };
        var pautasDisponibles = pautas.Where(p => !string.IsNullOrWhiteSpace(p.Texto)).ToArray();
        if (pautasDisponibles.Length > 0)
        {
            vistas.Add(Seccion("Pautas del plan"));
            vistas.AddRange(pautasDisponibles.Select(p => Tarjeta(p.Titulo, p.Texto)));
        }

        foreach (var dia in plan.PlanComidas.Where(d => d.Value.Any(c => !string.IsNullOrWhiteSpace(c.Value)))
                     .OrderBy(d => OrdenDia(d.Key)))
        {
            var comidas = dia.Value.Where(c => !string.IsNullOrWhiteSpace(c.Value))
                .Select(c => $"{Comida(c.Key)}: {c.Value}").ToArray();
            if (comidas.Length > 0) vistas.Add(Tarjeta(Textos.Dia(dia.Key), comidas));
        }
        return vistas;
    }

    // Mismo formato que la app original: días en mayúsculas y cuatro comidas por día.
    public static readonly string[] Dias = ["LUNES", "MARTES", "MIERCOLES", "JUEVES", "VIERNES", "SABADO", "DOMINGO"];
    public static readonly string[] Comidas = ["desayuno", "almuerzo", "merienda", "cena"];

    private static int OrdenDia(string dia)
    {
        var indice = Array.IndexOf(Dias, dia.ToUpperInvariant());
        return indice >= 0 ? indice : Dias.Length;
    }

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
