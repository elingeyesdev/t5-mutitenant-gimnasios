using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers;

/// <summary>Reservas de la sucursal, ingreso a clase con código y lista de espera (HU-15, HU-16, HU-17).</summary>
[Authorize(Politicas.Admin)]
public class ReservasController(ReservasService reservas, ListaEsperaService espera, SedesService sedes, UsuariosService usuarios) : PanelController
{
    public async Task<IActionResult> Index(DateOnly? desde, DateOnly? hasta, string? estado, int? sucursalId, string? buscar, int desdeFila = 0)
    {
        desde ??= Reloj.Hoy;
        hasta ??= Reloj.Hoy.AddDays(7);
        ViewBag.Filtro = (desde, hasta, estado, sucursalId, buscar);
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        return View(await reservas.ListarAsync(desde, hasta, estado, sucursalId, null, buscar, 50, desdeFila));
    }

    [HttpPost]
    public async Task<IActionResult> Ingreso(string codigo, bool forzar = false)
    {
        try
        {
            var r = await reservas.IngresoPorCodigoAsync(codigo, forzar);
            Exito(r.Mensaje);
        }
        catch (ErrorNegocio e) when (e.Estado == StatusCodes.Status409Conflict)
        {
            TempData["Error"] = e.Message + " ¿Registrar el ingreso de todas formas?";
            TempData["CodigoFuturo"] = codigo;
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public Task<IActionResult> Cancelar(int id) =>
        IntentarAsync(() => reservas.CancelarAsync(id), "Reserva cancelada; se avisó al socio.", () => RedirectToAction(nameof(Index)));

    [HttpPost]
    public Task<IActionResult> Confirmar(int id) =>
        IntentarAsync(() => reservas.ConfirmarAsync(id), "Asistencia registrada.", () => RedirectToAction(nameof(Index)));

    [HttpPost]
    public Task<IActionResult> EstadoEnBloque(List<int> ids, string estado) =>
        IntentarAsync(() => ids.Count == 0 ? throw ErrorNegocio.Solicitud("Selecciona al menos una reserva.") : reservas.EstadoEnBloqueAsync(ids, estado),
            "Reservas actualizadas.", () => RedirectToAction(nameof(Index)));

    public async Task<IActionResult> Nueva(int? sucursalId, DateOnly? fecha, string? buscar)
    {
        var lista = await sedes.SucursalesAsync();
        var sid = sucursalId ?? lista.FirstOrDefault()?.Id;
        ViewBag.Sucursales = lista;
        ViewBag.SucursalId = sid;
        ViewBag.Fecha = fecha ?? Reloj.Hoy;
        ViewBag.Buscar = buscar;
        ViewBag.Socios = string.IsNullOrWhiteSpace(buscar) ? new List<ClienteResumenDto>() : await usuarios.BuscarClientesAsync(buscar, 10);
        ViewBag.Disponibilidad = sid is null ? new List<DisponibilidadDto>() : await reservas.DisponibilidadAsync(sid.Value, fecha ?? Reloj.Hoy);
        return View();
    }

    [HttpPost]
    public Task<IActionResult> Nueva(int usuarioId, int horarioId, DateOnly fecha, bool presencial) =>
        IntentarAsync(() => presencial ? reservas.CrearPresencialAsync(usuarioId, horarioId) : reservas.CrearAsync(new ReservaForm { UsuarioId = usuarioId, HorarioActividadId = horarioId, Fecha = fecha }),
            presencial ? "Reserva presencial registrada como asistida." : "Reserva creada.", () => RedirectToAction(nameof(Index)), () => RedirectToAction(nameof(Nueva), new { fecha }));

    public async Task<IActionResult> ListaEspera() => View(await espera.PendientesAsync());

    [HttpPost]
    public Task<IActionResult> Asignar(int id) =>
        IntentarAsync(() => espera.AsignarAsync(id), "Cupo asignado: se creó la reserva y se avisó al socio.", () => RedirectToAction(nameof(ListaEspera)));

    [HttpPost]
    public Task<IActionResult> QuitarDeEspera(int id) =>
        IntentarAsync(() => espera.SalirAsync(id), "Entrada retirada de la lista.", () => RedirectToAction(nameof(ListaEspera)));
}

/// <summary>Control de acceso con el carnet y auditoría (HU-18, HU-19).</summary>
[Authorize(Politicas.Admin)]
public class AccesosController(AccesosService accesos, SedesService sedes, UsuarioActual actual) : PanelController
{
    public async Task<IActionResult> Index(string? estado, int? sucursalId, DateOnly? desde, DateOnly? hasta, string? buscar, string? tipo, int desdeFila = 0)
    {
        desde ??= Reloj.Hoy.AddDays(-7);
        hasta ??= Reloj.Hoy;
        ViewBag.Filtro = (estado, sucursalId, desde, hasta, buscar, tipo);
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        ViewBag.PuedeEscanear = actual.Nivel is >= Domain.Niveles.Recepcionista and < Domain.Niveles.SuperAdmin;
        return View(await accesos.AuditoriaAsync(estado, sucursalId, desde, hasta, buscar, tipo, 50, desdeFila));
    }

    [HttpPost, Authorize(Politicas.Escaner)]
    public async Task<IActionResult> VistaPrevia(string codigo)
    {
        try
        {
            TempData["Vista"] = System.Text.Json.JsonSerializer.Serialize(await accesos.VistaPreviaAsync(codigo));
            TempData["Codigo"] = codigo;
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Politicas.Escaner)]
    public async Task<IActionResult> Registrar(string codigo)
    {
        try
        {
            var a = await accesos.RegistrarAsync(codigo);
            Exito(a.Estado == Domain.EstadosAcceso.Activo ? $"Ingreso registrado: {a.Usuario} en {a.Sucursal}." : $"Salida registrada: {a.Usuario} ({a.MinutosDentro} min dentro).");
        }
        catch (ErrorNegocio e)
        {
            Error(e.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Politicas.Escaner)]
    public Task<IActionResult> Salida(int id) =>
        IntentarAsync(() => accesos.RegistrarSalidaAsync(id), "Salida registrada.", () => RedirectToAction(nameof(Index)));
}

/// <summary>Planes, suscripciones y pagos (HU-13, HU-14).</summary>
[Authorize(Politicas.Admin)]
public class SuscripcionesController(SuscripcionesService suscripciones, SedesService sedes, UsuariosService usuarios, UsuarioActual actual) : PanelController
{
    public async Task<IActionResult> Index(string? estado, int? sucursalId)
    {
        ViewBag.Estado = estado;
        ViewBag.SucursalId = sucursalId;
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        return View(await suscripciones.ListarAsync(estado, sucursalId));
    }

    public async Task<IActionResult> Nueva(string? buscar, int? usuarioId)
    {
        ViewBag.Buscar = buscar;
        ViewBag.Socios = string.IsNullOrWhiteSpace(buscar) ? new List<ClienteResumenDto>() : await usuarios.BuscarClientesAsync(buscar, 10);
        ViewBag.Planes = await suscripciones.PlanesAsync();
        ViewBag.Sucursales = await sedes.SucursalesAsync();
        ViewBag.Usuario = usuarioId is null ? null : await usuarios.ObtenerVisibleAsync(usuarioId.Value);
        return View(new SuscripcionForm { UsuarioId = usuarioId ?? 0, SucursalPrincipalId = actual.SucursalFiltro, Inicio = Reloj.Hoy });
    }

    [HttpPost]
    public Task<IActionResult> Nueva(SuscripcionForm f) =>
        IntentarAsync(() => suscripciones.CrearAsync(f), "Suscripción registrada.", () => RedirectToAction(nameof(Index)), () => RedirectToAction(nameof(Nueva), new { usuarioId = f.UsuarioId }));

    [HttpPost]
    public Task<IActionResult> Pago(int id, PagoForm f) =>
        IntentarAsync(() => suscripciones.RegistrarPagoAsync(id, f), "Pago registrado.", () => RedirectToAction(nameof(Index)));

    [HttpPost]
    public Task<IActionResult> Estado(int id, string estado) =>
        IntentarAsync(() => suscripciones.CambiarEstadoAsync(id, estado), "Estado actualizado.", () => RedirectToAction(nameof(Index)));

    [Authorize(Politicas.Gerente)]
    public async Task<IActionResult> Planes()
    {
        ViewBag.Cadenas = await sedes.CadenasAsync();
        return View(await suscripciones.PlanesAsync(incluirInactivos: true));
    }

    [HttpPost, Authorize(Politicas.Gerente)]
    public Task<IActionResult> GuardarPlan(int? id, PlanForm f, string? beneficios)
    {
        f.Beneficios = (beneficios ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        return IntentarAsync(() => id is int i ? suscripciones.ActualizarPlanAsync(i, f) : suscripciones.CrearPlanAsync(f), "Plan guardado.", () => RedirectToAction(nameof(Planes)));
    }

    [HttpPost, Authorize(Politicas.Gerente)]
    public Task<IActionResult> EliminarPlan(int id) =>
        IntentarAsync(() => suscripciones.EliminarPlanAsync(id), "Plan eliminado (o desactivado si tiene suscripciones).", () => RedirectToAction(nameof(Planes)));
}
