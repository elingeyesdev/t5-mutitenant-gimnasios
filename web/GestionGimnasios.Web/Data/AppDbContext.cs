using System.Linq.Expressions;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, UsuarioActual? usuario = null) : DbContext(options)
{
    /// <summary>
    /// Cadena del usuario actual. EF Core la evalúa en cada consulta: los datos operativos de
    /// otras cadenas no se devuelven aunque un servicio olvide filtrarlos (decisión 4.1).
    /// </summary>
    public int? CadenaFiltro => usuario?.CadenaFiltro;

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<PerfilUsuario> Perfiles => Set<PerfilUsuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<Membresia> Membresias => Set<Membresia>();
    public DbSet<Cadena> Cadenas => Set<Cadena>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<UbicacionSucursal> Ubicaciones => Set<UbicacionSucursal>();
    public DbSet<HorarioSucursal> HorariosSucursal => Set<HorarioSucursal>();
    public DbSet<Maquina> Maquinas => Set<Maquina>();
    public DbSet<Actividad> Actividades => Set<Actividad>();
    public DbSet<HorarioActividad> HorariosActividad => Set<HorarioActividad>();
    public DbSet<AsistenciaActividad> Asistencias => Set<AsistenciaActividad>();
    public DbSet<Ejercicio> Ejercicios => Set<Ejercicio>();
    public DbSet<Rutina> Rutinas => Set<Rutina>();
    public DbSet<RutinaEjercicio> RutinaEjercicios => Set<RutinaEjercicio>();
    public DbSet<PerfilEntrenamiento> PerfilesEntrenamiento => Set<PerfilEntrenamiento>();
    public DbSet<RestriccionEntrenamiento> Restricciones => Set<RestriccionEntrenamiento>();
    public DbSet<ContactoEmergencia> ContactosEmergencia => Set<ContactoEmergencia>();
    public DbSet<SesionEntrenamiento> Sesiones => Set<SesionEntrenamiento>();
    public DbSet<SerieEntrenamiento> Series => Set<SerieEntrenamiento>();
    public DbSet<MetricaFisica> Metricas => Set<MetricaFisica>();
    public DbSet<PlanSuscripcion> Planes => Set<PlanSuscripcion>();
    public DbSet<Suscripcion> Suscripciones => Set<Suscripcion>();
    public DbSet<PagoSuscripcion> Pagos => Set<PagoSuscripcion>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<EntradaListaEspera> ListaEspera => Set<EntradaListaEspera>();
    public DbSet<Acceso> Accesos => Set<Acceso>();
    public DbSet<Visita> Visitas => Set<Visita>();
    public DbSet<HorarioPersonal> HorariosPersonal => Set<HorarioPersonal>();
    public DbSet<Asesoria> Asesorias => Set<Asesoria>();
    public DbSet<CitaNutricional> Citas => Set<CitaNutricional>();
    public DbSet<PlanNutricional> PlanesNutricionales => Set<PlanNutricional>();
    public DbSet<ReglaInactividad> ReglasInactividad => Set<ReglaInactividad>();
    public DbSet<AlertaInactividad> Alertas => Set<AlertaInactividad>();
    public DbSet<IntervencionAlerta> Intervenciones => Set<IntervencionAlerta>();
    public DbSet<PlantillaNotificacion> Plantillas => Set<PlantillaNotificacion>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<PreferenciaNotificacion> PreferenciasNotificacion => Set<PreferenciaNotificacion>();
    public DbSet<Conversacion> Conversaciones => Set<Conversacion>();
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();
    public DbSet<ParametroSistema> Parametros => Set<ParametroSistema>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Usuario>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.OtpCodigo).HasMaxLength(6);
            e.HasOne(x => x.Perfil).WithOne(x => x.Usuario).HasForeignKey<PerfilUsuario>(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Roles).WithOne(x => x.Usuario).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Membresias).WithOne(x => x.Usuario).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.ContactosEmergencia).WithOne().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<PerfilUsuario>(e =>
        {
            e.HasIndex(x => x.Ci).IsUnique().HasFilter("[Ci] IS NOT NULL");
            e.HasIndex(x => x.NombreUsuario).IsUnique().HasFilter("[NombreUsuario] IS NOT NULL");
            e.Property(x => x.Nombres).HasMaxLength(100);
            e.Property(x => x.Apellidos).HasMaxLength(100);
            e.Property(x => x.Ci).HasMaxLength(20);
            e.Property(x => x.AlturaCm).HasPrecision(5, 2);
            e.Ignore(x => x.NombreCompleto);
        });
        b.Entity<Rol>(e =>
        {
            e.HasIndex(x => x.Nombre).IsUnique();
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.HasMany(x => x.Permisos).WithOne(x => x.Rol).HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Permiso>(e => e.HasIndex(x => x.Codigo).IsUnique());
        b.Entity<UsuarioRol>(e => e.HasIndex(x => new { x.CadenaId, x.SucursalId }));

        b.Entity<Cadena>(e =>
        {
            e.HasIndex(x => x.Nombre).IsUnique();
            e.Property(x => x.Nombre).HasMaxLength(150);
            e.HasMany(x => x.Sucursales).WithOne(x => x.Cadena).HasForeignKey(x => x.CadenaId);
        });
        b.Entity<Sucursal>(e =>
        {
            e.HasIndex(x => new { x.CadenaId, x.Nombre }).IsUnique();
            e.Property(x => x.Nombre).HasMaxLength(150);
            e.HasOne(x => x.Ubicacion).WithOne().HasForeignKey<UbicacionSucursal>(x => x.SucursalId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Horarios).WithOne().HasForeignKey(x => x.SucursalId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Maquinas).WithOne(x => x.Sucursal).HasForeignKey(x => x.SucursalId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Actividades).WithOne(x => x.Sucursal).HasForeignKey(x => x.SucursalId);
        });
        b.Entity<UbicacionSucursal>(e =>
        {
            e.Property(x => x.Latitud).HasPrecision(10, 8);
            e.Property(x => x.Longitud).HasPrecision(11, 8);
        });
        b.Entity<HorarioSucursal>(e => e.Property(x => x.DiaSemana).HasMaxLength(10));
        b.Entity<Maquina>(e =>
        {
            e.HasIndex(x => new { x.SucursalId, x.Estado });
            e.Property(x => x.Estado).HasMaxLength(20);
            e.Property(x => x.Categoria).HasMaxLength(20);
        });

        b.Entity<Actividad>(e => e.HasMany(x => x.Horarios).WithOne(x => x.Actividad).HasForeignKey(x => x.ActividadId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<HorarioActividad>(e =>
        {
            e.Property(x => x.DiaSemana).HasMaxLength(10);
            e.HasMany(x => x.Asistencias).WithOne(x => x.HorarioActividad).HasForeignKey(x => x.HorarioActividadId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Ejercicio>(e =>
        {
            e.HasIndex(x => x.Nombre).IsUnique();
            e.HasIndex(x => x.GrupoMuscular);
            e.Property(x => x.Nombre).HasMaxLength(150);
        });
        b.Entity<Rutina>(e =>
        {
            e.HasOne(x => x.Entrenador).WithMany().HasForeignKey(x => x.EntrenadorId);
            e.HasOne(x => x.AsignadaA).WithMany().HasForeignKey(x => x.AsignadaAId);
            e.HasMany(x => x.Ejercicios).WithOne(x => x.Rutina).HasForeignKey(x => x.RutinaId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<RutinaEjercicio>(e => e.Property(x => x.PesoKg).HasPrecision(6, 2));

        b.Entity<PerfilEntrenamiento>(e => e.HasIndex(x => x.UsuarioId).IsUnique());
        b.Entity<RestriccionEntrenamiento>(e => e.HasIndex(x => x.UsuarioId).IsUnique());
        b.Entity<SesionEntrenamiento>(e =>
        {
            e.HasIndex(x => new { x.UsuarioId, x.Inicio });
            e.HasMany(x => x.Series).WithOne(x => x.Sesion).HasForeignKey(x => x.SesionId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<SerieEntrenamiento>(e => e.Property(x => x.PesoKg).HasPrecision(6, 2));
        b.Entity<MetricaFisica>(e =>
        {
            e.HasIndex(x => new { x.UsuarioId, x.RegistradaEn });
            foreach (var p in new[] { nameof(MetricaFisica.PesoKg), nameof(MetricaFisica.GrasaPct), nameof(MetricaFisica.MasaMuscularKg),
                         nameof(MetricaFisica.CinturaCm), nameof(MetricaFisica.PechoCm), nameof(MetricaFisica.CaderaCm),
                         nameof(MetricaFisica.BrazoCm), nameof(MetricaFisica.MusloCm), nameof(MetricaFisica.PantorrillaCm) })
                e.Property(p).HasPrecision(5, 2);
        });

        b.Entity<PlanSuscripcion>(e =>
        {
            e.HasIndex(x => new { x.CadenaId, x.Nombre }).IsUnique();
            e.Property(x => x.PrecioMensual).HasPrecision(10, 2);
        });
        b.Entity<Suscripcion>(e =>
        {
            e.HasIndex(x => new { x.UsuarioId, x.Estado });
            e.HasMany(x => x.Pagos).WithOne().HasForeignKey(x => x.SuscripcionId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<PagoSuscripcion>(e => e.Property(x => x.Monto).HasPrecision(10, 2));

        b.Entity<Reserva>(e =>
        {
            e.HasIndex(x => new { x.UsuarioId, x.Estado });
            e.HasIndex(x => new { x.HorarioActividadId, x.Fecha });
            e.HasIndex(x => x.CodigoQr).IsUnique().HasFilter("[CodigoQr] IS NOT NULL");
            e.Property(x => x.CodigoQr).HasMaxLength(36);
            e.Property(x => x.Estado).HasMaxLength(20);
        });
        b.Entity<EntradaListaEspera>(e => e.HasIndex(x => new { x.HorarioActividadId, x.Estado, x.Posicion }));
        b.Entity<Acceso>(e =>
        {
            e.HasIndex(x => new { x.SucursalId, x.Ingreso });
            e.HasIndex(x => new { x.UsuarioId, x.Estado });
        });

        b.Entity<HorarioPersonal>(e => e.HasIndex(x => new { x.UsuarioId, x.SucursalId, x.DiaSemana }).IsUnique());
        b.Entity<Asesoria>(e =>
        {
            e.HasIndex(x => new { x.ClienteId, x.AsesorId }).IsUnique();
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId);
            e.HasOne(x => x.Asesor).WithMany().HasForeignKey(x => x.AsesorId);
        });
        b.Entity<CitaNutricional>(e =>
        {
            e.HasOne(x => x.Nutricionista).WithMany().HasForeignKey(x => x.NutricionistaId);
            e.HasOne(x => x.Paciente).WithMany().HasForeignKey(x => x.PacienteId);
        });
        b.Entity<PlanNutricional>(e =>
        {
            e.HasIndex(x => new { x.AsesorId, x.ClienteId }).IsUnique();
            e.Property(x => x.ProteinaG).HasPrecision(6, 1);
            e.Property(x => x.CarbohidratosG).HasPrecision(6, 1);
            e.Property(x => x.GrasaG).HasPrecision(6, 1);
        });
        b.Entity<ReglaInactividad>(e => e.HasIndex(x => x.CadenaId).IsUnique());
        b.Entity<AlertaInactividad>(e =>
        {
            // Una sola alerta abierta por socio y cadena: la detección diaria la actualiza en lugar de duplicarla.
            e.HasIndex(x => new { x.CadenaId, x.UsuarioId }).IsUnique().HasFilter("[Estado] <> 'CERRADA'");
            e.HasIndex(x => new { x.CadenaId, x.Estado, x.Nivel });
            e.Property(x => x.Nivel).HasMaxLength(10);
            e.Property(x => x.Estado).HasMaxLength(20);
            e.HasMany(x => x.Intervenciones).WithOne().HasForeignKey(x => x.AlertaId);
        });
        b.Entity<IntervencionAlerta>(e =>
        {
            e.Property(x => x.Tipo).HasMaxLength(20);
            e.Property(x => x.Nota).HasMaxLength(500);
        });

        b.Entity<PlantillaNotificacion>(e => e.HasIndex(x => x.Tipo).IsUnique());
        b.Entity<Notificacion>(e => e.HasIndex(x => new { x.UsuarioId, x.EnviadaEn }));
        b.Entity<PreferenciaNotificacion>(e => e.HasIndex(x => x.UsuarioId).IsUnique());
        b.Entity<Conversacion>(e =>
        {
            e.HasIndex(x => new { x.ClienteId, x.AsesorId }).IsUnique();
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId);
            e.HasOne(x => x.Asesor).WithMany().HasForeignKey(x => x.AsesorId);
            e.HasMany(x => x.Mensajes).WithOne().HasForeignKey(x => x.ConversacionId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ParametroSistema>(e => e.HasIndex(x => x.Clave).IsUnique());

        // SQL Server no admite varias rutas de borrado en cascada: solo se borra en cascada
        // dentro de cada agregado; el resto de relaciones se protege con Restrict.
        var cascadas = new HashSet<(Type, Type)>
        {
            (typeof(PerfilUsuario), typeof(Usuario)), (typeof(UsuarioRol), typeof(Usuario)),
            (typeof(Membresia), typeof(Usuario)), (typeof(ContactoEmergencia), typeof(Usuario)),
            (typeof(RolPermiso), typeof(Rol)), (typeof(UbicacionSucursal), typeof(Sucursal)),
            (typeof(HorarioSucursal), typeof(Sucursal)), (typeof(Maquina), typeof(Sucursal)),
            (typeof(HorarioActividad), typeof(Actividad)), (typeof(AsistenciaActividad), typeof(HorarioActividad)),
            (typeof(RutinaEjercicio), typeof(Rutina)), (typeof(SerieEntrenamiento), typeof(SesionEntrenamiento)),
            (typeof(PagoSuscripcion), typeof(Suscripcion)), (typeof(Mensaje), typeof(Conversacion)),
        };
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = cascadas.Contains((fk.DeclaringEntityType.ClrType, fk.PrincipalEntityType.ClrType))
                ? DeleteBehavior.Cascade
                : DeleteBehavior.Restrict;

        // Filtro global por cadena en toda entidad IDeCadena (multi-tenant).
        foreach (var tipo in b.Model.GetEntityTypes().Where(t => typeof(IDeCadena).IsAssignableFrom(t.ClrType)))
        {
            var metodo = typeof(AppDbContext).GetMethod(nameof(FiltroCadena), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(tipo.ClrType);
            b.Entity(tipo.ClrType).HasQueryFilter((LambdaExpression)metodo.Invoke(this, null)!);
        }
    }

    private LambdaExpression FiltroCadena<T>() where T : class, IDeCadena
    {
        Expression<Func<T, bool>> filtro = e => CadenaFiltro == null || e.CadenaId == CadenaFiltro;
        return filtro;
    }
}
