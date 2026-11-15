using System.Buffers.Binary;
using System.Globalization;
using GestionGimnasios.Web.Domain;

namespace GestionGimnasios.Web.Services;

/// <summary>Datos técnicos que trae el propio archivo de video. Nulo cuando el archivo no los indica.</summary>
public record MetadatosVideo(double? DuracionSeg, int? Ancho, int? Alto);

/// <summary>Resultado de la revisión automática: lo ve solo el entrenador, que lo confirma o lo corrige.</summary>
public record AnalisisVideo(string Resultado, double? DuracionSeg, int? Ancho, int? Alto, IReadOnlyList<string> Observaciones)
{
    public string? Texto => Observaciones.Count == 0 ? null : string.Join('\n', Observaciones);
}

/// <summary>
/// Análisis preliminar del video (RF-01, CU-35). Sin una tecnología de análisis de movimiento
/// aprobada por el docente, revisa solo lo que se puede comprobar del archivo: duración y
/// resolución, que deciden si el entrenador podrá ver bien la repetición. No evalúa la técnica:
/// esa valoración la da siempre el entrenador.
/// </summary>
public static class AnalizadorVideo
{
    public const double DuracionMinimaSeg = 3;
    public const double DuracionMaximaSeg = 180;
    public const int LadoMinimoPx = 360;

    /// <summary>Lee el archivo guardado y aplica las reglas. Nunca falla: si no puede leerlo, el análisis queda «no disponible».</summary>
    public static AnalisisVideo Analizar(string ruta, string extension)
    {
        try
        {
            using var archivo = File.OpenRead(ruta);
            return Evaluar(LectorMetadatosVideo.Leer(archivo, extension));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or OverflowException)
        {
            return Evaluar(null);
        }
    }

    public static AnalisisVideo Evaluar(MetadatosVideo? m)
    {
        if (m is null || (m.DuracionSeg is null && (m.Ancho is null || m.Alto is null)))
            return new AnalisisVideo(ResultadosAnalisisVideo.NoDisponible, null, null, null,
                ["No se pudieron leer la duración ni la resolución del archivo. Evalúa el video directamente."]);

        var observaciones = new List<string>();
        if (m.DuracionSeg is null) observaciones.Add("El archivo no indica su duración.");
        else if (m.DuracionSeg < DuracionMinimaSeg)
            observaciones.Add($"Dura {Segundos(m.DuracionSeg.Value)}: puede no mostrar una repetición completa.");
        else if (m.DuracionSeg > DuracionMaximaSeg)
            observaciones.Add($"Dura {Duracion(m.DuracionSeg.Value)}: puede convenir pedir solo el fragmento del ejercicio.");

        if (m.Ancho is null || m.Alto is null) observaciones.Add("El archivo no indica su resolución.");
        else if (Math.Min(m.Ancho.Value, m.Alto.Value) < LadoMinimoPx)
            observaciones.Add($"Resolución baja ({m.Ancho}×{m.Alto}): puede no distinguirse bien la postura.");

        var resultado = observaciones.Count == 0 ? ResultadosAnalisisVideo.Apto : ResultadosAnalisisVideo.ConObservaciones;
        return new AnalisisVideo(resultado, m.DuracionSeg, m.Ancho, m.Alto, observaciones);
    }

    /// <summary>Duración legible: 0:42, 2:05.</summary>
    public static string Duracion(double segundos)
    {
        var total = (int)Math.Round(segundos);
        return $"{total / 60}:{total % 60:00}";
    }

    /// <summary>Con coma decimal, sin depender de la cultura instalada en el servidor.</summary>
    private static string Segundos(double s) => s.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',') + " s";
}

/// <summary>
/// Lee duración y resolución de la cabecera de MP4/MOV (cajas moov, mvhd, tkhd) y WebM (EBML),
/// sin librerías externas. Solo lee la cabecera, no decodifica imágenes.
/// </summary>
public static class LectorMetadatosVideo
{
    /// <summary>La cabecera de un video de 50 MB ocupa pocos KB; este tope evita cargar archivos manipulados.</summary>
    private const int MaximoCabecera = 8 * 1024 * 1024;

    public static MetadatosVideo? Leer(Stream s, string extension) =>
        extension == ".webm" ? LeerWebm(s) : LeerMp4(s);

    // ── MP4 / MOV ────────────────────────────────────────────────────────

