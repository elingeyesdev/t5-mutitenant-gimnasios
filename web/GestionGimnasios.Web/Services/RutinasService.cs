using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record RutinaEjercicioDto(
    int Id, int EjercicioId, string Ejercicio, string GrupoMuscular, string? YoutubeVideoId, string TipoRegistro,
    int Orden, int? Series, string? Repeticiones, decimal? PesoKg, string? DiaSemana, int? DescansoSeg, string? Notas);

public record RutinaDto(
    int Id, string Nombre, string? Descripcion, string Dificultad, int? DuracionSemanas, bool EsPlantilla, bool Activa,
    int EntrenadorId, string? Entrenador, int? AsignadaAId, string? AsignadaA, int? SucursalId, string? Sucursal,
    DateTime CreadaEn, IReadOnlyList<RutinaEjercicioDto> Ejercicios);

public class RutinaEjercicioForm
{
    public int EjercicioId { get; set; }
    public int? Orden { get; set; }
    public int? Series { get; set; }
    public string? Repeticiones { get; set; }
    public decimal? PesoKg { get; set; }
    public string? DiaSemana { get; set; }
    public int? DescansoSeg { get; set; }
    public string? Notas { get; set; }
}

public class RutinaForm
{
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Dificultad { get; set; } = "PRINCIPIANTE";
    public int? DuracionSemanas { get; set; }
    public bool EsPlantilla { get; set; }
    public int? AsignadaAId { get; set; }
    public int? SucursalId { get; set; }
    public List<RutinaEjercicioForm> Ejercicios { get; set; } = [];
}

/// <summary>Rutinas de entrenamiento y su asignación a alumnos (CU-12).</summary>
public class RutinasService(AppDbContext db, UsuarioActual actual, DatosPersonales datos)
{
    public async Task<List<RutinaDto>> ListarAsync(bool? plantillas = null)
    {
        var q = db.Rutinas.Where(r => r.Activa);
        if (plantillas is not null) q = q.Where(r => r.EsPlantilla == plantillas);
        q = FiltrarAlcance(q, incluirPlantillas: plantillas == true);
        return (await Consulta(q).OrderByDescending(r => r.CreadaEn).ToListAsync()).Select(ADto).ToList();
    }

    /// <summary>Rutinas activas asignadas a un usuario (la más reciente primero).</summary>
    public async Task<List<RutinaDto>> DeUsuarioAsync(int usuarioId)
    {
        await datos.ExigirAsync(usuarioId);
        var q = db.Rutinas.Where(r => r.AsignadaAId == usuarioId && r.Activa);
        return (await Consulta(q).OrderByDescending(r => r.CreadaEn).ToListAsync()).Select(ADto).ToList();
    }

    public async Task<List<RutinaDto>> DeEntrenadorAsync(int entrenadorId)
    {
        var q = FiltrarAlcance(db.Rutinas.Where(r => r.EntrenadorId == entrenadorId && r.Activa), false);
        return (await Consulta(q).OrderByDescending(r => r.CreadaEn).ToListAsync()).Select(ADto).ToList();
    }

    /// <summary>Alumnos con rutina activa del entrenador (getMyStudents).</summary>
    public async Task<List<object>> MisAlumnosAsync() =>
        await db.Rutinas.Where(r => r.EntrenadorId == actual.Id && r.AsignadaAId != null && r.Activa)
            .Select(r => r.AsignadaA!).Distinct()
            .Select(u => (object)new { id = u.Id, email = u.Email, activo = u.Activo, nombres = u.Perfil!.Nombres, apellidos = u.Perfil.Apellidos, avatarUrl = u.Perfil.AvatarUrl })
            .ToListAsync();

    public async Task<RutinaDto> ObtenerAsync(int id)
    {
        var r = await Consulta(db.Rutinas).FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado($"Rutina {id} no encontrada");
        if (!await PuedeVerAsync(r)) throw ErrorNegocio.Prohibido("No tiene permisos para acceder a esta rutina");
        return ADto(r);
    }

