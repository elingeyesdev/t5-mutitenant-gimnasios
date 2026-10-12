using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Services;

public class OpcionesCorreo
{
    public string? Host { get; set; }
    public int Puerto { get; set; } = 587;
    public string? Usuario { get; set; }
    public string? Password { get; set; }
    public string? Remitente { get; set; }
    public bool Configurado => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(Usuario);
}

public record SesionIniciadaDto(string Token, UsuarioDto Usuario);

/// <summary>Registro, inicio de sesión y recuperación de contraseña (CU-01, CU-02, CU-03).</summary>
public class CuentaService(
    AppDbContext db, ServicioAutenticacion auth, UsuariosService usuarios, OpcionesCorreo correo, ILogger<CuentaService> logger)
{
    public async Task<SesionIniciadaDto> IniciarSesionAsync(string email, string password)
    {
        var u = await auth.ValidarAsync(email, password);
        var principal = await auth.PrincipalAsync(u, EsquemasAuth.Jwt);
        return new SesionIniciadaDto(auth.Token(principal), await usuarios.ObtenerAsync(u.Id));
    }

    public async Task<SesionIniciadaDto> RegistrarAsync(UsuarioForm f)
    {
        var creado = await usuarios.CrearAsync(f, registroPublico: true);
        var u = await db.Usuarios.Include(x => x.Perfil).FirstAsync(x => x.Id == creado.Id);
        var principal = await auth.PrincipalAsync(u, EsquemasAuth.Jwt);
        return new SesionIniciadaDto(auth.Token(principal), creado);
    }

    /// <summary>Genera un código de 6 dígitos válido 15 minutos. Responde igual exista o no el correo.</summary>
    public async Task<string> OlvidePasswordAsync(string email)
    {
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email.Trim().ToLower());
        if (u is not null)
        {
            u.OtpCodigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            u.OtpExpiraEn = DateTime.UtcNow.AddMinutes(15);
            await db.SaveChangesAsync();
            await EnviarCodigoAsync(u.Email, u.OtpCodigo);
        }
        return "Si el correo existe recibirás un código OTP.";
    }

    public async Task<string> RestablecerPasswordAsync(string email, string codigo, string nueva)
    {
        if (string.IsNullOrWhiteSpace(nueva) || nueva.Length < 6) throw ErrorNegocio.Solicitud("La contraseña debe tener al menos 6 caracteres.");
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email.Trim().ToLower());
        var coincide = u?.OtpCodigo is not null && CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(u.OtpCodigo), System.Text.Encoding.UTF8.GetBytes((codigo ?? "").PadRight(6)[..6]));
        if (u is null || !coincide) throw ErrorNegocio.Solicitud("Código OTP inválido.");
        if (u.OtpExpiraEn is null || u.OtpExpiraEn < DateTime.UtcNow) throw ErrorNegocio.Solicitud("El código OTP ha expirado. Solicita uno nuevo.");

        u.PasswordHash = ServicioAutenticacion.Hashear(nueva);
        u.OtpCodigo = null;
        u.OtpExpiraEn = null;
        await db.SaveChangesAsync();
        return "Contraseña actualizada correctamente.";
    }

    private async Task EnviarCodigoAsync(string destino, string codigo)
    {
        if (!correo.Configurado)
        {
            logger.LogWarning("[OTP] Correo no configurado. Código para {Correo}: {Codigo}", destino, codigo);
            return;
        }
        using var smtp = new SmtpClient(correo.Host, correo.Puerto)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(correo.Usuario, correo.Password),
        };
        var mensaje = new MailMessage(correo.Remitente ?? correo.Usuario!, destino)
        {
            Subject = "Código de verificación - Gestión Gimnasios",
            IsBodyHtml = true,
            Body = $"""
                <div style="font-family: Arial, sans-serif; max-width: 400px; margin: 0 auto; padding: 24px; border: 1px solid #eaeaea; border-radius: 8px;">
                  <h2 style="color: #1F4E79; text-align: center;">Gestión Gimnasios</h2>
                  <p>Solicitaste restablecer tu contraseña. Usa este código de verificación:</p>
                  <p style="font-size: 28px; font-weight: bold; letter-spacing: 6px; text-align: center;">{codigo}</p>
                  <p style="font-size: 12px; color: #666; text-align: center;">El código vence en 15 minutos. No lo compartas.</p>
                </div>
                """,
        };
        try
        {
            await smtp.SendMailAsync(mensaje);
        }
        catch (SmtpException ex)
        {
            logger.LogError(ex, "[SMTP] Error al enviar el código a {Correo}", destino);
            throw new ErrorNegocio(StatusCodes.Status500InternalServerError, "Fallo interno del servidor de correo.");
        }
    }
}
