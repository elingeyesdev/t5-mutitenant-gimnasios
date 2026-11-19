using System.Globalization;
using System.Text;

namespace GestionGimnasios.Movil.Modelos;

/// <summary>Normalización para búsquedas y nombres canónicos, sin coincidencias parciales inseguras.</summary>
public static class TextoEjercicio
{
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
        var sinAcentos = new string(descompuesto
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        return sinAcentos.Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    /// <summary>Filtra y ordena por coincidencia inicial, luego por coincidencia interna y finalmente por nombre.</summary>
    public static List<Ejercicio> Ordenar(IEnumerable<Ejercicio> ejercicios, string? consulta)
    {
        var catalogo = ejercicios
            .Where(e => e.Activo)
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .ToList();
        var texto = Normalizar(consulta);
        if (texto.Length == 0)
            return catalogo.OrderBy(e => Normalizar(e.Nombre)).ThenBy(e => e.Nombre).ToList();

        return catalogo
            .Select(e => new { Ejercicio = e, Nombre = Normalizar(e.Nombre) })
            .Where(x => x.Nombre.Contains(texto, StringComparison.Ordinal))
            .OrderBy(x => x.Nombre.StartsWith(texto, StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(x => x.Nombre)
            .ThenBy(x => x.Ejercicio.Nombre)
            .Select(x => x.Ejercicio)
            .ToList();
    }
}
