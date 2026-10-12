using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Actividades y horarios de clases (HU-11).</summary>
[Authorize(Politicas.Admin)]
public class ActividadesController(ActividadesService actividades, SedesService sedes, UsuarioActual actual) : PanelController
{
    public async Task<IActionResult> Index(int? sucursalId, bool inactivas = false)
    {
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        ViewBag.SucursalId = sucursalId;
        ViewBag.Inactivas = inactivas;
        return View(await actividades.ListarAsync(sucursalId, inactivas));
    }

    public async Task<IActionResult> Crear()
    {
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        return View("Formulario", new ActividadForm { SucursalId = actual.SucursalFiltro ?? 0, DuracionMin = 60 });
    }

    [HttpPost]
    public async Task<IActionResult> Crear(ActividadForm f)
    {
        ActividadDto? creada = null;
        if (await GuardarAsync(async () => creada = await actividades.CrearAsync(f), $"Actividad {f.Nombre} creada. Ahora agrega sus horarios."))
            return RedirectToAction(nameof(Editar), new { id = creada!.Id });
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        return View("Formulario", f);
    }

    public async Task<IActionResult> Editar(int id)
    {
        var a = await actividades.ObtenerAsync(id);
        ViewBag.Actividad = a;
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        ViewBag.Instructores = await actividades.InstructoresElegiblesAsync(a.SucursalId);
        return View("Formulario", new ActividadForm { SucursalId = a.SucursalId, Nombre = a.Nombre, Descripcion = a.Descripcion, DuracionMin = a.DuracionMin, AccesoLibre = a.AccesoLibre, Activa = a.Activa });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, ActividadForm f)
    {
        if (await GuardarAsync(() => actividades.ActualizarAsync(id, f), "Actividad actualizada.")) return RedirectToAction(nameof(Editar), new { id });
        var a = await actividades.ObtenerAsync(id);
        ViewBag.Actividad = a;
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        ViewBag.Instructores = await actividades.InstructoresElegiblesAsync(a.SucursalId);
        return View("Formulario", f);
    }

    [HttpPost]
    public Task<IActionResult> Eliminar(int id) =>
        IntentarAsync(() => actividades.EliminarAsync(id), "Actividad desactivada y sus horarios eliminados.", () => RedirectToAction(nameof(Index)));

    [HttpPost]
    public Task<IActionResult> AgregarHorario(int id, HorarioActividadForm f) =>
        IntentarAsync(() => actividades.CrearHorarioAsync(id, f), "Horario agregado.", () => RedirectToAction(nameof(Editar), new { id }));

    [HttpPost]
    public Task<IActionResult> EliminarHorario(int id, int horarioId) =>
        IntentarAsync(() => actividades.EliminarHorarioAsync(horarioId), "Horario eliminado.", () => RedirectToAction(nameof(Editar), new { id }));
}

/// <summary>Inventario de máquinas por sucursal (HU-12).</summary>
[Authorize(Politicas.Admin)]
public class MaquinasController(MaquinasService maquinas, SedesService sedes, UsuarioActual actual) : PanelController
{
    public async Task<IActionResult> Index(int? sucursalId, string? estado)
    {
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        ViewBag.SucursalId = sucursalId;
        ViewBag.Estado = estado;
        ViewBag.Nueva = new MaquinaForm { SucursalId = sucursalId ?? actual.SucursalFiltro ?? 0 };
        return View(await maquinas.ListarAsync(sucursalId, estado));
    }

    [HttpPost]
    public Task<IActionResult> Crear(MaquinaForm f) =>
        IntentarAsync(() => maquinas.CrearAsync(f), $"Máquina {f.Nombre} registrada.", () => RedirectToAction(nameof(Index), new { sucursalId = f.SucursalId }));

    [HttpPost]
    public Task<IActionResult> Editar(Guid id, MaquinaForm f) =>
        IntentarAsync(() => maquinas.ActualizarAsync(id, f), "Máquina actualizada.", () => RedirectToAction(nameof(Index), new { sucursalId = f.SucursalId }));

    [HttpPost]
    public Task<IActionResult> Imagen(Guid id, IFormFile? archivo, int? sucursalId) =>
        IntentarAsync(() => archivo is null ? throw ErrorNegocio.Solicitud("Elige una imagen.") : maquinas.ActualizarImagenAsync(id, archivo),
            "Imagen actualizada.", () => RedirectToAction(nameof(Index), new { sucursalId }));

    [HttpPost]
    public Task<IActionResult> Eliminar(Guid id, int? sucursalId) =>
        IntentarAsync(() => maquinas.EliminarAsync(id), "Máquina eliminada.", () => RedirectToAction(nameof(Index), new { sucursalId }));
}

/// <summary>Biblioteca de ejercicios: solo super administrador (HU-09).</summary>
[Authorize(Politicas.SuperAdmin)]
public class EjerciciosController(EjerciciosService ejercicios) : PanelController
{
    public async Task<IActionResult> Index(string? grupo, string? buscar)
    {
        ViewBag.Grupos = await ejercicios.GruposAsync();
        ViewBag.Grupo = grupo;
        ViewBag.Buscar = buscar;
        return View(await ejercicios.ListarAsync(grupo: grupo, buscar: buscar, incluirInactivos: true));
    }

    public IActionResult Crear() => View("Formulario", new EjercicioForm());

    [HttpPost]
    public async Task<IActionResult> Crear(EjercicioForm f, string? secundarios)
    {
        f.GruposSecundarios = Lista(secundarios);
        if (await GuardarAsync(() => ejercicios.CrearAsync(f), $"Ejercicio {f.Nombre} creado.")) return RedirectToAction(nameof(Index));
        return View("Formulario", f);
    }

