using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

/// <summary>
/// Tareas programadas del proyecto base (@nestjs/schedule) como servicio en segundo plano:
/// salida automática (cada 30 min), recordatorios de reserva (cada 15 min), alertas de
/// inactividad de alumnos y detección de abandono por cadena (RF-04), ambas a diario a las 9:00.
/// </summary>
public class TareasProgramadas(IServiceScopeFactory scopes, ILogger<TareasProgramadas> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ultimaSalida = DateTime.MinValue;
        DateOnly? ultimaAlerta = null;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                var ahora = Reloj.Ahora;
                if (ahora.Minute % 15 == 0) await EjecutarAsync(RecordatoriosAsync, "recordatorios");
                if ((DateTime.UtcNow - ultimaSalida).TotalMinutes >= 30)
                {
                    await EjecutarAsync(async db =>
                    {
                        var n = await AccesosService.CerrarVencidosAsync(db);
                        if (n > 0) logger.LogInformation("Salida automática: {N} ingresos cerrados (más de 4 h sin salida).", n);
                    }, "salida automática");
                    ultimaSalida = DateTime.UtcNow;
                }
                if (ahora.Hour >= 9 && ultimaAlerta != Reloj.Hoy)
                {
                    await EjecutarAsync(AlertasInactividadAsync, "alertas de inactividad");
                    await EjecutarAsync(async db =>
                    {
                        var r = await AlertasService.DetectarAsync(db);
                        logger.LogInformation("Detección de abandono: {Revisados} socios revisados, {Nuevas} alertas nuevas, {Cerradas} cerradas.", r.Revisados, r.Nuevas, r.Cerradas);
                    }, "detección de abandono");
                    ultimaAlerta = Reloj.Hoy;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error en las tareas programadas");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task EjecutarAsync(Func<AppDbContext, Task> tarea, string nombre)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            await tarea(db);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en la tarea {Tarea}", nombre);
        }
    }

    /// <summary>Avisa una sola vez de las reservas confirmadas que empiezan en unas 2 horas.</summary>
    private static async Task RecordatoriosAsync(AppDbContext db)
    {
        var ahora = Reloj.Ahora;
        var desde = ahora.AddHours(2);
        var hasta = ahora.AddHours(2.25);
        var fechas = new[] { DateOnly.FromDateTime(desde), DateOnly.FromDateTime(hasta) }.Distinct().ToList();
        var candidatas = await db.Reservas.IgnoreQueryFilters().Include(r => r.Actividad)
            .Where(r => r.Estado == EstadosReserva.Confirmada && !r.RecordatorioEnviado && r.Inicio != null && fechas.Contains(r.Fecha))
            .ToListAsync();
        foreach (var r in candidatas)
        {
            var inicio = r.Fecha.ToDateTime(r.Inicio!.Value);
            if (inicio < desde || inicio > hasta) continue;
            var pref = await db.PreferenciasNotificacion.FirstOrDefaultAsync(p => p.UsuarioId == r.UsuarioId);
            if (pref?.RecordatoriosClase != false)
                db.Notificaciones.Add(new Notificacion
                {
                    UsuarioId = r.UsuarioId, Tipo = TiposNotificacion.Recordatorio, Titulo = "Recordatorio de clase",
                    Cuerpo = $"Tu clase de {r.Actividad?.Nombre ?? "tu clase"} comienza a las {r.Inicio:HH\\:mm}. Te esperamos.",
                });
            r.RecordatorioEnviado = true;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Avisa a cada asesor de los alumnos que no entrenan según su meta semanal.</summary>
    private static async Task AlertasInactividadAsync(AppDbContext db)
    {
        var asesorias = await db.Asesorias.Where(a => a.Estado == EstadosAsesoria.Activa)
            .Select(a => new { a.AsesorId, a.ClienteId, a.MetaSesionesSemana, Nombre = a.Cliente!.Perfil!.Nombres + " " + a.Cliente.Perfil.Apellidos }).ToListAsync();
        if (asesorias.Count == 0) return;
        var clientes = asesorias.Select(a => a.ClienteId).Distinct().ToList();
        var ultimas = await db.Sesiones.Where(s => clientes.Contains(s.UsuarioId)).GroupBy(s => s.UsuarioId)
            .Select(g => new { g.Key, Ultima = g.Max(s => s.Inicio) }).ToDictionaryAsync(x => x.Key, x => x.Ultima);
        foreach (var grupo in asesorias.GroupBy(a => a.AsesorId))
        {
            var inactivos = grupo.Where(a =>
            {
                var dias = ultimas.TryGetValue(a.ClienteId, out var u) ? (DateTime.UtcNow - u).TotalDays : 999;
                return dias >= PersonalService.UmbralInactividad(a.MetaSesionesSemana);
            }).Select(a => a.Nombre).ToList();
            if (inactivos.Count == 0) continue;
            db.Notificaciones.Add(new Notificacion
            {
                UsuarioId = grupo.Key, Tipo = TiposNotificacion.Inactividad, Titulo = "Alumnos inactivos",
                Cuerpo = $"Tienes {inactivos.Count} alumno(s) sin entrenar según su meta: {string.Join(", ", inactivos.Take(5))}{(inactivos.Count > 5 ? "…" : "")}.",
            });
        }
        await db.SaveChangesAsync();
    }
}
