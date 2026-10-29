using System.Text.Json.Serialization;

namespace GestionGimnasios.Movil.Modelos;

// Contratos de la API de la web (ver web/GestionGimnasios.Web/Services). Los nombres coinciden con el JSON;
// las propiedades marcadas con [JsonIgnore] solo existen para mostrar datos en pantalla.

/// <summary>La API guarda las fechas en UTC; Santa Cruz está en America/La_Paz (UTC-4, sin horario de verano).</summary>
public static class Hora
{
    private static readonly TimeZoneInfo Zona = Buscar();

    private static TimeZoneInfo Buscar()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("BOT", TimeSpan.FromHours(-4), "Bolivia", "Bolivia"); }
    }

    public static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);
    public static DateOnly Hoy => DateOnly.FromDateTime(Local(DateTime.UtcNow));
    public static string Corta(TimeOnly? t) => t?.ToString("HH:mm") ?? "";
    public static string Fecha(DateOnly f) => f.ToString("ddd dd/MM/yyyy");
    public static string FechaHora(DateTime utc) => Local(utc).ToString("dd/MM/yyyy HH:mm");
    public static string Duracion(int? seg) => seg is null ? "—" : seg >= 3600 ? $"{seg / 3600} h {seg % 3600 / 60} min" : seg >= 60 ? $"{seg / 60} min" : $"{seg} s";
}

public record SesionIniciada(string Token, Usuario Usuario);

public record Perfil(
    string Nombres, string Apellidos, string? Telefono, DateOnly? FechaNacimiento, string? Genero, string? Ci,
    decimal? AlturaCm, string? NivelExperiencia, string? CondicionesMedicas, List<string>? DeportesFavoritos,
    string? AvatarUrl, string? NombreUsuario);

public record RolAsignado(int Id, int RolId, string Rol, int Nivel, int? CadenaId, string? Cadena, int? SucursalId, string? Sucursal);
public record Membresia(int Id, int CadenaId, string Cadena, int? SucursalId, string? Sucursal, bool Activa);

public record Usuario(
    int Id, string Email, bool Activo, DateTime CreadoEn, string? Rol, int Nivel,
    int? CadenaId, string? Cadena, int? SucursalId, string? Sucursal, Perfil? Perfil,
    List<RolAsignado>? Roles, List<Membresia>? Membresias, string? NombreCompleto)
{
    [JsonIgnore] public string Nombre => Perfil?.Nombres ?? Email;
    [JsonIgnore] public string Iniciales => Perfil is null ? Email[..1].ToUpper() : $"{Perfil.Nombres.FirstOrDefault()}{Perfil.Apellidos.FirstOrDefault()}".ToUpper();
    [JsonIgnore] public string RolLegible => Textos.Rol(Rol);
    [JsonIgnore] public string Lugar => Sucursal ?? Cadena ?? "Todas las cadenas";
}

public record ClienteResumen(int Id, string Email, string Nombres, string Apellidos, string? Ci)
{
    [JsonIgnore] public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}

// ── Sedes ───────────────────────────────────────────────────────────────

public record Ubicacion(string Direccion, string Ciudad, decimal Latitud, decimal Longitud);
public record HorarioSucursal(int Id, string DiaSemana, TimeOnly Abre, TimeOnly Cierra, bool EsFeriado)
{
    [JsonIgnore] public string Texto => EsFeriado ? $"{Textos.Dia(DiaSemana)}: cerrado" : $"{Textos.Dia(DiaSemana)}: {Abre:HH\\:mm} – {Cierra:HH\\:mm}";
}
public record ResumenMaquinas(int Total, int Disponibles, int EnMantenimiento);

