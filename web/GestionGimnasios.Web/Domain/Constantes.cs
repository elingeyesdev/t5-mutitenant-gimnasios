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

/// <summary>Tipos de beneficio que la cadena ofrece a sus socios (RF-03).</summary>
public static class TiposBeneficio
{
    public const string Descuento = "DESCUENTO";
    /// <summary>Producto o servicio sin costo (bebida, clase, evaluación).</summary>
    public const string Vale = "VALE";
    public const string Cupon = "CUPON";

    public static readonly string[] Todos = [Descuento, Vale, Cupon];

    public static string Etiqueta(string tipo) => tipo switch
    {
        Descuento => "Descuento",
        Vale => "Vale",
        Cupon => "Cupón",
        _ => tipo,
    };
}

public static class EstadosCanje
{
    /// <summary>Canje registrado; el socio muestra el comprobante en recepción.</summary>
    public const string Emitido = "EMITIDO";
    public const string Entregado = "ENTREGADO";
}

public static class EstadosAsesoria
{
    public const string Pendiente = "PENDIENTE";
    public const string Activa = "ACTIVA";
    public const string Rechazada = "RECHAZADA";
    public const string Cancelada = "CANCELADA";
}

/// <summary>Datos del perfil deportivo que el socio puede compartir con otra cadena (RF-02).</summary>
public static class DatosPortables
{
    /// <summary>Objetivo, nivel de experiencia y preferencias de entrenamiento.</summary>
    public const string Perfil = "PERFIL";
    /// <summary>Lesiones y movimientos a evitar.</summary>
    public const string Restricciones = "RESTRICCIONES";
    /// <summary>Sesiones de entrenamiento registradas.</summary>
    public const string Historial = "HISTORIAL";
    /// <summary>Medidas físicas (peso, grasa, circunferencias).</summary>
    public const string Metricas = "METRICAS";

    public static readonly string[] Todos = [Perfil, Restricciones, Historial, Metricas];

    public static string Etiqueta(string dato) => dato switch
    {
        Perfil => "Perfil de entrenamiento",
        Restricciones => "Restricciones",
        Historial => "Historial de sesiones",
        Metricas => "Métricas físicas",
        _ => dato,
    };
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

/// <summary>Nivel de una alerta de inactividad según los umbrales de la cadena (RF-04).</summary>
public static class NivelesAlerta
{
    public const string Medio = "MEDIO";
    public const string Alto = "ALTO";
}

public static class EstadosAlerta
{
    public const string Abierta = "ABIERTA";
    /// <summary>El personal ya registró al menos un contacto.</summary>
    public const string EnSeguimiento = "EN_SEGUIMIENTO";
    public const string Cerrada = "CERRADA";
}

public static class TiposIntervencion
{
    public const string Llamada = "LLAMADA";
    public const string Mensaje = "MENSAJE";
    public const string Presencial = "PRESENCIAL";
    public const string Otro = "OTRO";

    public static readonly string[] Todos = [Llamada, Mensaje, Presencial, Otro];

    public static string Etiqueta(string tipo) => tipo switch
    {
        Llamada => "Llamada",
        Mensaje => "Mensaje",
        Presencial => "En persona",
        _ => "Otro",
    };
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
