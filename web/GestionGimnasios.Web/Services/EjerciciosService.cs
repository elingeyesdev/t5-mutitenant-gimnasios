using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record EjercicioDto(
    int Id, string Nombre, string? Descripcion, string GrupoMuscular, string? Categoria, string? Tipo,
    IReadOnlyList<string> GruposSecundarios, string? Equipo, string Dificultad, string? YoutubeVideoId,
    string? ImagenUrl, string TipoRegistro, bool Activo);

public class EjercicioForm
{
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string GrupoMuscular { get; set; } = "";
    public string? Categoria { get; set; }
    public string? Tipo { get; set; }
    public List<string> GruposSecundarios { get; set; } = [];
    public string? Equipo { get; set; }
    public string Dificultad { get; set; } = "PRINCIPIANTE";
    public string? YoutubeVideoId { get; set; }
    public string TipoRegistro { get; set; } = TiposRegistroEjercicio.PesoRepeticiones;
    public bool Activo { get; set; } = true;
}

/// <summary>Catálogo global de ejercicios (CU-11).</summary>
public class EjerciciosService(AppDbContext db, Almacenamiento archivos)
{
    public static readonly string[] Dificultades = ["PRINCIPIANTE", "INTERMEDIO", "AVANZADO"];
    public static readonly string[] TiposRegistro =
        [TiposRegistroEjercicio.PesoRepeticiones, TiposRegistroEjercicio.SoloRepeticiones, TiposRegistroEjercicio.TiempoDistancia, TiposRegistroEjercicio.SoloTiempo];

    public async Task<List<EjercicioDto>> ListarAsync(string? grupo = null, string? dificultad = null, string? categoria = null, string? tipo = null, string? buscar = null, bool incluirInactivos = false)
    {
        var q = db.Ejercicios.AsQueryable();
        if (!incluirInactivos) q = q.Where(e => e.Activo);
        if (!string.IsNullOrWhiteSpace(grupo)) q = q.Where(e => e.GrupoMuscular.Contains(grupo));
        if (!string.IsNullOrWhiteSpace(dificultad)) q = q.Where(e => e.Dificultad == dificultad);
        if (!string.IsNullOrWhiteSpace(categoria)) q = q.Where(e => e.Categoria == categoria);
        if (!string.IsNullOrWhiteSpace(tipo)) q = q.Where(e => e.Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(buscar)) q = q.Where(e => e.Nombre.Contains(buscar));
        return (await q.OrderBy(e => e.Nombre).ToListAsync()).Select(ADto).ToList();
    }

    public async Task<List<string>> GruposAsync() =>
        await db.Ejercicios.Where(e => e.Activo).Select(e => e.GrupoMuscular).Distinct().OrderBy(g => g).ToListAsync();

    public async Task<EjercicioDto> ObtenerAsync(int id) => ADto(await Buscar(id));

    public async Task<EjercicioDto> CrearAsync(EjercicioForm f)
    {
        Validar(f);
        if (await db.Ejercicios.AnyAsync(e => e.Nombre == f.Nombre.Trim())) throw ErrorNegocio.Conflicto("Ya existe un ejercicio con ese nombre.");
        var e = new Ejercicio();
        Copiar(f, e);
        db.Ejercicios.Add(e);
        await db.SaveChangesAsync();
        return ADto(e);
    }

    public async Task<EjercicioDto> ActualizarAsync(int id, EjercicioForm f)
    {
        Validar(f);
        var e = await Buscar(id);
        if (await db.Ejercicios.AnyAsync(x => x.Nombre == f.Nombre.Trim() && x.Id != id)) throw ErrorNegocio.Conflicto("Ya existe un ejercicio con ese nombre.");
        Copiar(f, e);
        e.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ADto(e);
    }

    public async Task<EjercicioDto> ActualizarImagenAsync(int id, IFormFile archivo)
    {
        var e = await Buscar(id);
        archivos.Eliminar(e.ImagenUrl);
        e.ImagenUrl = await archivos.GuardarImagenAsync(archivo, "ejercicios");
        await db.SaveChangesAsync();
        return ADto(e);
    }

    public async Task<EjercicioDto> EliminarImagenAsync(int id)
    {
        var e = await Buscar(id);
        archivos.Eliminar(e.ImagenUrl);
        e.ImagenUrl = null;
        await db.SaveChangesAsync();
        return ADto(e);
    }

    public async Task EliminarAsync(int id)
    {
        var e = await Buscar(id);
        if (await db.RutinaEjercicios.AnyAsync(r => r.EjercicioId == id))
        {
            // Está en rutinas: se desactiva para no romper rutinas existentes.
            e.Activo = false;
            await db.SaveChangesAsync();
            return;
        }
        archivos.Eliminar(e.ImagenUrl);
        await db.Series.Where(s => s.EjercicioId == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EjercicioId, (int?)null));
        db.Ejercicios.Remove(e);
        await db.SaveChangesAsync();
    }

    private async Task<Ejercicio> Buscar(int id) =>
        await db.Ejercicios.FindAsync(id) ?? throw ErrorNegocio.NoEncontrado($"Ejercicio {id} no encontrado");

    private static void Validar(EjercicioForm f)
    {
        if (string.IsNullOrWhiteSpace(f.Nombre)) throw ErrorNegocio.Solicitud("El nombre del ejercicio es obligatorio.");
        if (string.IsNullOrWhiteSpace(f.GrupoMuscular)) throw ErrorNegocio.Solicitud("El grupo muscular es obligatorio.");
        if (!Dificultades.Contains(f.Dificultad)) throw ErrorNegocio.Solicitud("Dificultad no válida.");
        if (!TiposRegistro.Contains(f.TipoRegistro)) throw ErrorNegocio.Solicitud("Tipo de registro no válido.");
    }

    private static void Copiar(EjercicioForm f, Ejercicio e)
    {
        e.Nombre = f.Nombre.Trim();
        e.Descripcion = f.Descripcion;
        e.GrupoMuscular = f.GrupoMuscular.Trim();
        e.Categoria = f.Categoria;
        e.Tipo = f.Tipo;
        e.GruposSecundarios = f.GruposSecundarios;
        e.Equipo = f.Equipo;
        e.Dificultad = f.Dificultad;
        e.YoutubeVideoId = string.IsNullOrWhiteSpace(f.YoutubeVideoId) ? null : f.YoutubeVideoId.Trim();
        e.TipoRegistro = f.TipoRegistro;
        e.Activo = f.Activo;
    }

    public static EjercicioDto ADto(Ejercicio e) => new(
        e.Id, e.Nombre, e.Descripcion, e.GrupoMuscular, e.Categoria, e.Tipo, e.GruposSecundarios, e.Equipo,
        e.Dificultad, e.YoutubeVideoId, e.ImagenUrl, e.TipoRegistro, e.Activo);
}