public record Sucursal(
    int Id, int CadenaId, string Cadena, string Nombre, string? Descripcion, int CapacidadMaxima,
    bool Activa, bool AbiertaAhora, int OcupacionActual, Ubicacion? Ubicacion,
    List<HorarioSucursal> Horarios, List<string> Servicios, ResumenMaquinas Maquinas, double? DistanciaKm)
{
    [JsonIgnore] public string Direccion => Ubicacion is null ? "Sin dirección registrada" : $"{Ubicacion.Direccion}, {Ubicacion.Ciudad}";
    [JsonIgnore] public string Estado => AbiertaAhora ? "Abierto" : "Cerrado";
    [JsonIgnore] public Color ColorEstado => AbiertaAhora ? Colores.Exito : Colores.Peligro;
    [JsonIgnore] public double PorcentajeOcupacion => CapacidadMaxima > 0 ? Math.Min(1, (double)OcupacionActual / CapacidadMaxima) : 0;
    [JsonIgnore] public string Ocupacion => $"{OcupacionActual} de {CapacidadMaxima} personas";
    [JsonIgnore] public string Distancia => DistanciaKm is null ? "" : $"{DistanciaKm:0.0} km";
    [JsonIgnore] public string ServiciosTexto => Servicios.Count == 0 ? "—" : string.Join(" · ", Servicios);
}

public record GimnasioFrecuente(int SucursalId, string Nombre, string? Direccion, int Visitas, int AforoActual, int AforoMaximo, int Porcentaje, string Etiqueta)
{
    [JsonIgnore] public string Texto => $"{Visitas} visitas · {Etiqueta} ({Porcentaje}%)";
}

public record EstadisticasSucursalHoy(int Capacidad, int Ocupacion, int ReservasHoy, int Pendientes, int Completadas);

// ── Reservas y lista de espera ──────────────────────────────────────────

public record Disponibilidad(
    int HorarioId, int ActividadId, string Actividad, string? Descripcion, int SucursalId, string Sucursal, string DiaSemana, DateOnly Fecha,
    TimeOnly Inicio, TimeOnly Fin, string? Instructor, int CupoMaximo, int Reservados, int Disponibles, bool YaReservado, bool EnEspera)
{
    [JsonIgnore] public string Horario => $"{Inicio:HH\\:mm} – {Fin:HH\\:mm}";
    [JsonIgnore] public string Cupos => YaReservado ? "Ya reservaste esta clase" : EnEspera ? "Estás en la lista de espera" : Disponibles > 0 ? $"{Disponibles} de {CupoMaximo} cupos libres" : "Clase llena";
    [JsonIgnore] public string InstructorTexto => Instructor is null ? "Sin instructor asignado" : $"Con {Instructor}";
    [JsonIgnore] public bool PuedeReservar => !YaReservado && !EnEspera && Disponibles > 0;
    [JsonIgnore] public bool PuedeEsperar => !YaReservado && !EnEspera && Disponibles <= 0;
    [JsonIgnore] public Color ColorCupos => YaReservado || EnEspera ? Colores.Primario : Disponibles > 0 ? Colores.Exito : Colores.Peligro;
}

public record Reserva(
    int Id, int UsuarioId, string Usuario, string? Ci, int? HorarioActividadId, int? ActividadId, string Actividad,
    int? SucursalId, string Sucursal, string? Cadena, string? Direccion, string? Instructor, DateOnly Fecha, TimeOnly? Inicio, TimeOnly? Fin,
    string Estado, string? Codigo, DateTime CreadaEn, DateTime? CanceladaEn, bool EsPasada)
{
    [JsonIgnore] public string Cuando => $"{Hora.Fecha(Fecha)} · {Hora.Corta(Inicio)} – {Hora.Corta(Fin)}";
    [JsonIgnore] public string EstadoTexto => Textos.Estado(Estado);
    [JsonIgnore] public Color ColorEstado => Textos.ColorEstado(Estado);
    [JsonIgnore] public bool Cancelable => !EsPasada && Estado is "CONFIRMADA" or "PENDIENTE";
}

public record ResultadoIngreso(Reserva Reserva, string Mensaje);

// ── Portabilidad del perfil deportivo ────────────────────────────────────

public record CadenaDisponible(int Id, string Nombre, string? Descripcion);

public record Consentimiento(
    int Id, int UsuarioId, string Socio, int CadenaId, string Cadena, List<string> Datos,
    DateTime OtorgadoEn, DateTime? RevocadoEn, int Consultas, DateTime? UltimaConsulta)
{
    [JsonIgnore] public bool Vigente => RevocadoEn is null;
    [JsonIgnore] public string Estado => Vigente ? "Vigente" : "Revocada";
    [JsonIgnore] public string FechaOtorgado => Hora.FechaHora(OtorgadoEn);
    [JsonIgnore] public string FechaRevocado => RevocadoEn is null ? "" : Hora.FechaHora(RevocadoEn.Value);
}