    public async Task<RutinaDto> CrearAsync(RutinaForm f)
    {
        await Validar(f);
        var (cadenaId, sucursalId) = await AlcanceNuevaRutina(f.SucursalId);
        if (f.AsignadaAId is int alumno)
        {
            await ExigirAlumnoAsync(alumno);
            // Un alumno tiene una sola rutina activa: la nueva reemplaza a la anterior.
            await db.Rutinas.Where(r => r.AsignadaAId == alumno && r.Activa).ExecuteUpdateAsync(s => s.SetProperty(r => r.Activa, false));
        }
        var rutina = new Rutina
        {
            Nombre = f.Nombre.Trim(), Descripcion = f.Descripcion, Dificultad = f.Dificultad, DuracionSemanas = f.DuracionSemanas,
            EsPlantilla = f.EsPlantilla, AsignadaAId = f.AsignadaAId, EntrenadorId = actual.Id, CadenaId = cadenaId, SucursalId = sucursalId,
            Ejercicios = AEjercicios(f.Ejercicios),
        };
        db.Rutinas.Add(rutina);
        await db.SaveChangesAsync();
        return await ObtenerAsync(rutina.Id);
    }

    public async Task<RutinaDto> ActualizarAsync(int id, RutinaForm f)
    {
        await Validar(f);
        var r = await db.Rutinas.Include(x => x.Ejercicios).FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado($"Rutina {id} no encontrada");
        await ExigirGestionAsync(r);
        if (f.SucursalId is not null && f.SucursalId != r.SucursalId && actual.SucursalFiltro is not null)
            throw ErrorNegocio.Prohibido("No puede mover la rutina a otra sucursal");
        if (f.AsignadaAId is int alumno && alumno != r.AsignadaAId) await ExigirAlumnoAsync(alumno);

        r.Nombre = f.Nombre.Trim();
        r.Descripcion = f.Descripcion;
        r.Dificultad = f.Dificultad;
        r.DuracionSemanas = f.DuracionSemanas;
        r.EsPlantilla = f.EsPlantilla;
        r.AsignadaAId = f.AsignadaAId;
        r.ActualizadaEn = DateTime.UtcNow;
        db.RutinaEjercicios.RemoveRange(r.Ejercicios);
        r.Ejercicios = AEjercicios(f.Ejercicios);
        await db.SaveChangesAsync();
        return await ObtenerAsync(id);
    }

    public async Task EliminarAsync(int id)
    {
        var r = await db.Rutinas.FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado($"Rutina {id} no encontrada");
        await ExigirGestionAsync(r);
        if (await db.Sesiones.AnyAsync(s => s.RutinaId == id))
        {
            r.Activa = false; // conserva el historial de sesiones hechas con esta rutina
        }
        else
        {
            db.Rutinas.Remove(r);
        }
        await db.SaveChangesAsync();
    }

    // ── Apoyo ────────────────────────────────────────────────────────────

    private IQueryable<Rutina> FiltrarAlcance(IQueryable<Rutina> q, bool incluirPlantillas)
    {
        if (actual.EsSuperAdmin) return q;
        if (actual.EsCliente) return q.Where(r => r.AsignadaAId == actual.Id || (incluirPlantillas && r.EsPlantilla));
        var cadena = actual.CadenaId;
        var sucursal = actual.SucursalFiltro;
        return q.Where(r => (incluirPlantillas && r.EsPlantilla) || (r.CadenaId == cadena && (sucursal == null || r.SucursalId == sucursal)));
    }

    private async Task<bool> PuedeVerAsync(Rutina r)
    {
        if (actual.EsSuperAdmin || r.EntrenadorId == actual.Id || r.EsPlantilla) return true;
        if (r.AsignadaAId is int alumno && await datos.PuedeVerAsync(alumno)) return true;
        return actual.EsPersonal && r.CadenaId == actual.CadenaId && (actual.SucursalFiltro == null || r.SucursalId == actual.SucursalFiltro);
    }

    private async Task ExigirGestionAsync(Rutina r)
    {
        if (actual.EsSuperAdmin || r.EntrenadorId == actual.Id) return;
        if (actual.Nivel >= Niveles.Recepcionista && r.CadenaId == actual.CadenaId && (actual.SucursalFiltro == null || r.SucursalId == actual.SucursalFiltro)) return;
        await Task.CompletedTask;
        throw ErrorNegocio.Prohibido("No tiene permisos para gestionar esta rutina");
    }

