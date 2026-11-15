using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

/// <summary>Pautas que el profesional escribió en el plan del socio. Nunca se calculan.</summary>
public record PautasNutricionalesDto(
    string Profesional, DateTime ActualizadoEn, int? KcalDiarias, decimal? ProteinaG, decimal? CarbohidratosG, decimal? GrasaG,
    string? AntesDeEntrenar, string? DespuesDeEntrenar, string? Hidratacion);

/// <summary>Lo que el socio ve al abrir un ejercicio: su demostración visual y las pautas de su plan.</summary>
public record SimulacionDto(
    int EjercicioId, string Ejercicio, string? Descripcion, string GrupoMuscular, IReadOnlyList<string> GruposSecundarios,
    string? Equipo, string Dificultad, string TipoSimulacion, string? YoutubeVideoId, string? ImagenUrl,
    PautasNutricionalesDto? Nutricion, string? AvisoNutricion);

public static class TiposSimulacion
{
    public const string Video = "VIDEO";
    public const string Imagen = "IMAGEN";
    public const string NoDisponible = "NO_DISPONIBLE";
}

/// <summary>
/// Simulaciones de ejercicios con el plan nutricional (RF-06, CU-40). La simulación es la
/// demostración visual del catálogo de ejercicios (video o imagen); una simulación 3D necesitaría
/// aprobación del docente. Las pautas nutricionales salen solo del plan que asignó el profesional:
/// el sistema no genera recomendaciones.
/// </summary>
public class SimulacionesService(AppDbContext db, UsuarioActual actual, PersonalService personal)
{
    public const string SinPautas = "Tu plan todavía no tiene pautas nutricionales. Las define tu nutricionista o tu entrenador.";

    public async Task<SimulacionDto> SimulacionAsync(int ejercicioId)
    {
        var e = await db.Ejercicios.FirstOrDefaultAsync(x => x.Id == ejercicioId && x.Activo)
                ?? throw ErrorNegocio.NoEncontrado("Ejercicio no encontrado.");
        var pautas = Pautas(await personal.PlanVigenteAsync(actual.Id));
        return new SimulacionDto(
            e.Id, e.Nombre, e.Descripcion, e.GrupoMuscular, e.GruposSecundarios, e.Equipo, e.Dificultad,
            TipoDe(e.YoutubeVideoId, e.ImagenUrl), e.YoutubeVideoId, e.ImagenUrl, pautas, pautas is null ? SinPautas : null);
    }

    public static string TipoDe(string? youtubeVideoId, string? imagenUrl) =>
        !string.IsNullOrWhiteSpace(youtubeVideoId) ? TiposSimulacion.Video
        : !string.IsNullOrWhiteSpace(imagenUrl) ? TiposSimulacion.Imagen
        : TiposSimulacion.NoDisponible;

    /// <summary>Copia lo que el profesional escribió. Sin plan o sin ningún dato, no hay pautas que mostrar.</summary>
    public static PautasNutricionalesDto? Pautas(PlanNutricionalDto? p)
    {
        if (p is null) return null;
        var tieneDatos = p.KcalDiarias is not null || p.ProteinaG is not null || p.CarbohidratosG is not null || p.GrasaG is not null
                         || p.PautaAntesEntrenar is not null || p.PautaDespuesEntrenar is not null || p.Hidratacion is not null;
        return tieneDatos
            ? new PautasNutricionalesDto(p.Asesor, p.ActualizadoEn, p.KcalDiarias, p.ProteinaG, p.CarbohidratosG, p.GrasaG,
                p.PautaAntesEntrenar, p.PautaDespuesEntrenar, p.Hidratacion)
            : null;
    }
}