public record ConsultaPerfil(DateTime ConsultadoEn, string ConsultadoPor)
{
    [JsonIgnore] public string Fecha => Hora.FechaHora(ConsultadoEn);
}

public record EntradaEspera(
    int Id, int UsuarioId, string Usuario, int HorarioActividadId, string Actividad, string Sucursal, string DiaSemana,
    TimeOnly Inicio, DateOnly? Fecha, int Posicion, string Estado, DateTime CreadaEn, DateTime? AsignadaEn, int? ReservaId)
{
    [JsonIgnore] public string Cuando => Fecha is null ? $"{Textos.Dia(DiaSemana)} {Inicio:HH\\:mm}" : $"{Hora.Fecha(Fecha.Value)} · {Inicio:HH\\:mm}";
    [JsonIgnore] public string PosicionTexto => Estado == "EN_ESPERA" ? $"Posición {Posicion} en la lista" : Textos.Estado(Estado);
}

// ── Suscripciones ───────────────────────────────────────────────────────

public record Plan(int Id, int? CadenaId, string? Cadena, string Nombre, string? Descripcion, decimal PrecioMensual, int? MaxSucursales, List<string> Beneficios, bool Activo);
public record Pago(int Id, decimal Monto, DateTime Fecha, string? Metodo, string Estado)
{
    [JsonIgnore] public string Texto => $"Bs {Monto:0.00} · {Hora.FechaHora(Fecha)} · {Metodo ?? "—"}";
}

public record Suscripcion(
    int Id, int UsuarioId, string Usuario, int PlanId, string Plan, decimal PrecioMensual, int? SucursalPrincipalId, string? SucursalPrincipal,
    string Estado, DateOnly Inicio, DateOnly Fin, bool RenovacionAutomatica, int DiasRestantes, List<Pago> Pagos)
{
    [JsonIgnore] public string Vigencia => $"Del {Inicio:dd/MM/yyyy} al {Fin:dd/MM/yyyy}";
    [JsonIgnore] public string Restante => Estado == "ACTIVA" ? $"{DiasRestantes} días restantes" : Textos.Estado(Estado);
    [JsonIgnore] public string Precio => $"Bs {PrecioMensual:0.00} / mes";
}

// ── Accesos ─────────────────────────────────────────────────────────────

public record VistaPreviaAcceso(int UsuarioId, string NombreCompleto, string Rol, string Sucursal, bool Dentro, string? AvatarUrl)
{
    [JsonIgnore] public string Accion => Dentro ? "Registrar SALIDA" : "Registrar INGRESO";
    [JsonIgnore] public string Estado => Dentro ? "Está dentro de la sede" : "Está fuera de la sede";
}

public record Acceso(
    int Id, int UsuarioId, string Usuario, string Email, string Rol, int SucursalId, string Sucursal,
    DateTime Ingreso, DateTime? Salida, string Metodo, string Estado, int? MinutosDentro)
{
    [JsonIgnore] public string Texto => Salida is null
        ? $"Ingreso {Hora.FechaHora(Ingreso)} · {Sucursal}"
        : $"{Hora.FechaHora(Ingreso)} – {Hora.Local(Salida.Value):HH:mm} · {MinutosDentro} min · {Sucursal}";
    [JsonIgnore] public string EstadoTexto => Textos.Estado(Estado);
}

// ── Entrenamiento ───────────────────────────────────────────────────────

public record Ejercicio(
    int Id, string Nombre, string? Descripcion, string GrupoMuscular, string? Categoria, string? Tipo,
    List<string> GruposSecundarios, string? Equipo, string Dificultad, string? YoutubeVideoId,
    string? ImagenUrl, string TipoRegistro, bool Activo)
{
    [JsonIgnore] public string Detalle => $"{GrupoMuscular} · {Textos.Dificultad(Dificultad)}{(Equipo is null ? "" : " · " + Equipo)}";
}

