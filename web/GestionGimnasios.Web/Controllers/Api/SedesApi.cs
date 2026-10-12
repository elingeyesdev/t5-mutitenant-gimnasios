using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers.Api;

[Route("api/cadenas")]
public class CadenasApi(SedesService sedes) : ApiBase
{
    [HttpGet]
    public Task<List<CadenaDto>> Listar() => sedes.CadenasAsync();

    [HttpPost, Authorize(Politicas.SuperAdmin)]
    public Task<CadenaDto> Crear(CadenaForm f) => sedes.CrearCadenaAsync(f);

    [HttpPut("{id:int}"), Authorize(Politicas.SuperAdmin)]
    public async Task<NoContentResult> Actualizar(int id, CadenaForm f)
    {
        await sedes.ActualizarCadenaAsync(id, f);
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Politicas.SuperAdmin)]
    public async Task<NoContentResult> Eliminar(int id)
    {
        await sedes.EliminarCadenaAsync(id);
        return NoContent();
    }
}

[Route("api/sucursales")]
public class SucursalesApi(SedesService sedes, UsuarioActual actual) : ApiBase
{
    /// <summary>Catálogo para clientes (con lat/lng ordena por distancia) o sucursales del alcance del personal.</summary>
    [HttpGet]
    public Task<List<SucursalDto>> Listar(double? lat, double? lng, double radioKm = 50, int? cadenaId = null) => sedes.SucursalesAsync(lat, lng, radioKm, cadenaId);

    [HttpGet("ubicaciones")]
    public Task<List<object>> Ubicaciones() => sedes.UbicacionesAsync();

    [HttpGet("frecuentes")]
    public Task<List<GimnasioFrecuente>> Frecuentes() => sedes.FrecuentesAsync(actual.Id);

    [HttpGet("estadisticas-hoy"), Authorize(Politicas.Admin)]
    public Task<EstadisticasSucursalHoy> EstadisticasHoy(int? sucursalId) => sedes.EstadisticasHoyAsync(sucursalId);

    [HttpGet("{id:int}")]
    public Task<SucursalDto> Obtener(int id) => sedes.SucursalAsync(id);

    [HttpGet("{id:int}/ocupacion")]
    public Task<object> Ocupacion(int id, string periodo = "day") => sedes.EstadisticasOcupacionAsync(id, periodo);

    [HttpPost, Authorize(Politicas.SuperAdmin)]
    public Task<SucursalDto> Crear(SucursalForm f) => sedes.CrearSucursalAsync(f);

    [HttpPut("{id:int}"), Authorize(Politicas.Admin)]
    public Task<SucursalDto> Actualizar(int id, SucursalForm f) => sedes.ActualizarSucursalAsync(id, f);

    [HttpDelete("{id:int}"), Authorize(Politicas.SuperAdmin)]
    public async Task<NoContentResult> Eliminar(int id)
    {
        await sedes.EliminarSucursalAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}/horarios")]
    public Task<List<HorarioSucursalDto>> Horarios(int id) => sedes.HorariosAsync(id);

    [HttpPost("{id:int}/horarios"), Authorize(Politicas.Admin)]
    public Task<HorarioSucursalDto> AgregarHorario(int id, HorarioSucursalDto h) => sedes.AgregarHorarioAsync(id, h);

    [HttpDelete("horarios/{horarioId:int}"), Authorize(Politicas.Admin)]
    public async Task<NoContentResult> EliminarHorario(int horarioId)
    {
        await sedes.EliminarHorarioAsync(horarioId);
        return NoContent();
    }
}

[Route("api/maquinas")]
public class MaquinasApi(MaquinasService maquinas) : ApiBase
{
    [HttpGet]
    public Task<List<MaquinaDto>> Listar(int? sucursalId, string? estado) => maquinas.ListarAsync(sucursalId, estado);

    [HttpGet("{id:guid}")]
    public Task<MaquinaDto> Obtener(Guid id) => maquinas.ObtenerAsync(id);

    [HttpPost, Authorize(Politicas.Admin)]
    public Task<MaquinaDto> Crear(MaquinaForm f) => maquinas.CrearAsync(f);

    [HttpPut("{id:guid}"), Authorize(Politicas.Admin)]
    public Task<MaquinaDto> Actualizar(Guid id, MaquinaForm f) => maquinas.ActualizarAsync(id, f);

    [HttpPut("{id:guid}/imagen"), Authorize(Politicas.Admin)]
    public Task<MaquinaDto> Imagen(Guid id, IFormFile archivo) => maquinas.ActualizarImagenAsync(id, archivo);

    [HttpDelete("{id:guid}/imagen"), Authorize(Politicas.Admin)]
    public Task<MaquinaDto> QuitarImagen(Guid id) => maquinas.EliminarImagenAsync(id);

    [HttpDelete("{id:guid}"), Authorize(Politicas.Admin)]
    public async Task<object> Eliminar(Guid id) => new { mensaje = await maquinas.EliminarAsync(id) };
}

public record AsistenciaRequest(int UsuarioId);

[Route("api/actividades")]
public class ActividadesApi(ActividadesService actividades) : ApiBase
{
    [HttpGet]
    public Task<List<ActividadDto>> Listar(int? sucursalId, bool incluirInactivas = false) => actividades.ListarAsync(sucursalId, incluirInactivas);

    [HttpGet("instructores"), Authorize(Politicas.Admin)]
    public Task<List<InstructorDto>> Instructores(int? sucursalId) => actividades.InstructoresElegiblesAsync(sucursalId);

    [HttpGet("instructor/{instructorId:int}/horarios")]
    public Task<List<object>> HorariosDeInstructor(int instructorId) => actividades.HorariosDeInstructorAsync(instructorId);

    [HttpGet("{id:int}")]
    public Task<ActividadDto> Obtener(int id) => actividades.ObtenerAsync(id);

    [HttpPost, Authorize(Politicas.Admin)]
    public Task<ActividadDto> Crear(ActividadForm f) => actividades.CrearAsync(f);

    [HttpPut("{id:int}"), Authorize(Politicas.Admin)]
    public Task<ActividadDto> Actualizar(int id, ActividadForm f) => actividades.ActualizarAsync(id, f);

    [HttpDelete("{id:int}"), Authorize(Politicas.Admin)]
    public async Task<object> Eliminar(int id) => new { mensaje = await actividades.EliminarAsync(id) };

    [HttpGet("{id:int}/horarios")]
    public Task<List<HorarioActividadDto>> Horarios(int id) => actividades.HorariosAsync(id);

    [HttpPost("{id:int}/horarios"), Authorize(Politicas.Admin)]
    public Task<HorarioActividadDto> CrearHorario(int id, HorarioActividadForm f) => actividades.CrearHorarioAsync(id, f);

    [HttpDelete("horarios/{horarioId:int}"), Authorize(Politicas.Admin)]
    public async Task<object> EliminarHorario(int horarioId) => new { mensaje = await actividades.EliminarHorarioAsync(horarioId) };

    [HttpPost("horarios/{horarioId:int}/asistencia"), Authorize(Politicas.Staff)]
    public Task<AsistenciaDto> RegistrarAsistencia(int horarioId, AsistenciaRequest r) => actividades.RegistrarAsistenciaAsync(horarioId, r.UsuarioId);

    [HttpGet("horarios/{horarioId:int}/asistencia"), Authorize(Politicas.Staff)]
    public Task<List<AsistenciaDto>> Asistencias(int horarioId, DateOnly? fecha) => actividades.AsistenciasAsync(horarioId, fecha);
}