    /// <summary>Recorre las cajas de primer nivel hasta encontrar moov, que puede estar al principio o al final.</summary>
    public static MetadatosVideo? LeerMp4(Stream s)
    {
        var cabecera = new byte[16];
        while (true)
        {
            var inicio = s.Position;
            if (s.ReadAtLeast(cabecera.AsSpan(0, 8), 8, throwOnEndOfStream: false) < 8) return null;
            long tamano = BinaryPrimitives.ReadUInt32BigEndian(cabecera);
            var tipo = System.Text.Encoding.ASCII.GetString(cabecera, 4, 4);
            var encabezado = 8;
            if (tamano == 1)
            {
                if (s.ReadAtLeast(cabecera.AsSpan(8, 8), 8, throwOnEndOfStream: false) < 8) return null;
                tamano = (long)BinaryPrimitives.ReadUInt64BigEndian(cabecera.AsSpan(8));
                encabezado = 16;
            }
            else if (tamano == 0) tamano = s.Length - inicio;
            if (tamano < encabezado || inicio + tamano > s.Length) return null;

            if (tipo == "moov")
            {
                if (tamano - encabezado > MaximoCabecera) return null;
                var moov = new byte[tamano - encabezado];
                s.ReadExactly(moov);
                return Moov(moov);
            }
            s.Position = inicio + tamano;
        }
    }

    private static MetadatosVideo? Moov(ReadOnlySpan<byte> moov)
    {
        double? duracion = null;
        int? ancho = null, alto = null;
        foreach (var (tipo, datos) in Cajas(moov))
        {
            if (tipo == "mvhd") duracion = Mvhd(datos);
            else if (tipo == "trak" && ancho is null && EsPistaDeVideo(datos))
                (ancho, alto) = Tkhd(datos);
        }
        return new MetadatosVideo(duracion, ancho, alto);
    }

    /// <summary>mvhd: escala de tiempo y duración total de la película.</summary>
    private static double? Mvhd(ReadOnlySpan<byte> d)
    {
        if (d.Length < 20) return null;
        var version = d[0];
        uint escala;
        ulong duracion;
        if (version == 1)
        {
            if (d.Length < 32) return null;
            escala = BinaryPrimitives.ReadUInt32BigEndian(d[20..]);
            duracion = BinaryPrimitives.ReadUInt64BigEndian(d[24..]);
        }
        else
        {
            escala = BinaryPrimitives.ReadUInt32BigEndian(d[12..]);
            duracion = BinaryPrimitives.ReadUInt32BigEndian(d[16..]);
        }
        return escala == 0 || duracion == 0 || duracion == uint.MaxValue ? null : (double)duracion / escala;
    }

    /// <summary>Una pista es de video si su manejador (mdia → hdlr) es «vide».</summary>
    private static bool EsPistaDeVideo(ReadOnlySpan<byte> trak)
    {
        foreach (var (tipo, mdia) in Cajas(trak))
        {
            if (tipo != "mdia") continue;
            foreach (var (t, hdlr) in Cajas(mdia))
                if (t == "hdlr" && hdlr.Length >= 12 && hdlr.AsSpan(8, 4).SequenceEqual("vide"u8)) return true;
        }
        return false;
    }

    /// <summary>tkhd: ancho y alto (16.16) y la matriz, que indica si el celular grabó girado.</summary>
    private static (int?, int?) Tkhd(ReadOnlySpan<byte> trak)
    {
        foreach (var (tipo, d) in Cajas(trak))
        {
            if (tipo != "tkhd") continue;
            var matriz = d[0] == 1 ? 52 : 40;
            if (d.Length < matriz + 44) return (null, null);
            var a = BinaryPrimitives.ReadInt32BigEndian(d[matriz..]);
            var ancho = (int)(BinaryPrimitives.ReadUInt32BigEndian(d[(matriz + 36)..]) >> 16);
            var alto = (int)(BinaryPrimitives.ReadUInt32BigEndian(d[(matriz + 40)..]) >> 16);
            if (ancho == 0 || alto == 0) return (null, null);
            // Girado 90° o 270°: el primer coeficiente de la matriz es 0; se muestra con los lados intercambiados.
            return a == 0 ? (alto, ancho) : (ancho, alto);
        }
        return (null, null);
    }

    private static List<(string Tipo, byte[] Datos)> Cajas(ReadOnlySpan<byte> d)
    {
        var cajas = new List<(string, byte[])>();
        var i = 0;
        while (i + 8 <= d.Length)
        {
            long tamano = BinaryPrimitives.ReadUInt32BigEndian(d[i..]);
            var tipo = System.Text.Encoding.ASCII.GetString(d.Slice(i + 4, 4));
            var encabezado = 8;
            if (tamano == 1)
            {
                if (i + 16 > d.Length) break;
                tamano = (long)BinaryPrimitives.ReadUInt64BigEndian(d[(i + 8)..]);
                encabezado = 16;
            }
            else if (tamano == 0) tamano = d.Length - i;
            if (tamano < encabezado || i + tamano > d.Length) break;
            cajas.Add((tipo, d.Slice(i + encabezado, (int)tamano - encabezado).ToArray()));
            i += (int)tamano;
        }
        return cajas;
    }

    // ── WebM (EBML) ──────────────────────────────────────────────────────