public record RutinaEjercicio(
    int Id, int EjercicioId, string Ejercicio, string GrupoMuscular, string? YoutubeVideoId, string TipoRegistro,
    int Orden, int? Series, string? Repeticiones, decimal? PesoKg, string? DiaSemana, int? DescansoSeg, string? Notas)
{
    [JsonIgnore] public string Prescripcion =>
        $"{Series ?? 3} series × {Repeticiones ?? "10"}{(PesoKg is null ? "" : $" · {PesoKg:0.#} kg")} · descanso {DescansoSeg ?? 60} s";
    [JsonIgnore] public string DiaTexto => DiaSemana is null ? "Cualquier día" : Textos.Dia(DiaSemana);
    [JsonIgnore] public bool TieneVideo => !string.IsNullOrEmpty(YoutubeVideoId);
}

public record Rutina(
    int Id, string Nombre, string? Descripcion, string Dificultad, int? DuracionSemanas, bool EsPlantilla, bool Activa,
    int EntrenadorId, string? Entrenador, int? AsignadaAId, string? AsignadaA, int? SucursalId, string? Sucursal,
    DateTime CreadaEn, List<RutinaEjercicio> Ejercicios)
{
    [JsonIgnore] public string Detalle => $"{Textos.Dificultad(Dificultad)} · {Ejercicios.Count} ejercicios{(DuracionSemanas is null ? "" : $" · {DuracionSemanas} semanas")}";
    [JsonIgnore] public string EntrenadorTexto => Entrenador is null ? "" : $"Asignada por {Entrenador}";
}

public record Serie(
    int Id, int NumeroSerie, int? EjercicioId, string? Ejercicio, string? GrupoMuscular, int? RutinaEjercicioId,
    int? Repeticiones, decimal? PesoKg, int? DuracionSeg, int? DistanciaM, int? DescansoSeg, int? EsfuerzoPercibido, DateTime CompletadaEn)
{
    [JsonIgnore] public string Texto => $"{Ejercicio} · serie {NumeroSerie}: {Repeticiones ?? 0} reps{(PesoKg is null ? "" : $" × {PesoKg:0.#} kg")}";
}

public record SesionEntrenamiento(
    int Id, int UsuarioId, int? SucursalId, string Lugar, string Actividad, int? RutinaId, string? Rutina, int? EntrenadorId,
    string? TipoDeporte, string Estado, DateTime Inicio, DateTime? Fin, int? DuracionSeg, int? Calorias, string? Notas,
    List<Serie> Series)
{
    [JsonIgnore] public string Titulo => Rutina ?? TipoDeporte ?? Actividad;
    [JsonIgnore] public string Detalle => $"{Hora.FechaHora(Inicio)} · {Hora.Duracion(DuracionSeg)} · {Series.Count} series · {Lugar}";
}

public record PuntoRecord(DateOnly Fecha, decimal PesoMaxKg, int Series);
public record RecordFuerza(int EjercicioId, string Ejercicio, string GrupoMuscular, List<PuntoRecord> Historial)
{
    [JsonIgnore] public decimal Maximo => Historial.Count == 0 ? 0 : Historial.Max(h => h.PesoMaxKg);
    [JsonIgnore] public string Texto => $"Récord: {Maximo:0.#} kg · {Historial.Count} sesiones";
}

public record Metrica(
    int Id, DateTime RegistradaEn, decimal? PesoKg, decimal? GrasaPct, decimal? MasaMuscularKg, decimal? CinturaCm,
    decimal? PechoCm, decimal? CaderaCm, decimal? BrazoCm, decimal? MusloCm, decimal? PantorrillaCm, string? Notas)
{
    [JsonIgnore] public string Fecha => Hora.FechaHora(RegistradaEn);
    [JsonIgnore] public string Resumen => string.Join(" · ", new[]
    {
        PesoKg is null ? null : $"{PesoKg:0.#} kg",
        GrasaPct is null ? null : $"grasa {GrasaPct:0.#}%",
        MasaMuscularKg is null ? null : $"músculo {MasaMuscularKg:0.#} kg",
        CinturaCm is null ? null : $"cintura {CinturaCm:0.#} cm",
    }.Where(x => x is not null));
}

public record PerfilEntrenamiento(string? ObjetivoPrincipal, string? NivelExperiencia, List<string> TiposPreferidos, List<string> AreasPrioritarias, int? DiasDisponibles);
public record Restriccion(string Tipo, string? Descripcion, List<string> AreasAfectadas, List<string> MovimientosEvitar, bool RequiereAprobacion, bool Activa);
public record Contacto(int Id, string NombreCompleto, string Telefono, string Relacion, bool EsPrincipal)
{
    [JsonIgnore] public string Detalle => $"{Relacion} · {Telefono}{(EsPrincipal ? " · principal" : "")}";
}

// ── Personal, asesorías y nutrición ─────────────────────────────────────

public record Asesor(int Id, string NombreCompleto, string Rol, int? SucursalId, string? Sucursal, string? Cadena, string Especialidad, string? AvatarUrl)
{
    [JsonIgnore] public string Detalle => $"{Textos.Rol(Rol)} · {Sucursal ?? Cadena ?? ""}";
}

public record Asesoria(
    int Id, int ClienteId, string Cliente, string? TelefonoCliente, string? CiCliente, string? EmailCliente, int AsesorId, string Asesor,
    string RolAsesor, string? SucursalAsesor, string Estado, int? MetaSesionesSemana, DateTime CreadaEn)
{
    [JsonIgnore] public string EstadoTexto => Textos.Estado(Estado);
    [JsonIgnore] public Color ColorEstado => Textos.ColorEstado(Estado);
    [JsonIgnore] public string AsesorTexto => $"{Asesor} · {Textos.Rol(RolAsesor)}";
    [JsonIgnore] public string ClienteTexto => $"{EmailCliente} · {TelefonoCliente ?? "sin teléfono"}";
    [JsonIgnore] public bool Cancelable => Estado is "PENDIENTE" or "ACTIVA";
}

public record ActividadAlumno(int AsesoriaId, int ClienteId, string Cliente, DateTime? UltimaSesion, int Sesiones, int SesionesSemana, int? MetaSesionesSemana, bool Inactivo)
{
    [JsonIgnore] public string Detalle => $"Esta semana {SesionesSemana}{(MetaSesionesSemana is null ? "" : $"/{MetaSesionesSemana}")} sesiones · última: {(UltimaSesion is null ? "nunca" : Hora.FechaHora(UltimaSesion.Value))}";
    [JsonIgnore] public string Alerta => Inactivo ? "Inactivo" : "Al día";
    [JsonIgnore] public Color ColorAlerta => Inactivo ? Colores.Peligro : Colores.Exito;
}

public record PanelAsesor(int ClientesActivos, int SolicitudesPendientes, int CitasHoy);
public record SesionResumen(int Id, DateTime Inicio, string Actividad, int? DuracionSeg, int Series)
{
    [JsonIgnore] public string Texto => $"{Hora.FechaHora(Inicio)} · {Actividad} · {Series} series";
}

public record FichaCliente(int ClienteId, Perfil Perfil, Metrica? UltimaMetrica, Restriccion? Restriccion, PerfilEntrenamiento? Entrenamiento, List<SesionResumen> UltimasSesiones);

public record PlanNutricional(
    int Id, int AsesorId, string Asesor, int ClienteId, string Cliente, int? KcalDiarias, decimal? ProteinaG, decimal? CarbohidratosG, decimal? GrasaG,
    string? Notas, Dictionary<string, Dictionary<string, string>> PlanComidas, DateTime ActualizadoEn)
{
    [JsonIgnore] public string Macros => $"{KcalDiarias ?? 0} kcal · P {ProteinaG ?? 0:0} g · C {CarbohidratosG ?? 0:0} g · G {GrasaG ?? 0:0} g";
}

public record Cita(int Id, int PacienteId, string Paciente, string? AvatarUrl, DateOnly Fecha, TimeOnly Hora, string Tipo, string Estado, string? Notas)
{
    [JsonIgnore] public string Cuando => $"{Modelos.Hora.Fecha(Fecha)} · {Hora:HH\\:mm} · {Textos.Estado(Tipo)}";
    [JsonIgnore] public string EstadoTexto => Textos.Estado(Estado);
    [JsonIgnore] public Color ColorEstado => Textos.ColorEstado(Estado);
}

public record Turno(int Id, int SucursalId, string Sucursal, int DiaSemana, string Dia, TimeOnly Inicio, TimeOnly Fin)
{
    [JsonIgnore] public string Texto => $"{Textos.Dia(Dia)} · {Inicio:HH\\:mm} – {Fin:HH\\:mm} · {Sucursal}";
}

public record Participante(int ReservaId, int UsuarioId, string NombreCompleto, string? AvatarUrl, string Estado)
{
    [JsonIgnore] public string EstadoTexto => Textos.Estado(Estado);
    [JsonIgnore] public Color ColorEstado => Textos.ColorEstado(Estado);
    [JsonIgnore] public bool Pendiente => Estado is "CONFIRMADA" or "PENDIENTE";
}

public record ClaseAgenda(int HorarioId, string Actividad, string Sucursal, string DiaSemana, TimeOnly Inicio, TimeOnly Fin, int CupoMaximo, int Inscritos, bool Terminada, List<Participante> Participantes)
{
    [JsonIgnore] public string Detalle => $"{Textos.Dia(DiaSemana)} {Inicio:HH\\:mm} – {Fin:HH\\:mm} · {Sucursal}";
    [JsonIgnore] public string Cupo => $"{Inscritos}/{CupoMaximo} inscritos{(Terminada ? " · terminada" : "")}";
}

public record EstadisticaClase(int HorarioId, string Actividad, string DiaSemana, TimeOnly Inicio, int Asistencias30Dias)
{
    [JsonIgnore] public string Texto => $"{Textos.Dia(DiaSemana)} {Inicio:HH\\:mm} · {Asistencias30Dias} asistencias en 30 días";
}

// ── Notificaciones, mensajes y reportes ─────────────────────────────────

public record Notificacion(int Id, string Tipo, string Titulo, string Cuerpo, DateTime EnviadaEn, bool Leida)
{
    [JsonIgnore] public string Fecha => Hora.FechaHora(EnviadaEn);
    [JsonIgnore] public FontAttributes Peso => Leida ? FontAttributes.None : FontAttributes.Bold;
}

public record Preferencias(bool Push, bool ConfirmacionesReserva, bool RecordatoriosClase, bool AlertasCancelacion, bool Promociones);

public record Conversacion(int Id, int OtroUsuarioId, string OtroUsuario, string? AvatarUrl, string? UltimoMensaje, DateTime? UltimoEn, int SinLeer)
{
    [JsonIgnore] public string Fecha => UltimoEn is null ? "" : Hora.FechaHora(UltimoEn.Value);
    [JsonIgnore] public bool TieneSinLeer => SinLeer > 0;
}

public record Mensaje(int Id, int ConversacionId, int RemitenteId, bool EsMio, string Contenido, bool Eliminado, DateTime CreadoEn, DateTime? LeidoEn)
{
    [JsonIgnore] public LayoutOptions Alineacion => EsMio ? LayoutOptions.End : LayoutOptions.Start;
    [JsonIgnore] public Color Fondo => EsMio ? Colores.Primario : Colores.Gris;
    [JsonIgnore] public Color ColorTexto => EsMio ? Colors.White : Colores.TextoClaro;
    [JsonIgnore] public string Texto => Eliminado ? "Mensaje eliminado" : Contenido;
    [JsonIgnore] public string Fecha => Hora.Local(CreadoEn).ToString("dd/MM HH:mm") + (EsMio && LeidoEn is not null ? " · visto" : "");
}

public record PuntoSerie(string Etiqueta, double Valor);
public record SerieReporte(string Titulo, List<PuntoSerie> Puntos);
public record ResumenPanel(int UsuariosActivos, int IngresosHoy, int ReservasHoy, int SociosConSuscripcion, int PersonasDentro, SerieReporte Usuarios, SerieReporte Ingresos, SerieReporte Reservas);

/// <summary>Listado paginado de la API (auditoría de accesos, reservas).</summary>
public record PaginaApi<T>(List<T> Datos, int Total, int Limite, int Desde);

/// <summary>Opción simple para listas de selección (id + texto).</summary>
public record Opcion(int Id, string Texto)
{
    public override string ToString() => Texto;
}
