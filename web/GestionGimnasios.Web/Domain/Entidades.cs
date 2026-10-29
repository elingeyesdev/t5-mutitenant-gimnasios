namespace GestionGimnasios.Web.Domain;

/// <summary>Entidad que pertenece a una cadena: EF Core la filtra por la cadena del usuario.</summary>
public interface IDeCadena
{
    int CadenaId { get; set; }
}

// ── Identidad ────────────────────────────────────────────────────────────

/// <summary>Persona con cuenta. Es global: no pertenece a una cadena (decisión 4.2).</summary>
public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadoEn { get; set; }
    public string? OtpCodigo { get; set; }
    public DateTime? OtpExpiraEn { get; set; }

    public PerfilUsuario? Perfil { get; set; }
    public List<UsuarioRol> Roles { get; set; } = [];
    public List<Membresia> Membresias { get; set; } = [];
    public List<ContactoEmergencia> ContactosEmergencia { get; set; } = [];
}

public class PerfilUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string? Telefono { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public string? Genero { get; set; }
    public string? Ci { get; set; }
    public decimal? AlturaCm { get; set; }
    public string? NivelExperiencia { get; set; }
    public string? CondicionesMedicas { get; set; }
    public List<string> DeportesFavoritos { get; set; } = [];
    public string? AvatarUrl { get; set; }
    public string? NombreUsuario { get; set; }

    public Usuario? Usuario { get; set; }
    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}

public class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int Nivel { get; set; }
    public bool EsDeSistema { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadoEn { get; set; }

    public List<RolPermiso> Permisos { get; set; } = [];
}

public class Permiso
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Recurso { get; set; } = "";
    public string Accion { get; set; } = "";
    public bool Activo { get; set; } = true;
}

public class RolPermiso
{
    public int Id { get; set; }
    public int RolId { get; set; }
    public int PermisoId { get; set; }
    public DateTime OtorgadoEn { get; set; } = DateTime.UtcNow;
    public int? OtorgadoPorId { get; set; }

    public Rol? Rol { get; set; }
    public Permiso? Permiso { get; set; }
}

/// <summary>
/// Rol asignado a un usuario con su alcance: sin cadena (super administrador o cliente),
/// toda una cadena (gerente) o una sucursal (resto del personal).
/// </summary>
public class UsuarioRol
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int RolId { get; set; }
    public int? CadenaId { get; set; }
    public int? SucursalId { get; set; }
    public DateTime AsignadoEn { get; set; } = DateTime.UtcNow;
    public int? AsignadoPorId { get; set; }
    public DateTime? ExpiraEn { get; set; }

    public Usuario? Usuario { get; set; }
    public Rol? Rol { get; set; }
    public Cadena? Cadena { get; set; }
    public Sucursal? Sucursal { get; set; }
}

/// <summary>Relación de un socio con una cadena y su sucursal habitual (decisión 4.2).</summary>
public class Membresia : IDeCadena
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int CadenaId { get; set; }
    public int? SucursalId { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    public Usuario? Usuario { get; set; }
    public Cadena? Cadena { get; set; }
    public Sucursal? Sucursal { get; set; }
}

// ── Cadenas y sucursales ────────────────────────────────────────────────

/// <summary>Inquilino del sistema: una marca de gimnasios.</summary>
public class Cadena
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadaEn { get; set; }

    public List<Sucursal> Sucursales { get; set; } = [];
}

public class Sucursal : IDeCadena
{
    public int Id { get; set; }
    public int CadenaId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int CapacidadMaxima { get; set; }
    public bool Activa { get; set; } = true;
    public bool Abierta { get; set; } = true;
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadaEn { get; set; }

    public Cadena? Cadena { get; set; }
    public UbicacionSucursal? Ubicacion { get; set; }
    public List<HorarioSucursal> Horarios { get; set; } = [];
    public List<Actividad> Actividades { get; set; } = [];
    public List<Maquina> Maquinas { get; set; } = [];
}