    /// <summary>El entrenador asigna rutinas a sus alumnos (asesoría activa); el personal administrativo a los socios de su alcance.</summary>
    private async Task ExigirAlumnoAsync(int alumnoId)
    {
        if (actual.Nivel < Niveles.Recepcionista) await datos.ExigirAsesorAsync(alumnoId);
        else await datos.ExigirAsync(alumnoId);
    }

    private async Task<(int?, int?)> AlcanceNuevaRutina(int? sucursalSolicitada)
    {
        if (actual.EsSuperAdmin)
        {
            if (sucursalSolicitada is null) return (null, null);
            var s = await db.Sucursales.FirstOrDefaultAsync(x => x.Id == sucursalSolicitada) ?? throw ErrorNegocio.NoEncontrado("Sucursal no encontrada.");
            return (s.CadenaId, s.Id);
        }
        if (actual.SucursalFiltro is int propia)
        {
            if (sucursalSolicitada is not null && sucursalSolicitada != propia) throw ErrorNegocio.Prohibido("No puede crear rutinas para otra sucursal");
            return (actual.CadenaId, propia);
        }
        if (sucursalSolicitada is not null && !await db.Sucursales.AnyAsync(s => s.Id == sucursalSolicitada))
            throw ErrorNegocio.Prohibido("No puede crear rutinas para otra sucursal");
        return (actual.CadenaId, sucursalSolicitada);
    }

    private async Task Validar(RutinaForm f)
    {
        if (string.IsNullOrWhiteSpace(f.Nombre)) throw ErrorNegocio.Solicitud("El nombre de la rutina es obligatorio.");
        var ids = f.Ejercicios.Select(e => e.EjercicioId).Distinct().ToList();
        var existentes = await db.Ejercicios.CountAsync(e => ids.Contains(e.Id));
        if (existentes != ids.Count) throw ErrorNegocio.Solicitud("Uno o más ejercicios no existen en el catálogo.");
    }

    private static List<RutinaEjercicio> AEjercicios(List<RutinaEjercicioForm> items) =>
        items.Select((e, i) => new RutinaEjercicio
        {
            EjercicioId = e.EjercicioId, Orden = e.Orden ?? i, Series = e.Series, Repeticiones = e.Repeticiones,
            PesoKg = e.PesoKg, DiaSemana = e.DiaSemana, DescansoSeg = e.DescansoSeg, Notas = e.Notas,
        }).ToList();

    private static IQueryable<Rutina> Consulta(IQueryable<Rutina> q) =>
        q.Include(r => r.Entrenador).ThenInclude(u => u!.Perfil)
            .Include(r => r.AsignadaA).ThenInclude(u => u!.Perfil)
            .Include(r => r.Sucursal)
            .Include(r => r.Ejercicios).ThenInclude(e => e.Ejercicio)
            .AsSplitQuery()
            .IgnoreQueryFilters();

    public static RutinaDto ADto(Rutina r) => new(
        r.Id, r.Nombre, r.Descripcion, r.Dificultad, r.DuracionSemanas, r.EsPlantilla, r.Activa,
        r.EntrenadorId, r.Entrenador?.Perfil?.NombreCompleto, r.AsignadaAId, r.AsignadaA?.Perfil?.NombreCompleto,
        r.SucursalId, r.Sucursal?.Nombre, r.CreadaEn,
        r.Ejercicios.OrderBy(e => e.Orden).Select(e => new RutinaEjercicioDto(
            e.Id, e.EjercicioId, e.Ejercicio?.Nombre ?? "", e.Ejercicio?.GrupoMuscular ?? "", e.Ejercicio?.YoutubeVideoId,
            e.Ejercicio?.TipoRegistro ?? TiposRegistroEjercicio.PesoRepeticiones,
            e.Orden, e.Series, e.Repeticiones, e.PesoKg, e.DiaSemana, e.DescansoSeg, e.Notas)).ToList());
}
