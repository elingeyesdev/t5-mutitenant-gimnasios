namespace GestionGimnasios.Web.Services;

/// <summary>
/// Archivos personales (comprobantes de pago, videos de socios). Se guardan fuera de wwwroot, así
/// no tienen una URL pública y solo se entregan desde acciones que verifican el permiso.
/// La carpeta se cambia con Archivos:CarpetaPrivada; en el hosting debe apuntar a un disco persistente.
/// </summary>
public class ArchivosPrivados(IConfiguration configuration)
{
    private readonly string raiz = configuration["Archivos:CarpetaPrivada"] is { Length: > 0 } carpeta
        ? carpeta
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GestionGimnasios", "privado");

    /// <summary>Guarda el archivo con un nombre aleatorio y devuelve su ruta relativa para la base de datos.</summary>
    public async Task<string> GuardarAsync(IFormFile archivo, string carpeta, string extension)
    {
        var relativa = Path.Combine(carpeta, $"{Guid.NewGuid():N}{extension}");
        var ruta = RutaCompleta(relativa);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        await using var destino = File.Create(ruta);
        await archivo.CopyToAsync(destino);
        return relativa;
    }

    public string RutaCompleta(string relativa) => Path.Combine(raiz, relativa);

    public void Eliminar(string? relativa)
    {
        if (string.IsNullOrEmpty(relativa)) return;
        var ruta = RutaCompleta(relativa);
        if (File.Exists(ruta)) File.Delete(ruta);
    }
}