    public async Task<IActionResult> Editar(int id)
    {
        var e = await ejercicios.ObtenerAsync(id);
        ViewBag.Ejercicio = e;
        return View("Formulario", new EjercicioForm
        {
            Nombre = e.Nombre, Descripcion = e.Descripcion, GrupoMuscular = e.GrupoMuscular, Categoria = e.Categoria, Tipo = e.Tipo,
            GruposSecundarios = e.GruposSecundarios.ToList(), Equipo = e.Equipo, Dificultad = e.Dificultad, YoutubeVideoId = e.YoutubeVideoId,
            TipoRegistro = e.TipoRegistro, Activo = e.Activo,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, EjercicioForm f, string? secundarios)
    {
        f.GruposSecundarios = Lista(secundarios);
        if (await GuardarAsync(() => ejercicios.ActualizarAsync(id, f), "Ejercicio actualizado.")) return RedirectToAction(nameof(Index));
        ViewBag.Ejercicio = await ejercicios.ObtenerAsync(id);
        return View("Formulario", f);
    }

    [HttpPost]
    public Task<IActionResult> Imagen(int id, IFormFile? archivo) =>
        IntentarAsync(() => archivo is null ? throw ErrorNegocio.Solicitud("Elige una imagen.") : ejercicios.ActualizarImagenAsync(id, archivo),
            "Imagen actualizada.", () => RedirectToAction(nameof(Editar), new { id }));

    [HttpPost]
    public Task<IActionResult> Eliminar(int id) =>
        IntentarAsync(() => ejercicios.EliminarAsync(id), "Ejercicio eliminado (o desactivado si está en rutinas).", () => RedirectToAction(nameof(Index)));

    private static List<string> Lista(string? texto) =>
        (texto ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}

/// <summary>Rutinas desde el panel (HU-10): entrenadores y personal administrativo.</summary>
[Authorize(Politicas.Asesor)]
public class RutinasController(RutinasService rutinas, EjerciciosService ejercicios, PersonalService personal, UsuariosService usuarios, UsuarioActual actual) : PanelController
{
    public async Task<IActionResult> Index(bool plantillas = false) =>
        View(plantillas ? await rutinas.ListarAsync(true) : actual.Nivel >= Domain.Niveles.Recepcionista ? await rutinas.ListarAsync() : await rutinas.DeEntrenadorAsync(actual.Id));

    public async Task<IActionResult> Crear(int? alumnoId, int? desdePlantilla)
    {
        await CargarListas();
        var f = new RutinaForm { AsignadaAId = alumnoId, Ejercicios = [new RutinaEjercicioForm()] };
        if (desdePlantilla is int p)
        {
            var r = await rutinas.ObtenerAsync(p);
            f = new RutinaForm
            {
                Nombre = r.Nombre, Descripcion = r.Descripcion, Dificultad = r.Dificultad, DuracionSemanas = r.DuracionSemanas, AsignadaAId = alumnoId,
                Ejercicios = r.Ejercicios.Select(e => new RutinaEjercicioForm { EjercicioId = e.EjercicioId, Series = e.Series, Repeticiones = e.Repeticiones, PesoKg = e.PesoKg, DiaSemana = e.DiaSemana, DescansoSeg = e.DescansoSeg, Notas = e.Notas }).ToList(),
            };
        }
        return View("Formulario", f);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(RutinaForm f)
    {
        f.Ejercicios = f.Ejercicios.Where(e => e.EjercicioId > 0).ToList();
        if (await GuardarAsync(() => rutinas.CrearAsync(f), $"Rutina {f.Nombre} guardada.")) return RedirectToAction(nameof(Index));
        await CargarListas();
        return View("Formulario", f);
    }

    public async Task<IActionResult> Editar(int id)
    {
        var r = await rutinas.ObtenerAsync(id);
        await CargarListas();
        ViewBag.Rutina = r;
        return View("Formulario", new RutinaForm
        {
            Nombre = r.Nombre, Descripcion = r.Descripcion, Dificultad = r.Dificultad, DuracionSemanas = r.DuracionSemanas, EsPlantilla = r.EsPlantilla,
            AsignadaAId = r.AsignadaAId, SucursalId = r.SucursalId,
            Ejercicios = r.Ejercicios.Select(e => new RutinaEjercicioForm { EjercicioId = e.EjercicioId, Orden = e.Orden, Series = e.Series, Repeticiones = e.Repeticiones, PesoKg = e.PesoKg, DiaSemana = e.DiaSemana, DescansoSeg = e.DescansoSeg, Notas = e.Notas }).ToList(),
        });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, RutinaForm f)
    {
        f.Ejercicios = f.Ejercicios.Where(e => e.EjercicioId > 0).ToList();
        if (await GuardarAsync(() => rutinas.ActualizarAsync(id, f), "Rutina actualizada.")) return RedirectToAction(nameof(Index));
        await CargarListas();
        ViewBag.Rutina = await rutinas.ObtenerAsync(id);
        return View("Formulario", f);
    }

    [HttpPost]
    public Task<IActionResult> Eliminar(int id) =>
        IntentarAsync(() => rutinas.EliminarAsync(id), "Rutina eliminada.", () => RedirectToAction(nameof(Index)));

    private async Task CargarListas()
    {
        ViewBag.Ejercicios = await ejercicios.ListarAsync();
        // Alumnos: los asesorados activos del entrenador o los socios del alcance del personal administrativo.
        ViewBag.Alumnos = actual.Nivel >= Domain.Niveles.Recepcionista
            ? (await usuarios.ListarAsync(new FiltroUsuarios { Nivel = Domain.Niveles.Cliente, Limite = 500 })).Datos.Select(u => (u.Id, u.NombreCompleto)).ToList()
            : (await personal.ActivasAsync()).Select(a => (a.ClienteId, a.Cliente)).ToList();
    }
}
