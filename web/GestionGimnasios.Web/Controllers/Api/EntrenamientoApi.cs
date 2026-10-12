using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers.Api;

[Route("api/ejercicios")]
public class EjerciciosApi(EjerciciosService ejercicios) : ApiBase
{
    [HttpGet]
    public Task<List<EjercicioDto>> Listar(string? grupo, string? dificultad, string? categoria, string? tipo, string? buscar) =>
        ejercicios.ListarAsync(grupo, dificultad, categoria, tipo, buscar);

    [HttpGet("grupos")]
    public Task<List<string>> Grupos() => ejercicios.GruposAsync();

    [HttpGet("{id:int}")]
    public Task<EjercicioDto> Obtener(int id) => ejercicios.ObtenerAsync(id);

    [HttpPost, Authorize(Politicas.SuperAdmin)]
    public Task<EjercicioDto> Crear(EjercicioForm f) => ejercicios.CrearAsync(f);

    [HttpPut("{id:int}"), Authorize(Politicas.SuperAdmin)]
    public Task<EjercicioDto> Actualizar(int id, EjercicioForm f) => ejercicios.ActualizarAsync(id, f);

    [HttpPut("{id:int}/imagen"), Authorize(Politicas.SuperAdmin)]
    public Task<EjercicioDto> Imagen(int id, IFormFile archivo) => ejercicios.ActualizarImagenAsync(id, archivo);

    [HttpDelete("{id:int}/imagen"), Authorize(Politicas.SuperAdmin)]
    public Task<EjercicioDto> QuitarImagen(int id) => ejercicios.EliminarImagenAsync(id);

    [HttpDelete("{id:int}"), Authorize(Politicas.SuperAdmin)]
    public async Task<NoContentResult> Eliminar(int id)
    {
        await ejercicios.EliminarAsync(id);
        return NoContent();
    }
}

[Route("api/rutinas")]
public class RutinasApi(RutinasService rutinas, UsuarioActual actual) : ApiBase
{
    [HttpGet]
    public Task<List<RutinaDto>> Listar(bool? plantillas) => rutinas.ListarAsync(plantillas);

    [HttpGet("mias")]
    public Task<List<RutinaDto>> Mias() => rutinas.DeUsuarioAsync(actual.Id);

    [HttpGet("usuario/{usuarioId:int}")]
    public Task<List<RutinaDto>> DeUsuario(int usuarioId) => rutinas.DeUsuarioAsync(usuarioId);

    [HttpGet("entrenador/{entrenadorId:int}"), Authorize(Politicas.Staff)]
    public Task<List<RutinaDto>> DeEntrenador(int entrenadorId) => rutinas.DeEntrenadorAsync(entrenadorId);

    [HttpGet("mis-alumnos"), Authorize(Politicas.Staff)]
    public Task<List<object>> MisAlumnos() => rutinas.MisAlumnosAsync();

    [HttpGet("{id:int}")]
    public Task<RutinaDto> Obtener(int id) => rutinas.ObtenerAsync(id);

    [HttpPost, Authorize(Politicas.Staff)]
    public Task<RutinaDto> Crear(RutinaForm f) => rutinas.CrearAsync(f);

    [HttpPut("{id:int}"), Authorize(Politicas.Staff)]
    public Task<RutinaDto> Actualizar(int id, RutinaForm f) => rutinas.ActualizarAsync(id, f);

    [HttpDelete("{id:int}"), Authorize(Politicas.Staff)]
    public async Task<NoContentResult> Eliminar(int id)
    {
        await rutinas.EliminarAsync(id);
        return NoContent();
    }
}

[Route("api/entrenamiento")]
public class EntrenamientoApi(EntrenamientoService entrenamiento, UsuarioActual actual) : ApiBase
{
    [HttpGet("perfil")]
    public Task<PerfilEntrenamientoDto?> Perfil(int? usuarioId) => entrenamiento.PerfilAsync(usuarioId ?? actual.Id);

    [HttpPut("perfil")]
    public Task<PerfilEntrenamientoDto> GuardarPerfil(PerfilEntrenamientoDto f) => entrenamiento.GuardarPerfilAsync(f);

    [HttpGet("restriccion")]
    public Task<RestriccionDto?> Restriccion(int? usuarioId) => entrenamiento.RestriccionAsync(usuarioId ?? actual.Id);

    [HttpPut("restriccion")]
    public Task<RestriccionDto> GuardarRestriccion(RestriccionDto f) => entrenamiento.GuardarRestriccionAsync(f);

    [HttpGet("contactos")]
    public Task<List<ContactoDto>> Contactos(int? usuarioId) => entrenamiento.ContactosAsync(usuarioId ?? actual.Id);

    [HttpPost("contactos")]
    public Task<ContactoDto> AgregarContacto(ContactoDto f) => entrenamiento.AgregarContactoAsync(f);

    [HttpDelete("contactos/{id:int}")]
    public async Task<NoContentResult> EliminarContacto(int id)
    {
        await entrenamiento.EliminarContactoAsync(id);
        return NoContent();
    }

    [HttpGet("sesiones")]
    public Task<List<SesionDto>> Sesiones(int? usuarioId, int? rutinaId, int limite = 50, int desde = 0) =>
        entrenamiento.SesionesAsync(usuarioId ?? actual.Id, rutinaId, limite, desde);

    [HttpGet("sesiones/{id:int}")]
    public Task<SesionDto> Sesion(int id) => entrenamiento.SesionAsync(id);

    [HttpPost("sesiones")]
    public Task<SesionDto> GuardarSesion(SesionForm f) => entrenamiento.GuardarSesionAsync(f);

    [HttpPut("sesiones/{id:int}")]
    public Task<SesionDto> ActualizarSesion(int id, SesionForm f) => entrenamiento.ActualizarSesionAsync(id, f);

    [HttpPost("sesiones/{id:int}/series")]
    public Task<SerieDto> AgregarSerie(int id, SerieForm f) => entrenamiento.AgregarSerieAsync(id, f);

    [HttpDelete("sesiones/{id:int}")]
    public async Task<NoContentResult> EliminarSesion(int id)
    {
        await entrenamiento.EliminarSesionAsync(id);
        return NoContent();
    }

    [HttpGet("records")]
    public Task<List<RecordFuerza>> Records(int? usuarioId) => entrenamiento.RecordsAsync(usuarioId ?? actual.Id);
}

[Route("api/metricas")]
public class MetricasApi(EntrenamientoService entrenamiento, UsuarioActual actual) : ApiBase
{
    [HttpGet]
    public Task<List<MetricaDto>> Listar(int? usuarioId) => entrenamiento.MetricasAsync(usuarioId ?? actual.Id);

    [HttpGet("ultima")]
    public Task<MetricaDto?> Ultima(int? usuarioId) => entrenamiento.UltimaMetricaAsync(usuarioId ?? actual.Id);

    [HttpPost]
    public Task<MetricaDto> Registrar(MetricaForm f) => entrenamiento.RegistrarMetricaAsync(f);

    [HttpPatch("circunferencias")]
    public Task<MetricaDto> Circunferencias(MetricaForm f) => entrenamiento.GuardarCircunferenciasAsync(f);

    [HttpDelete("{id:int}")]
    public async Task<NoContentResult> Eliminar(int id)
    {
        await entrenamiento.EliminarMetricaAsync(id);
        return NoContent();
    }
}
