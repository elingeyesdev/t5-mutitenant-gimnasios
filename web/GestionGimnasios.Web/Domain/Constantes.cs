namespace GestionGimnasios.Web.Domain;

/// <summary>Roles del sistema y su nivel jerárquico (igual que en el proyecto base).</summary>
public static class Roles
{
    public const string SuperAdmin = "SUPER_ADMIN";
    public const string Gerente = "GERENTE";
    public const string Recepcionista = "RECEPCIONISTA";
    public const string Entrenador = "ENTRENADOR";
    public const string Nutricionista = "NUTRICIONISTA";
    public const string Instructor = "INSTRUCTOR";
    public const string Coordinador = "COORDINADOR";
    public const string Cliente = "CLIENTE";
    public const string Limpieza = "PERSONAL_DE_LIMPIEZA";
}

/// <summary>Niveles jerárquicos que usan las reglas de autorización.</summary>
public static class Niveles
{
    public const int SuperAdmin = 10;
    public const int Gerente = 5;
    public const int Recepcionista = 4;
    public const int Asesor = 3;
    public const int Staff = 2;
    public const int Cliente = 1;
}

public static class EstadosReserva
{
    public const string Confirmada = "CONFIRMADA";
    public const string Pendiente = "PENDIENTE";
    public const string Asistida = "ASISTIDA";
    public const string Cancelada = "CANCELADA";
    public const string NoAsistio = "NO_ASISTIO";

    /// <summary>Estados que ocupan cupo.</summary>
    public static readonly string[] Activas = [Confirmada, Pendiente, Asistida];
}

public static class EstadosEspera
{
    public const string EnEspera = "EN_ESPERA";
    public const string Notificada = "NOTIFICADA";
    public const string Asignada = "ASIGNADA";
    public const string Cancelada = "CANCELADA";
}

public static class EstadosAcceso
{
    public const string Activo = "ACTIVO";
    public const string Completado = "COMPLETADO";
}

public static class EstadosSuscripcion
{
    public const string Activa = "ACTIVA";
    public const string Vencida = "VENCIDA";
    public const string Cancelada = "CANCELADA";
}

public static class EstadosAsesoria
{
    public const string Pendiente = "PENDIENTE";
    public const string Activa = "ACTIVA";
    public const string Rechazada = "RECHAZADA";
    public const string Cancelada = "CANCELADA";
}

public static class EstadosSesion
{
    public const string EnCurso = "EN_CURSO";
    public const string Completada = "COMPLETADA";
}

public static class EstadosMaquina
{
    public const string Disponible = "DISPONIBLE";
    public const string Mantenimiento = "MANTENIMIENTO";
}

public static class CategoriasMaquina
{
    public const string Cardio = "CARDIO";
    public const string TrenSuperior = "TREN_SUPERIOR";
    public const string TrenInferior = "TREN_INFERIOR";
    public const string Espalda = "ESPALDA";
    public const string Multiestacion = "MULTIESTACION";

    public static readonly string[] Todas = [Cardio, TrenSuperior, TrenInferior, Espalda, Multiestacion];
}

public static class TiposRegistroEjercicio
{
    public const string PesoRepeticiones = "PESO_REPETICIONES";
    public const string SoloRepeticiones = "SOLO_REPETICIONES";
    public const string TiempoDistancia = "TIEMPO_DISTANCIA";
    public const string SoloTiempo = "SOLO_TIEMPO";
}

/// <summary>Días de la semana como los guarda el proyecto base.</summary>
public static class Dias
{
    /// <summary>Horario de atención de la sucursal (abreviado).</summary>
    public static readonly string[] Cortos = ["LUN", "MAR", "MIE", "JUE", "VIE", "SAB", "DOM"];

    /// <summary>Horarios de clases (completo).</summary>
    public static readonly string[] Largos = ["LUNES", "MARTES", "MIERCOLES", "JUEVES", "VIERNES", "SABADO", "DOMINGO"];

    public static string Largo(DayOfWeek dia) => Largos[((int)dia + 6) % 7];
}