public class UbicacionSucursal
{
    public int Id { get; set; }
    public int SucursalId { get; set; }
    public string Direccion { get; set; } = "";
    public string Ciudad { get; set; } = "";
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
}

public class HorarioSucursal
{
    public int Id { get; set; }
    public int SucursalId { get; set; }
    public string DiaSemana { get; set; } = "";
    public TimeOnly Abre { get; set; }
    public TimeOnly Cierra { get; set; }
    public bool EsFeriado { get; set; }
}

public class Maquina : IDeCadena
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int CadenaId { get; set; }
    public int SucursalId { get; set; }
    public string Nombre { get; set; } = "";
    public string Estado { get; set; } = EstadosMaquina.Disponible;
    public string Categoria { get; set; } = CategoriasMaquina.Multiestacion;
    public string? ImagenUrl { get; set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadaEn { get; set; } = DateTime.UtcNow;

    public Sucursal? Sucursal { get; set; }
}

// ── Actividades ─────────────────────────────────────────────────────────

public class Actividad : IDeCadena
{
    public int Id { get; set; }
    public int CadenaId { get; set; }
    public int SucursalId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int? DuracionMin { get; set; }
    public bool Activa { get; set; } = true;
    public bool AccesoLibre { get; set; }

    public Sucursal? Sucursal { get; set; }
    public List<HorarioActividad> Horarios { get; set; } = [];
}

public class HorarioActividad : IDeCadena
{
    public int Id { get; set; }
    public int CadenaId { get; set; }
    public int ActividadId { get; set; }
    public int? InstructorId { get; set; }
    public string DiaSemana { get; set; } = "";
    public TimeOnly Inicio { get; set; }
    public TimeOnly Fin { get; set; }
    public int CupoMaximo { get; set; }
    public bool Recurrente { get; set; } = true;

    public Actividad? Actividad { get; set; }
    public Usuario? Instructor { get; set; }
    public List<AsistenciaActividad> Asistencias { get; set; } = [];
}

public class AsistenciaActividad
{
    public int Id { get; set; }
    public int HorarioActividadId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Ingreso { get; set; } = DateTime.UtcNow;
    public DateTime? Salida { get; set; }
    public string Estado { get; set; } = "CONFIRMADA";

    public HorarioActividad? HorarioActividad { get; set; }
    public Usuario? Usuario { get; set; }
}

// ── Ejercicios y rutinas ────────────────────────────────────────────────

/// <summary>Catálogo global de ejercicios: lo comparten todas las cadenas.</summary>
public class Ejercicio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string GrupoMuscular { get; set; } = "";
    public string? Categoria { get; set; }
    public string? Tipo { get; set; }
    public List<string> GruposSecundarios { get; set; } = [];
    public string? Equipo { get; set; }
    public string Dificultad { get; set; } = "";
    public string? YoutubeVideoId { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Activo { get; set; } = true;
    public string TipoRegistro { get; set; } = TiposRegistroEjercicio.PesoRepeticiones;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadoEn { get; set; }
}

public class Rutina
{
    public int Id { get; set; }
    public int? CadenaId { get; set; }
    public int? SucursalId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int EntrenadorId { get; set; }
    public int? AsignadaAId { get; set; }
    public string Dificultad { get; set; } = "";
    public int? DuracionSemanas { get; set; }
    public bool EsPlantilla { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadaEn { get; set; }

    public Usuario? Entrenador { get; set; }
    public Usuario? AsignadaA { get; set; }
    public Sucursal? Sucursal { get; set; }
    public List<RutinaEjercicio> Ejercicios { get; set; } = [];
}

public class RutinaEjercicio
{
    public int Id { get; set; }
    public int RutinaId { get; set; }
    public int EjercicioId { get; set; }
    public int Orden { get; set; }
    public int? Series { get; set; }
    public string? Repeticiones { get; set; }
    public decimal? PesoKg { get; set; }
    public string? DiaSemana { get; set; }
    public int? DescansoSeg { get; set; }
    public string? Notas { get; set; }
    public string? Parametros { get; set; }

