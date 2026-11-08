using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestionGimnasios.Web.Controllers.Api;

/// <summary>Envío de videos y consulta de la devolución desde la app del socio (RF-01, HU-46).</summary>
[Route("api/videos-tecnica")]
public class VideosTecnicaApi(VideosTecnicaService videos) : ApiBase
{
    /// <summary>Un poco más que el máximo del video, por el resto de los campos del formulario.</summary>
    private const long LimitePeticion = VideosTecnicaService.MaximoBytes + 1024 * 1024;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimitePeticion)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimitePeticion)]
    public Task<VideoSocioDto> Enviar([FromForm] VideoForm f) => videos.EnviarAsync(f);

    [HttpGet("mios")]
    public Task<List<VideoSocioDto>> Mios() => videos.MisVideosAsync();

    [HttpGet("{id:int}/archivo")]
    [Produces("video/mp4", "video/quicktime", "video/webm")]
    public async Task<IActionResult> Archivo(int id)
    {
        var a = await videos.ArchivoPropioAsync(id);
        return PhysicalFile(a.Ruta, a.TipoContenido, enableRangeProcessing: true);
    }
}
