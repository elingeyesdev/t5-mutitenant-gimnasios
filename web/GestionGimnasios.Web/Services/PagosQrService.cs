using System.Security.Cryptography;
using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public record OrdenPagoDto(
    int Id, string Codigo, int SuscripcionId, string Plan, string Cadena, int UsuarioId, string Socio, string? Sucursal,
    int Meses, decimal Monto, string Estado, DateTime CreadaEn, DateTime ExpiraEn, string? ComprobanteReferencia, bool TieneImagenComprobante,
    DateTime? ComprobanteEn, string? RevisadaPor, DateTime? RevisadaEn, string? MotivoRechazo, DateOnly VenceSuscripcion)
{
    /// <summary>Imagen PNG del QR en Base64; solo mientras la orden sigue pendiente.</summary>
    public string? QrPngBase64 { get; init; }
}

public record ResumenPagosQrDto(int PorConfirmar, int SinComprobante, int ConfirmadasDelMes, decimal MontoConfirmadoDelMes);
public record ArchivoComprobante(string Ruta, string TipoContenido);

public class OrdenPagoForm
{
    public int SuscripcionId { get; set; }
    public int Meses { get; set; } = 1;
}

public class ComprobanteForm
{
    public string Referencia { get; set; } = "";
    public IFormFile? Archivo { get; set; }
}

/// <summary>
/// Renovación de la membresía con QR (RF-05, CU-39). El socio genera la orden y registra su
/// comprobante; recepción lo concilia con el dinero recibido y lo confirma. No hay una pasarela de
/// pago conectada: el QR identifica la orden y nada se activa sin la confirmación de una persona.
/// </summary>
public class PagosQrService(AppDbContext db, UsuarioActual actual, ArchivosPrivados archivos, NotificacionesService notificaciones)
{
    public const int HorasParaPagar = 24;
    private const int MaximoMeses = 12;
    private const long MaximoBytesComprobante = 5 * 1024 * 1024;
    private static readonly Dictionary<string, string> TiposImagen = new()
    {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp",
    };

    // ── Socio ────────────────────────────────────────────────────────────