    public Rutina? Rutina { get; set; }
    public Ejercicio? Ejercicio { get; set; }
}

// ── Entrenamiento y seguimiento (pertenecen a la persona) ───────────────

public class PerfilEntrenamiento
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string? ObjetivoPrincipal { get; set; }
    public string? NivelExperiencia { get; set; }
    public List<string> TiposPreferidos { get; set; } = [];
    public List<string> AreasPrioritarias { get; set; } = [];
    public int? DiasDisponibles { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public class RestriccionEntrenamiento
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Tipo { get; set; } = "";
    public string? Descripcion { get; set; }
    public List<string> AreasAfectadas { get; set; } = [];
    public List<string> MovimientosEvitar { get; set; } = [];
    public bool RequiereAprobacion { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadaEn { get; set; }
}

public class ContactoEmergencia
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Relacion { get; set; } = "";
    public bool EsPrincipal { get; set; }
}

/// <summary>
/// Autorización del socio para que otra cadena consulte su perfil deportivo (RF-02, CU-36).
/// No se borra: la revocación queda registrada para poder verificarla.
/// </summary>
public class ConsentimientoPerfil : IDeCadena
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    /// <summary>Cadena autorizada a consultar el perfil.</summary>
    public int CadenaId { get; set; }
    /// <summary>Datos que comparte (DatosPortables).</summary>
    public List<string> Datos { get; set; } = [];
    public DateTime OtorgadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? RevocadoEn { get; set; }

    public Usuario? Usuario { get; set; }
    public Cadena? Cadena { get; set; }
    public List<ConsultaPerfilPortable> Consultas { get; set; } = [];
}

/// <summary>Cada consulta del personal autorizado queda registrada para que el socio pueda verla.</summary>
public class ConsultaPerfilPortable
{
    public int Id { get; set; }
    public int ConsentimientoId { get; set; }
    public int ConsultadoPorId { get; set; }
    public DateTime ConsultadoEn { get; set; } = DateTime.UtcNow;

    public Usuario? ConsultadoPor { get; set; }
}

public class SesionEntrenamiento
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int? RutinaId { get; set; }
    public int? SucursalId { get; set; }
    public string? TipoDeporte { get; set; }
    public DateTime Inicio { get; set; } = DateTime.UtcNow;
    public DateTime? Fin { get; set; }
    public string Estado { get; set; } = EstadosSesion.EnCurso;
    public int? DuracionSeg { get; set; }
    public int? Calorias { get; set; }
    public string? Notas { get; set; }

    public Rutina? Rutina { get; set; }
    public Usuario? Usuario { get; set; }
    public List<SerieEntrenamiento> Series { get; set; } = [];
}

public class SerieEntrenamiento
{
    public int Id { get; set; }
    public int SesionId { get; set; }
    public int? RutinaEjercicioId { get; set; }
    public int? EjercicioId { get; set; }
    public int NumeroSerie { get; set; }
    public int? Repeticiones { get; set; }
    public decimal? PesoKg { get; set; }
    public int? DuracionSeg { get; set; }
    public int? DistanciaM { get; set; }
    public int? DescansoSeg { get; set; }
    public int? EsfuerzoPercibido { get; set; }
    public string? Metadatos { get; set; }
    public DateTime CompletadaEn { get; set; } = DateTime.UtcNow;

    public SesionEntrenamiento? Sesion { get; set; }
    public Ejercicio? Ejercicio { get; set; }
}

public class MetricaFisica
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int? SucursalId { get; set; }
    public DateTime RegistradaEn { get; set; } = DateTime.UtcNow;
    public decimal? PesoKg { get; set; }
    public decimal? GrasaPct { get; set; }
    public decimal? MasaMuscularKg { get; set; }
    public decimal? CinturaCm { get; set; }
    public decimal? PechoCm { get; set; }
    public decimal? CaderaCm { get; set; }
    public decimal? BrazoCm { get; set; }
    public decimal? MusloCm { get; set; }
    public decimal? PantorrillaCm { get; set; }
    public string? Notas { get; set; }
}

