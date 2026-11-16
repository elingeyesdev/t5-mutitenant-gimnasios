using System.Text.RegularExpressions;
using GestionGimnasios.Web.Services;

namespace GestionGimnasios.Web.Infrastructure;

/// <summary>
/// Reglas comunes de los formularios. Se aplican en el servidor para que el panel y la app
/// reciban el mismo mensaje, aunque el navegador o la app no hayan validado el dato.
/// </summary>
public static partial class Reglas
{
    /// <summary>Texto obligatorio: sin espacios sobrantes y con un largo máximo.</summary>
    public static string Obligatorio(string? valor, string campo, int maximo = 150)
    {
        var texto = valor?.Trim() ?? "";
        if (texto.Length == 0) throw ErrorNegocio.Solicitud($"Completa {Minuscula(campo)}.");
        if (texto.Length > maximo) throw ErrorNegocio.Solicitud($"Acorta {Minuscula(campo)}: máximo {maximo} caracteres.");
        return texto;
    }

    /// <summary>Texto opcional: vacío se guarda como nulo.</summary>
    public static string? Opcional(string? valor, string campo, int maximo)
    {
        var texto = valor?.Trim();
        if (string.IsNullOrEmpty(texto)) return null;
        if (texto.Length > maximo) throw ErrorNegocio.Solicitud($"Acorta {Minuscula(campo)}: máximo {maximo} caracteres.");
        return texto;
    }

    /// <summary>Número opcional dentro de un rango; nulo se acepta.</summary>
    public static void Rango(int? valor, int minimo, int maximo, string campo)
    {
        if (valor is int v && (v < minimo || v > maximo))
            throw ErrorNegocio.Solicitud($"Revisa {Minuscula(campo)}: debe estar entre {minimo} y {maximo}.");
    }

    public static void Rango(decimal? valor, decimal minimo, decimal maximo, string campo)
    {
        if (valor is decimal v && (v < minimo || v > maximo))
            throw ErrorNegocio.Solicitud($"Revisa {Minuscula(campo)}: debe estar entre {minimo:0.##} y {maximo:0.##}.");
    }

    /// <summary>Uno de los valores permitidos (estados, tipos, métodos).</summary>
    public static void UnoDe(string? valor, IEnumerable<string> permitidos, string campo)
    {
        if (valor is null || !permitidos.Contains(valor)) throw ErrorNegocio.Solicitud($"Elige una opción válida para {Minuscula(campo)}.");
    }

    /// <summary>Teléfono opcional: dígitos, espacios, guiones y un + inicial; de 7 a 15 dígitos.</summary>
    public static string? Telefono(string? valor)
    {
        var texto = valor?.Trim();
        if (string.IsNullOrEmpty(texto)) return null;
        var digitos = texto.Count(char.IsDigit);
        if (!TelefonoValido().IsMatch(texto) || digitos is < 7 or > 15)
            throw ErrorNegocio.Solicitud("El teléfono solo puede tener números (de 7 a 15), espacios, guiones y un + inicial.");
        return texto;
    }

    /// <summary>Fecha de nacimiento opcional: en el pasado y de alguien de entre 5 y 100 años.</summary>
    public static void FechaNacimiento(DateOnly? fecha)
    {
        if (fecha is not DateOnly f) return;
        var hoy = Reloj.Hoy;
        if (f > hoy) throw ErrorNegocio.Solicitud("La fecha de nacimiento no puede ser futura.");
        if (f < hoy.AddYears(-100) || f > hoy.AddYears(-5)) throw ErrorNegocio.Solicitud("Revisa la fecha de nacimiento: la edad debe estar entre 5 y 100 años.");
    }

    /// <summary>Los nombres de campo se escriben como en una frase («El peso (kg)» → «el peso (kg)»).</summary>
    private static string Minuscula(string campo) => campo.Length > 0 ? char.ToLowerInvariant(campo[0]) + campo[1..] : campo;

    [GeneratedRegex(@"^\+?[0-9][0-9 \-]*$")]
    private static partial Regex TelefonoValido();
}
