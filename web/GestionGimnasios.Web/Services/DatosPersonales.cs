using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

/// <summary>
/// Quién puede ver los datos deportivos de una persona (decisión 4.2): la propia persona, su
/// asesor con asesoría activa, el super administrador y el personal administrativo de una
/// cadena donde la persona tiene membresía o rol.
/// </summary>
public class DatosPersonales(AppDbContext db, UsuarioActual actual)
{
    public async Task<bool> PuedeVerAsync(int usuarioId)
    {
        if (usuarioId == actual.Id || actual.EsSuperAdmin) return true;
        if (await db.Asesorias.AnyAsync(a => a.ClienteId == usuarioId && a.AsesorId == actual.Id && a.Estado == EstadosAsesoria.Activa))
            return true;
        if (actual.EsPersonal && actual.Nivel >= Niveles.Recepcionista && actual.CadenaId is int cadena)
        {
            var sucursal = actual.SucursalFiltro;
            return await db.Membresias.IgnoreQueryFilters().AnyAsync(m => m.UsuarioId == usuarioId && m.CadenaId == cadena && (sucursal == null || m.SucursalId == sucursal))
                   || await db.UsuarioRoles.IgnoreQueryFilters().AnyAsync(r => r.UsuarioId == usuarioId && (r.CadenaId == cadena || r.Sucursal!.CadenaId == cadena) && (sucursal == null || r.SucursalId == sucursal));
        }
        return false;
    }

    public async Task ExigirAsync(int usuarioId)
    {
        if (!await PuedeVerAsync(usuarioId)) throw ErrorNegocio.Prohibido("No tienes acceso a los datos de este usuario.");
    }

    /// <summary>Asesor con relación activa con el cliente (o super administrador).</summary>
    public async Task ExigirAsesorAsync(int clienteId)
    {
        if (actual.EsSuperAdmin) return;
        if (!await db.Asesorias.AnyAsync(a => a.ClienteId == clienteId && a.AsesorId == actual.Id && a.Estado == EstadosAsesoria.Activa))
            throw ErrorNegocio.Prohibido("No tienes una relación activa con este cliente.");
    }
}