// ── Suscripciones ───────────────────────────────────────────────────────

/// <summary>Plan de suscripción. Sin cadena = plan global, como en el proyecto base.</summary>
public class PlanSuscripcion
{
    public int Id { get; set; }
    public int? CadenaId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal PrecioMensual { get; set; }
    public int? MaxSucursales { get; set; }
    public List<string> Beneficios { get; set; } = [];
    public bool Activo { get; set; } = true;

    public Cadena? Cadena { get; set; }
}

public class Suscripcion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int PlanId { get; set; }
    public int? CadenaId { get; set; }
    public int? SucursalPrincipalId { get; set; }
    public string Estado { get; set; } = EstadosSuscripcion.Activa;
    public DateOnly Inicio { get; set; }
    public DateOnly Fin { get; set; }
    public bool RenovacionAutomatica { get; set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadaEn { get; set; }

    public Usuario? Usuario { get; set; }
    public PlanSuscripcion? Plan { get; set; }
    public Sucursal? SucursalPrincipal { get; set; }
    public List<PagoSuscripcion> Pagos { get; set; } = [];
}

public class PagoSuscripcion
{
    public int Id { get; set; }
    public int SuscripcionId { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? Metodo { get; set; }
    public string Estado { get; set; } = "PAGADO";
}

// ── Beneficios y canjes (RF-03) ─────────────────────────────────────────

/// <summary>Beneficio, vale o cupón que la cadena ofrece a sus socios, con sus reglas de canje (CU-37).</summary>
public class Beneficio : IDeCadena
{
    public int Id { get; set; }
    public int CadenaId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = TiposBeneficio.Descuento;
    /// <summary>Condiciones de uso que ve el socio (por ejemplo, "válido en bebidas").</summary>
    public string? Condiciones { get; set; }
    public DateOnly VigenteDesde { get; set; }
    public DateOnly VigenteHasta { get; set; }
    /// <summary>Canjes disponibles en total; sin valor, no se agota.</summary>
    public int? CuposTotales { get; set; }
    public int LimitePorSocio { get; set; } = 1;
    /// <summary>Ingresos a la cadena en los últimos 30 días que el socio necesita para canjearlo.</summary>
    public int AsistenciasMinimas { get; set; }
    public bool RequierePlanActivo { get; set; } = true;
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public Cadena? Cadena { get; set; }
    public List<CanjeBeneficio> Canjes { get; set; } = [];
}

/// <summary>Canje de un beneficio, con el código del comprobante que el socio muestra en recepción.</summary>
public class CanjeBeneficio : IDeCadena
{
    public int Id { get; set; }
    public int CadenaId { get; set; }
    public int BeneficioId { get; set; }
    public int UsuarioId { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime CanjeadoEn { get; set; } = DateTime.UtcNow;
    public string Estado { get; set; } = EstadosCanje.Emitido;
    public DateTime? EntregadoEn { get; set; }
    public int? EntregadoPorId { get; set; }

    public Beneficio? Beneficio { get; set; }
    public Usuario? Usuario { get; set; }
}

// ── Reservas, lista de espera y accesos ─────────────────────────────────

public class Reserva : IDeCadena
{
    public int Id { get; set; }
    public int CadenaId { get; set; }
    public int UsuarioId { get; set; }
    public int? HorarioActividadId { get; set; }
    public int? SucursalId { get; set; }
    public int? ActividadId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly? Inicio { get; set; }
    public TimeOnly? Fin { get; set; }
    public string Estado { get; set; } = EstadosReserva.Confirmada;
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
    public DateTime? CanceladaEn { get; set; }
    public bool RecordatorioEnviado { get; set; }
    public int CreadaPorId { get; set; }
    public string? CodigoQr { get; set; }

