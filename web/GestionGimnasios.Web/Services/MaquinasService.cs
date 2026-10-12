using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record MaquinaDto(Guid Id, int SucursalId, string Sucursal, string Nombre, string Estado, string Categoria, string? ImagenUrl, DateTime CreadaEn);

public class MaquinaForm
{
    public int SucursalId { get; set; }
    public string Nombre { get; set; } = "";
    public string Estado { get; set; } = EstadosMaquina.Disponible;
    public string Categoria { get; set; } = CategoriasMaquina.Multiestacion;
}

public class MaquinasService(AppDbContext db, UsuarioActual usuario, Alcances alcances, Almacenamiento archivos)
{
    public async Task<List<MaquinaDto>> ListarAsync(int? sucursalId, string? estado)
    {
        IQueryable<Maquina> q;
        if (usuario.Nivel < Niveles.Recepcionista)
        {
            // Clientes y personal de sala consultan las máquinas de una sucursal concreta.
            if (sucursalId is null) throw ErrorNegocio.Prohibido("Se requiere el parámetro sucursalId para consultar máquinas.");
            q = db.Maquinas.Where(m => m.SucursalId == sucursalId);
        }
        else
        {
            var visibles = alcances.SucursalesGestionables().Select(s => s.Id);
            q = db.Maquinas.Where(m => visibles.Contains(m.SucursalId));
            if (sucursalId is not null) q = q.Where(m => m.SucursalId == sucursalId);
        }
        if (!string.IsNullOrEmpty(estado)) q = q.Where(m => m.Estado == estado.ToUpper());
        return await q.OrderBy(m => m.Sucursal!.Nombre).ThenBy(m => m.Nombre)
            .Select(m => new MaquinaDto(m.Id, m.SucursalId, m.Sucursal!.Nombre, m.Nombre, m.Estado, m.Categoria, m.ImagenUrl, m.CreadaEn))
            .ToListAsync();
    }

    public async Task<MaquinaDto> ObtenerAsync(Guid id)
    {
        var m = await Buscar(id);
        return ADto(m);
    }

    public async Task<MaquinaDto> CrearAsync(MaquinaForm f)
    {
        var sucursalId = alcances.ForzarSucursal(f.SucursalId);
        var s = await alcances.ExigirSucursalAsync(sucursalId);
        Validar(f);
        var m = new Maquina { CadenaId = s.CadenaId, SucursalId = s.Id, Nombre = f.Nombre.Trim(), Estado = f.Estado, Categoria = f.Categoria };
        db.Maquinas.Add(m);
        await db.SaveChangesAsync();
        m.Sucursal = s;
        return ADto(m);
    }

    public async Task<MaquinaDto> ActualizarAsync(Guid id, MaquinaForm f)
    {
        var m = await Buscar(id);
        await alcances.ExigirSucursalAsync(m.SucursalId);
        Validar(f);
        if (f.SucursalId != 0 && f.SucursalId != m.SucursalId)
        {
            var destino = await alcances.ExigirSucursalAsync(alcances.ForzarSucursal(f.SucursalId));
            m.SucursalId = destino.Id;
            m.Sucursal = destino;
        }
        m.Nombre = f.Nombre.Trim();
        m.Estado = f.Estado;
        m.Categoria = f.Categoria;
        m.ActualizadaEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ADto(m);
    }

    public async Task<MaquinaDto> ActualizarImagenAsync(Guid id, IFormFile archivo)
    {
        var m = await Buscar(id);
        await alcances.ExigirSucursalAsync(m.SucursalId);
        archivos.Eliminar(m.ImagenUrl);
        m.ImagenUrl = await archivos.GuardarImagenAsync(archivo, "maquinas");
        m.ActualizadaEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ADto(m);
    }

    public async Task<MaquinaDto> EliminarImagenAsync(Guid id)
    {
        var m = await Buscar(id);
        await alcances.ExigirSucursalAsync(m.SucursalId);
        archivos.Eliminar(m.ImagenUrl);
        m.ImagenUrl = null;
        await db.SaveChangesAsync();
        return ADto(m);
    }

    public async Task<string> EliminarAsync(Guid id)
    {
        var m = await Buscar(id);
        await alcances.ExigirSucursalAsync(m.SucursalId);
        archivos.Eliminar(m.ImagenUrl);
        db.Maquinas.Remove(m);
        await db.SaveChangesAsync();
        return $"Máquina \"{m.Nombre}\" eliminada.";
    }

    private async Task<Maquina> Buscar(Guid id)
    {
        var m = await db.Maquinas.Include(x => x.Sucursal).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw ErrorNegocio.NoEncontrado($"Máquina {id} no encontrada.");
        if (usuario.Nivel >= Niveles.Recepcionista && !await alcances.SucursalesGestionables().AnyAsync(s => s.Id == m.SucursalId))
            throw ErrorNegocio.Prohibido("No tienes acceso a esta máquina.");
        return m;
    }

    private static void Validar(MaquinaForm f)
    {
        if (string.IsNullOrWhiteSpace(f.Nombre)) throw ErrorNegocio.Solicitud("El nombre de la máquina es obligatorio.");
        if (f.Estado is not (EstadosMaquina.Disponible or EstadosMaquina.Mantenimiento))
            throw ErrorNegocio.Solicitud("Estado de máquina no válido (DISPONIBLE o MANTENIMIENTO).");
        if (!CategoriasMaquina.Todas.Contains(f.Categoria)) throw ErrorNegocio.Solicitud("Categoría de máquina no válida.");
    }

    private static MaquinaDto ADto(Maquina m) => new(m.Id, m.SucursalId, m.Sucursal?.Nombre ?? "", m.Nombre, m.Estado, m.Categoria, m.ImagenUrl, m.CreadaEn);
}
