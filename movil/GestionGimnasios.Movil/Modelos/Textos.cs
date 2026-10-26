namespace GestionGimnasios.Movil.Modelos;

/// <summary>Colores de la marca (los mismos de Resources/Styles/Colors.xaml) para usarlos desde C#.</summary>
public static class Colores
{
    public static readonly Color Primario = Color.FromArgb("#4C8FD6");
    public static readonly Color TextoSobrePrimario = Color.FromArgb("#0F1419");
    public static readonly Color TextoClaro = Color.FromArgb("#F5F7FA");
    public static readonly Color Exito = Color.FromArgb("#2F9E44");
    public static readonly Color Alerta = Color.FromArgb("#E67700");
    public static readonly Color Peligro = Color.FromArgb("#C92A2A");
    public static readonly Color Gris = Color.FromArgb("#161D26");
    public static readonly Color GrisTexto = Color.FromArgb("#9AA7B5");
}

/// <summary>Traduce los códigos de la API (roles, estados, días) a textos para el usuario.</summary>
public static class Textos
{
    public static string Rol(string? rol) => rol switch
    {
        "SUPER_ADMIN" => "Super administrador",
        "GERENTE" => "Gerente",
        "RECEPCIONISTA" => "Recepcionista",
        "ENTRENADOR" => "Entrenador",
        "NUTRICIONISTA" => "Nutricionista",
        "INSTRUCTOR" => "Instructor",
        "COORDINADOR" => "Coordinador",
        "PERSONAL_DE_LIMPIEZA" => "Personal de limpieza",
        "CLIENTE" => "Cliente",
        null => "",
        _ => rol,
    };

    public static string Estado(string? estado) => estado switch
    {
        "CONFIRMADA" => "Confirmada",
        "PENDIENTE" => "Pendiente",
        "ASISTIDA" => "Asistida",
        "CANCELADA" => "Cancelada",
        "NO_ASISTIO" => "No asistió",
        "EN_ESPERA" => "En espera",
        "NOTIFICADA" => "Cupo liberado: ¡reserva ya!",
        "ASIGNADA" => "Cupo asignado",
        "ACTIVO" => "Dentro",
        "COMPLETADO" => "Completado",
        "ACTIVA" => "Activa",
        "VENCIDA" => "Vencida",
        "RECHAZADA" => "Rechazada",
        "PROGRAMADA" => "Programada",
        "COMPLETADA" => "Completada",
        "ATENDIDA" => "Atendida",
        "EN_CURSO" => "En curso",
        "CONSULTA" => "Consulta",
        "SEGUIMIENTO" => "Seguimiento",
        "EVALUACION" => "Evaluación",
        null => "",
        _ => estado[..1] + estado[1..].ToLower().Replace('_', ' '),
    };

    public static Color ColorEstado(string? estado) => estado switch
    {
        "CONFIRMADA" or "ACTIVA" or "ASISTIDA" or "COMPLETADA" or "ATENDIDA" or "ASIGNADA" => Colores.Exito,
        "PENDIENTE" or "EN_ESPERA" or "NOTIFICADA" or "PROGRAMADA" => Colores.Alerta,
        "CANCELADA" or "RECHAZADA" or "NO_ASISTIO" or "VENCIDA" => Colores.Peligro,
        _ => Colores.GrisTexto,
    };

    private static readonly string[] Dias = ["LUNES", "MARTES", "MIERCOLES", "JUEVES", "VIERNES", "SABADO", "DOMINGO"];
    private static readonly string[] Cortos = ["LUN", "MAR", "MIE", "JUE", "VIE", "SAB", "DOM"];
    private static readonly string[] Legibles = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"];

    public static string Dia(string? dia)
    {
        if (dia is null) return "";
        var i = Array.IndexOf(Dias, dia.ToUpperInvariant());
        if (i < 0) i = Array.IndexOf(Cortos, dia.ToUpperInvariant());
        return i < 0 ? dia : Legibles[i];
    }

    public static string Dificultad(string? d) => d switch
    {
        "PRINCIPIANTE" => "Principiante",
        "INTERMEDIO" => "Intermedio",
        "AVANZADO" => "Avanzado",
        _ => d ?? "",
    };

    /// <summary>Índice de masa corporal y su categoría, con la misma regla que la API (EntrenamientoService.Imc).</summary>
    public static string Imc(decimal? pesoKg, decimal? alturaCm)
    {
        if (pesoKg is not > 0 || alturaCm is not > 0) return "Registra tu peso y altura para calcular tu IMC.";
        var m = alturaCm.Value / 100;
        var imc = Math.Round(pesoKg.Value / (m * m), 1);
        var categoria = imc < 18.5m ? "bajo peso" : imc <= 24.9m ? "peso normal" : imc <= 29.9m ? "sobrepeso" : "obesidad";
        return $"IMC {imc:0.0} · {categoria}";
    }
}