    public Usuario? Usuario { get; set; }
    public HorarioActividad? HorarioActividad { get; set; }
    public Sucursal? Sucursal { get; set; }
    public Actividad? Actividad { get; set; }
}

public class EntradaListaEspera : IDeCadena
{
    public int Id { get; set; }
    public int CadenaId { get; set; }
    public int? ReservaId { get; set; }
    public int UsuarioId { get; set; }
    public int HorarioActividadId { get; set; }
    public DateOnly? Fecha { get; set; }
    public int Posicion { get; set; }
    public string Estado { get; set; } = EstadosEspera.EnEspera;
    public DateTime? NotificadaEn { get; set; }
    public DateTime? AsignadaEn { get; set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    public Usuario? Usuario { get; set; }
    public HorarioActividad? HorarioActividad { get; set; }
}

/// <summary>Registro de ingreso y salida de una sucursal (check-in).</summary>
public class Acceso : IDeCadena
{
    public int Id { get; set; }
    public int CadenaId { get; set; }
    public int UsuarioId { get; set; }
    public int SucursalId { get; set; }
    public DateTime Ingreso { get; set; } = DateTime.UtcNow;
    public DateTime? Salida { get; set; }
    public string Metodo { get; set; } = "MANUAL";
    public string Estado { get; set; } = EstadosAcceso.Activo;
    public int? RegistradoPorId { get; set; }

    public Usuario? Usuario { get; set; }
    public Sucursal? Sucursal { get; set; }
}

public class Visita
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int SucursalId { get; set; }
    public DateTime Entrada { get; set; }
    public DateTime? Salida { get; set; }
    public int? DuracionMin { get; set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    public Sucursal? Sucursal { get; set; }
}

// ── Personal ────────────────────────────────────────────────────────────

public class HorarioPersonal : IDeCadena
{
    public int Id { get; set; }
    public int CadenaId { get; set; }
    public int UsuarioId { get; set; }
    public int SucursalId { get; set; }
    /// <summary>0 = domingo … 6 = sábado.</summary>
    public int DiaSemana { get; set; }
    public TimeOnly Inicio { get; set; }
    public TimeOnly Fin { get; set; }