    private const uint IdSegmento = 0x18538067, IdInfo = 0x1549A966, IdPistas = 0x1654AE6B, IdPista = 0xAE, IdVideo = 0xE0, IdCluster = 0x1F43B675;
    private const uint IdEscalaTiempo = 0x2AD7B1, IdDuracion = 0x4489, IdTipoPista = 0x83, IdAncho = 0xB0, IdAlto = 0xBA;

    /// <summary>Info y Tracks van antes de los datos de imagen, así que basta con leer el comienzo del archivo.</summary>
    public static MetadatosVideo? LeerWebm(Stream s)
    {
        var bufer = new byte[(int)Math.Min(s.Length, 1024 * 1024)];
        var leidos = s.ReadAtLeast(bufer, bufer.Length, throwOnEndOfStream: false);
        var d = bufer.AsSpan(0, leidos);

        var i = 0;
        while (Elemento(d, ref i, out var id, out var inicio, out var fin))
        {
            if (id != IdSegmento) { i = fin; continue; }
            ulong escala = 1_000_000;
            double? duracion = null;
            int? ancho = null, alto = null;
            var j = inicio;
            while (j < fin && Elemento(d, ref j, out var hijo, out var hi, out var hf))
            {
                if (hijo == IdInfo) (escala, duracion) = Info(d[hi..hf], escala);
                else if (hijo == IdPistas) (ancho, alto) = Pistas(d[hi..hf]);
                else if (hijo == IdCluster) break;
                j = hf;
            }
            return new MetadatosVideo(duracion is null ? null : duracion * escala / 1e9, ancho, alto);
        }
        return null;
    }

    private static (ulong, double?) Info(ReadOnlySpan<byte> d, ulong escala)
    {
        double? duracion = null;
        var i = 0;
        while (Elemento(d, ref i, out var id, out var a, out var b))
        {
            if (id == IdEscalaTiempo) escala = Entero(d[a..b]);
            else if (id == IdDuracion) duracion = (b - a) switch
            {
                4 => (double?)BinaryPrimitives.ReadSingleBigEndian(d[a..b]),
                8 => BinaryPrimitives.ReadDoubleBigEndian(d[a..b]),
                _ => null,
            };
            i = b;
        }
        return (escala == 0 ? 1_000_000 : escala, duracion is > 0 ? duracion : null);
    }

    private static (int?, int?) Pistas(ReadOnlySpan<byte> d)
    {
        var i = 0;
        while (Elemento(d, ref i, out var id, out var a, out var b))
        {
            if (id == IdPista)
            {
                var pista = d[a..b];
                ulong tipo = 0;
                int? ancho = null, alto = null;
                var j = 0;
                while (Elemento(pista, ref j, out var hijo, out var ha, out var hb))
                {
                    if (hijo == IdTipoPista) tipo = Entero(pista[ha..hb]);
                    else if (hijo == IdVideo)
                    {
                        var video = pista[ha..hb];
                        var k = 0;
                        while (Elemento(video, ref k, out var v, out var va, out var vb))
                        {
                            if (v == IdAncho) ancho = (int)Entero(video[va..vb]);
                            else if (v == IdAlto) alto = (int)Entero(video[va..vb]);
                            k = vb;
                        }
                    }
                    j = hb;
                }
                if (tipo == 1 && ancho > 0 && alto > 0) return (ancho, alto);
            }
            i = b;
        }
        return (null, null);
    }

    /// <summary>Lee un elemento EBML: identificador y tamaño en formato de longitud variable. Si el tamaño es desconocido, llega hasta el final.</summary>
    private static bool Elemento(ReadOnlySpan<byte> d, ref int i, out uint id, out int inicio, out int fin)
    {
        id = 0; inicio = fin = 0;
        if (i >= d.Length) return false;
        var largoId = LargoVariable(d[i], 4);
        if (largoId == 0 || i + largoId > d.Length) return false;
        for (var k = 0; k < largoId; k++) id = (id << 8) | d[i + k];

        var p = i + largoId;
        if (p >= d.Length) return false;
        var largoTamano = LargoVariable(d[p], 8);
        if (largoTamano == 0 || p + largoTamano > d.Length) return false;
        ulong tamano = (ulong)(d[p] & (0xFF >> largoTamano));
        var desconocido = tamano == (ulong)(0xFF >> largoTamano);
        for (var k = 1; k < largoTamano; k++)
        {
            tamano = (tamano << 8) | d[p + k];
            desconocido &= d[p + k] == 0xFF;
        }

        inicio = p + largoTamano;
        fin = desconocido || tamano > (ulong)(d.Length - inicio) ? d.Length : inicio + (int)tamano;
        i = fin;
        return true;
    }

    private static int LargoVariable(byte primero, int maximo)
    {
        for (var n = 1; n <= maximo; n++)
            if ((primero & (0x80 >> (n - 1))) != 0) return n;
        return 0;
    }

    private static ulong Entero(ReadOnlySpan<byte> d)
    {
        ulong v = 0;
        foreach (var b in d[..Math.Min(d.Length, 8)]) v = (v << 8) | b;
        return v;
    }
}