    public async Task<OrdenPagoDto> CrearAsync(OrdenPagoForm f)
    {
        ValidarMeses(f.Meses);
        await VencerAsync();
        var s = await db.Suscripciones.IgnoreQueryFilters().Include(x => x.Plan)
                    .FirstOrDefaultAsync(x => x.Id == f.SuscripcionId && x.UsuarioId == actual.Id)
                ?? throw ErrorNegocio.NoEncontrado("Suscripción no encontrada.");
        if (s.Estado == EstadosSuscripcion.Cancelada) throw ErrorNegocio.Conflicto("Una suscripción cancelada no se puede renovar.");
        if (s.Plan is not { Activo: true }) throw ErrorNegocio.Conflicto("Este plan ya no está disponible. Consulta en recepción.");
        if (s.CadenaId is not int cadenaId) throw ErrorNegocio.Conflicto("Esta suscripción no pertenece a una cadena: renuévala en recepción.");
        if (await db.OrdenesPagoQr.IgnoreQueryFilters().AnyAsync(o => o.SuscripcionId == s.Id && o.Estado == EstadosOrdenPago.Pendiente))
            throw ErrorNegocio.Conflicto("Ya tienes una orden pendiente para esta suscripción.");

        var orden = new OrdenPagoQr
        {
            CadenaId = cadenaId, UsuarioId = actual.Id, SuscripcionId = s.Id, SucursalId = s.SucursalPrincipalId,
            Codigo = await NuevoCodigoAsync(), Meses = f.Meses, Monto = CalcularMonto(s.Plan.PrecioMensual, f.Meses),
            ExpiraEn = DateTime.UtcNow.AddHours(HorasParaPagar),
        };
        db.OrdenesPagoQr.Add(orden);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // El índice único filtrado rechaza dos pedidos simultáneos para la misma suscripción.
            throw ErrorNegocio.Conflicto("Ya tienes una orden pendiente para esta suscripción.");
        }
        return await ObtenerPropiaAsync(orden.Id);
    }

    public async Task<List<OrdenPagoDto>> MisOrdenesAsync()
    {
        await VencerAsync();
        return await Consulta(db.OrdenesPagoQr.IgnoreQueryFilters().Where(o => o.UsuarioId == actual.Id).OrderByDescending(o => o.CreadaEn)).ToListAsync();
    }

    public async Task<OrdenPagoDto> ObtenerPropiaAsync(int id)
    {
        await VencerAsync();
        var o = await Consulta(db.OrdenesPagoQr.IgnoreQueryFilters().Where(x => x.Id == id && x.UsuarioId == actual.Id)).FirstOrDefaultAsync()
                ?? throw ErrorNegocio.NoEncontrado("Orden no encontrada.");
        return ConQr(o);
    }

    /// <summary>El socio indica la referencia de su pago y, si quiere, adjunta la imagen. Se puede corregir mientras siga pendiente.</summary>
    public async Task<OrdenPagoDto> RegistrarComprobanteAsync(int id, ComprobanteForm f)
    {
        var referencia = ValidarReferencia(f.Referencia);
        var extension = f.Archivo is null ? null : ValidarImagen(f.Archivo);
        await VencerAsync();
        var o = await db.OrdenesPagoQr.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == actual.Id)
                ?? throw ErrorNegocio.NoEncontrado("Orden no encontrada.");
        if (o.Estado != EstadosOrdenPago.Pendiente) throw ErrorNegocio.Conflicto($"La orden ya está {EstadosOrdenPago.Etiqueta(o.Estado).ToLowerInvariant()}.");

        if (f.Archivo is not null)
        {
            archivos.Eliminar(o.ComprobanteArchivo);
            o.ComprobanteArchivo = await archivos.GuardarAsync(f.Archivo, "comprobantes", extension!);
        }
        o.ComprobanteReferencia = referencia;
        o.ComprobanteEn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await ObtenerPropiaAsync(id);
    }

    public async Task<ArchivoComprobante> ComprobantePropioAsync(int id)
    {
        var o = await db.OrdenesPagoQr.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == actual.Id)
                ?? throw ErrorNegocio.NoEncontrado("Orden no encontrada.");
        return Archivo(o);
    }

    // ── Recepción: revisar y conciliar ───────────────────────────────────

    /// <summary>Órdenes del alcance del personal. Primero las que ya tienen comprobante y esperan confirmación.</summary>
    public async Task<List<OrdenPagoDto>> ListarAsync(string? estado = null)
    {
        await VencerAsync();
        var q = Visibles();
        if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(o => o.Estado == estado);
        return await Consulta(q.OrderBy(o => o.Estado == EstadosOrdenPago.Pendiente && o.ComprobanteEn != null ? 0 : 1)
            .ThenByDescending(o => o.CreadaEn).Take(200)).ToListAsync();
    }

    public async Task<ResumenPagosQrDto> ResumenAsync()
    {
        await VencerAsync();
        var q = Visibles();
        var hoy = Reloj.Hoy;
        var inicioMes = Reloj.InicioDelDiaUtc(new DateOnly(hoy.Year, hoy.Month, 1));
        var confirmadas = q.Where(o => o.Estado == EstadosOrdenPago.Confirmada && o.RevisadaEn >= inicioMes);
        return new ResumenPagosQrDto(
            await q.CountAsync(o => o.Estado == EstadosOrdenPago.Pendiente && o.ComprobanteEn != null),
            await q.CountAsync(o => o.Estado == EstadosOrdenPago.Pendiente && o.ComprobanteEn == null),
            await confirmadas.CountAsync(),
            await confirmadas.SumAsync(o => (decimal?)o.Monto) ?? 0);
    }

    public async Task<OrdenPagoDto> ObtenerAsync(int id)
    {
        await VencerAsync();
        var o = await Consulta(Visibles().Where(x => x.Id == id)).FirstOrDefaultAsync() ?? throw ErrorNegocio.NoEncontrado("Orden no encontrada.");
        return ConQr(o);
    }

    public async Task<ArchivoComprobante> ComprobanteAsync(int id)
    {
        var o = await Visibles().FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado("Orden no encontrada.");
        return Archivo(o);
    }

    /// <summary>Recepción verificó el dinero: la orden se confirma, se registra el pago y recién entonces se renueva la suscripción.</summary>
    public async Task<OrdenPagoDto> ConfirmarAsync(int id)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var o = await Visibles().Include(x => x.Suscripcion).ThenInclude(s => s!.Plan).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw ErrorNegocio.NoEncontrado("Orden no encontrada.");
        if (MotivoNoConfirmable(o) is string motivo) throw ErrorNegocio.Conflicto(motivo);
        var s = o.Suscripcion!;
        if (s.Estado == EstadosSuscripcion.Cancelada) throw ErrorNegocio.Conflicto("La suscripción fue cancelada: rechaza esta orden.");

        await TomarParaRevisarAsync(id, EstadosOrdenPago.Confirmada, null);

        var hoy = Reloj.Hoy;
        s.Fin = FinRenovado(s.Fin, hoy, o.Meses);
        s.Estado = EstadosSuscripcion.Activa;
        s.ActualizadaEn = DateTime.UtcNow;
        o.Pago = new PagoSuscripcion { SuscripcionId = s.Id, Monto = o.Monto, Metodo = "QR" };
        await db.SaveChangesAsync();
        await notificaciones.NotificarAsync(o.UsuarioId, TiposNotificacion.PagoQr, "Pago confirmado",
            $"Tu plan {s.Plan?.Nombre} quedó renovado hasta el {s.Fin:dd/MM/yyyy}.");
        await tx.CommitAsync();
        return await ObtenerAsync(id);
    }

    /// <summary>El pago no se pudo verificar: la orden se cierra con su motivo y la suscripción no cambia.</summary>
    public async Task<OrdenPagoDto> RechazarAsync(int id, string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw ErrorNegocio.Solicitud("Indica el motivo del rechazo.");
        var texto = motivo.Trim();
        if (texto.Length > 300) throw ErrorNegocio.Solicitud("El motivo no puede superar los 300 caracteres.");

        var o = await Visibles().FirstOrDefaultAsync(x => x.Id == id) ?? throw ErrorNegocio.NoEncontrado("Orden no encontrada.");
        if (o.Estado != EstadosOrdenPago.Pendiente) throw ErrorNegocio.Conflicto($"La orden ya está {EstadosOrdenPago.Etiqueta(o.Estado).ToLowerInvariant()}.");
        await TomarParaRevisarAsync(id, EstadosOrdenPago.Rechazada, texto);
        await notificaciones.NotificarAsync(o.UsuarioId, TiposNotificacion.PagoQr, "Pago no confirmado", $"La orden {o.Codigo} fue rechazada: {texto}");
        return await ObtenerAsync(id);
    }

    // ── Reglas ───────────────────────────────────────────────────────────

    public static decimal CalcularMonto(decimal precioMensual, int meses) => decimal.Round(precioMensual * meses, 2);

    /// <summary>Con la suscripción vigente se suma al vencimiento; si ya venció, los meses cuentan desde hoy.</summary>
    public static DateOnly FinRenovado(DateOnly finActual, DateOnly hoy, int meses) =>
        finActual >= hoy ? finActual.AddMonths(meses) : hoy.AddMonths(meses).AddDays(-1);

    /// <summary>Por qué recepción no puede confirmar la orden, o null si puede. Sin comprobante no hay confirmación.</summary>
    public static string? MotivoNoConfirmable(OrdenPagoQr o)
    {
        if (o.Estado != EstadosOrdenPago.Pendiente) return $"La orden ya está {EstadosOrdenPago.Etiqueta(o.Estado).ToLowerInvariant()}.";
        if (o.ComprobanteEn is null) return "El socio todavía no registró su comprobante.";
        return null;
    }

    public static void ValidarMeses(int meses)
    {
        if (meses is < 1 or > MaximoMeses) throw ErrorNegocio.Solicitud($"La renovación debe ser de 1 a {MaximoMeses} meses.");
    }

    public static string ValidarReferencia(string? referencia)
    {
        var texto = referencia?.Trim() ?? "";
        if (texto.Length == 0) throw ErrorNegocio.Solicitud("Indica el número o la referencia de tu comprobante.");
        if (texto.Length > 60) throw ErrorNegocio.Solicitud("La referencia no puede superar los 60 caracteres.");
        return texto;
    }

    /// <summary>Contenido del QR: identifica la orden, el monto y la moneda.</summary>
    public static string TextoQr(string codigo, decimal monto) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"GG-PAGO|{codigo}|{monto:0.00}|BOB");

    // ── Apoyo ────────────────────────────────────────────────────────────

    /// <summary>Órdenes de la cadena (filtro global); el personal de sucursal solo ve las de su sucursal.</summary>
    private IQueryable<OrdenPagoQr> Visibles()
    {
        if (!actual.EsSuperAdmin && (!actual.EsPersonal || actual.Nivel < Niveles.Recepcionista))
            throw ErrorNegocio.Prohibido("No tienes acceso a las órdenes de pago.");
        var q = db.OrdenesPagoQr.AsQueryable();
        if (actual.SucursalFiltro is int propia) q = q.Where(o => o.SucursalId == propia);
        return q;
    }

    /// <summary>Cambia el estado solo si sigue pendiente: si dos personas revisan a la vez, la segunda recibe un error.</summary>
    private async Task TomarParaRevisarAsync(int id, string estado, string? motivo)
    {
        var ahora = DateTime.UtcNow;
        var tomadas = await db.OrdenesPagoQr.Where(o => o.Id == id && o.Estado == EstadosOrdenPago.Pendiente)
            .ExecuteUpdateAsync(x => x.SetProperty(o => o.Estado, estado).SetProperty(o => o.RevisadaPorId, actual.Id)
                .SetProperty(o => o.RevisadaEn, ahora).SetProperty(o => o.MotivoRechazo, motivo));
        if (tomadas == 0) throw ErrorNegocio.Conflicto("Otra persona ya revisó esta orden.");
    }

    /// <summary>Las órdenes sin comprobante vencen pasado el plazo; las que tienen comprobante esperan a recepción.</summary>
    private Task VencerAsync()
    {
        var ahora = DateTime.UtcNow;
        return db.OrdenesPagoQr.IgnoreQueryFilters()
            .Where(o => o.Estado == EstadosOrdenPago.Pendiente && o.ComprobanteEn == null && o.ExpiraEn < ahora)
            .ExecuteUpdateAsync(x => x.SetProperty(o => o.Estado, EstadosOrdenPago.Vencida));
    }

    private ArchivoComprobante Archivo(OrdenPagoQr o)
    {
        if (string.IsNullOrEmpty(o.ComprobanteArchivo)) throw ErrorNegocio.NoEncontrado("Esta orden no tiene una imagen de comprobante.");
        var ruta = archivos.RutaCompleta(o.ComprobanteArchivo);
        if (!File.Exists(ruta)) throw ErrorNegocio.NoEncontrado("No se encontró el archivo del comprobante.");
        return new ArchivoComprobante(ruta, TiposImagen[Path.GetExtension(ruta).ToLowerInvariant()]);
    }

    private static string ValidarImagen(IFormFile archivo)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!TiposImagen.ContainsKey(extension)) throw ErrorNegocio.Solicitud("Formato de imagen no permitido (jpg, png o webp).");
        if (archivo.Length > MaximoBytesComprobante) throw ErrorNegocio.Solicitud("La imagen supera los 5 MB.");
        return extension;
    }

    private static OrdenPagoDto ConQr(OrdenPagoDto o) => o.Estado == EstadosOrdenPago.Pendiente
        ? o with { QrPngBase64 = Convert.ToBase64String(CodigoQr.GenerarPng(TextoQr(o.Codigo, o.Monto))) }
        : o;

    private async Task<string> NuevoCodigoAsync()
    {
        string codigo;
        do codigo = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
        while (await db.OrdenesPagoQr.IgnoreQueryFilters().AnyAsync(o => o.Codigo == codigo));
        return codigo;
    }

    private static IQueryable<OrdenPagoDto> Consulta(IQueryable<OrdenPagoQr> q) =>
        q.Select(o => new OrdenPagoDto(
            o.Id, o.Codigo, o.SuscripcionId, o.Suscripcion!.Plan!.Nombre, o.Cadena!.Nombre, o.UsuarioId,
            o.Usuario!.Perfil!.Nombres + " " + o.Usuario.Perfil.Apellidos, o.Sucursal != null ? o.Sucursal.Nombre : null,
            o.Meses, o.Monto, o.Estado, o.CreadaEn, o.ExpiraEn, o.ComprobanteReferencia, o.ComprobanteArchivo != null,
            o.ComprobanteEn, o.RevisadaPor != null ? o.RevisadaPor.Perfil!.Nombres + " " + o.RevisadaPor.Perfil.Apellidos : null,
            o.RevisadaEn, o.MotivoRechazo, o.Suscripcion.Fin));
}
