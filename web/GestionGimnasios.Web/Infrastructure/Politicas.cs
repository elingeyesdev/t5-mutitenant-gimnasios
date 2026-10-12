namespace GestionGimnasios.Web.Infrastructure;

/// <summary>Políticas de autorización por nivel jerárquico (equivalentes a los guards del proyecto base).</summary>
public static class Politicas
{
    /// <summary>Instructor o superior (StaffLevelGuard).</summary>
    public const string Staff = "Staff";
    /// <summary>Entrenador, nutricionista o superior.</summary>
    public const string Asesor = "Asesor";
    /// <summary>Recepcionista, gerente o super administrador (AdminLevelGuard).</summary>
    public const string Admin = "Admin";
    public const string Gerente = "Gerente";
    /// <summary>Solo super administrador (SuperAdminGuard).</summary>
    public const string SuperAdmin = "SuperAdmin";
    /// <summary>Personal de sucursal que registra ingresos; el super administrador no escanea (ScannerGuard).</summary>
    public const string Escaner = "Escaner";

    public const string LimiteLogin = "LimiteLogin";
    public const string LimiteRecuperacion = "LimiteRecuperacion";
}