    public Usuario? Usuario { get; set; }
    public Sucursal? Sucursal { get; set; }
}

/// <summary>Relación de asesoría entre un cliente y un entrenador o nutricionista.</summary>
public class Asesoria
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int AsesorId { get; set; }
    public string Estado { get; set; } = EstadosAsesoria.Pendiente;
    public int? MetaSesionesSemana { get; set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    public Usuario? Cliente { get; set; }
    public Usuario? Asesor { get; set; }
}

public class CitaNutricional
{
    public int Id { get; set; }
    public int NutricionistaId { get; set; }
    public int PacienteId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public string Tipo { get; set; } = "CONSULTA";
    public string Estado { get; set; } = "PENDIENTE";
    public string? Notas { get; set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    public Usuario? Nutricionista { get; set; }
    public Usuario? Paciente { get; set; }
}

/// <summary>Plan nutricional que un asesor define para su cliente.</summary>
public class PlanNutricional
{
    public int Id { get; set; }
    public int AsesorId { get; set; }
    public int ClienteId { get; set; }
    public int? KcalDiarias { get; set; }
    public decimal? ProteinaG { get; set; }
    public decimal? CarbohidratosG { get; set; }
    public decimal? GrasaG { get; set; }
    public string? Notas { get; set; }
    /// <summary>JSON: día → comida → descripción, como el meal_plan del proyecto base.</summary>
    public string? PlanComidas { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}

// ── Alertas de inactividad (RF-04) ──────────────────────────────────────

/// <summary>Umbrales de inactividad que el gerente configura para su cadena (CU-38).</summary>
public class ReglaInactividad : IDeCadena
{
    public int Id { get; set; }
    public int CadenaId { get; set; }
    /// <summary>Días sin asistir desde los que se abre una alerta de nivel medio.</summary>
    public int DiasAviso { get; set; } = 7;
    /// <summary>Días sin asistir desde los que la alerta pasa a riesgo alto.</summary>
    public int DiasRiesgo { get; set; } = 14;
    /// <summary>Revisar solo a los socios con un plan activo en la cadena.</summary>
    public bool SoloConPlanActivo { get; set; } = true;
    public bool Activa { get; set; } = true;
    public DateTime ActualizadaEn { get; set; } = DateTime.UtcNow;
    public int? ActualizadaPorId { get; set; }
}

/// <summary>Socio que superó el umbral de su cadena. Hay como máximo una alerta abierta por socio y cadena.</summary>
public class AlertaInactividad : IDeCadena
{
    public int Id { get; set; }
    public int CadenaId { get; set; }
    /// <summary>Sucursal habitual del socio: limita qué personal de sucursal ve la alerta (RT-02).</summary>
    public int? SucursalId { get; set; }
    public int UsuarioId { get; set; }
    public string Nivel { get; set; } = NivelesAlerta.Medio;
    public int DiasInactivo { get; set; }
    /// <summary>Último ingreso o entrenamiento en la cadena; sin valor si nunca asistió.</summary>
    public DateTime? UltimaActividad { get; set; }
    public string Estado { get; set; } = EstadosAlerta.Abierta;
    public DateTime DetectadaEn { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadaEn { get; set; } = DateTime.UtcNow;
    public DateTime? CerradaEn { get; set; }
    public string? MotivoCierre { get; set; }

    public Usuario? Usuario { get; set; }
    public Sucursal? Sucursal { get; set; }
    public List<IntervencionAlerta> Intervenciones { get; set; } = [];
}

/// <summary>Contacto o seguimiento que el personal registra en una alerta.</summary>
public class IntervencionAlerta
{
    public int Id { get; set; }
    public int AlertaId { get; set; }
    public int RegistradaPorId { get; set; }
    public string Tipo { get; set; } = TiposIntervencion.Llamada;
    public string Nota { get; set; } = "";
    public DateTime RegistradaEn { get; set; } = DateTime.UtcNow;

    public Usuario? RegistradaPor { get; set; }
}

// ── Notificaciones, mensajería y sistema ────────────────────────────────

public class PlantillaNotificacion
{
    public int Id { get; set; }
    public string Tipo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Cuerpo { get; set; } = "";
}

public class Notificacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int? PlantillaId { get; set; }
    public string Tipo { get; set; } = "GENERAL";
    public string Titulo { get; set; } = "";
    public string Cuerpo { get; set; } = "";
    public DateTime EnviadaEn { get; set; } = DateTime.UtcNow;
    public DateTime? LeidaEn { get; set; }
}

public class PreferenciaNotificacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public bool Push { get; set; } = true;
    public bool ConfirmacionesReserva { get; set; } = true;
    public bool RecordatoriosClase { get; set; } = true;
    public bool AlertasCancelacion { get; set; } = true;
    public bool Promociones { get; set; }
    public DateTime ActualizadaEn { get; set; } = DateTime.UtcNow;
}

public class Conversacion
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int AsesorId { get; set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    public Usuario? Cliente { get; set; }
    public Usuario? Asesor { get; set; }
    public List<Mensaje> Mensajes { get; set; } = [];
}

public class Mensaje
{
    public int Id { get; set; }
    public int ConversacionId { get; set; }
    public int RemitenteId { get; set; }
    public string Contenido { get; set; } = "";
    public bool BorradoParaRemitente { get; set; }
    public bool BorradoParaReceptor { get; set; }
    public bool BorradoParaTodos { get; set; }
    public DateTime? LeidoEn { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public class ParametroSistema
{
    public int Id { get; set; }
    public string Clave { get; set; } = "";
    public string Valor { get; set; } = "";
    public string? Descripcion { get; set; }
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
    public int? ActualizadoPorId { get; set; }
}
