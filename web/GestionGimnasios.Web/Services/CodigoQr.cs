using System.IO.Compression;
using System.Text;

namespace GestionGimnasios.Web.Services;

/// <summary>
/// Genera el código QR de una orden como imagen PNG, sin librerías externas (solo las tecnologías
/// de la guía del curso). Usa siempre QR versión 3 (29 × 29 módulos) con corrección de errores
/// nivel M y modo byte: alcanza para los 42 bytes del texto de una orden. Sigue la norma ISO/IEC 18004.
/// </summary>
public static class CodigoQr
{
    public const int CapacidadBytes = 42;

    private const int Tamano = 29;
    private const int PalabrasDatos = 44;
    private const int PalabrasCorreccion = 26;
    private const int EscalaPng = 8;
    private const int MargenModulos = 4;

    /// <summary>Texto del QR en una imagen PNG en blanco y negro.</summary>
    public static byte[] GenerarPng(string texto) => APng(GenerarMatriz(texto));

    /// <summary>Matriz de módulos: true es negro. El índice es [fila, columna].</summary>
    public static bool[,] GenerarMatriz(string texto)
    {
        var bytes = Encoding.UTF8.GetBytes(texto);
        if (bytes.Length > CapacidadBytes) throw new ArgumentException($"El texto del QR no puede superar {CapacidadBytes} bytes.", nameof(texto));

        var datos = CodificarDatos(bytes);
        var correccion = CalcularCorreccion(datos);
        var modulos = new bool[Tamano, Tamano];
        var reservados = new bool[Tamano, Tamano];

        DibujarPatrones(modulos, reservados);
        ColocarDatos(modulos, reservados, [.. datos, .. correccion]);
        DibujarFormato(modulos, reservados);
        return modulos;
    }

    // ── Datos ────────────────────────────────────────────────────────────

    /// <summary>Modo byte: indicador, longitud, datos, terminador y palabras de relleno.</summary>
    private static byte[] CodificarDatos(byte[] texto)
    {
        var bits = new List<bool>();
        void Agregar(int valor, int cantidad)
        {
            for (var i = cantidad - 1; i >= 0; i--) bits.Add(((valor >> i) & 1) == 1);
        }

        Agregar(0b0100, 4);
        Agregar(texto.Length, 8);
        foreach (var b in texto) Agregar(b, 8);
        Agregar(0, Math.Min(4, PalabrasDatos * 8 - bits.Count));
        while (bits.Count % 8 != 0) bits.Add(false);

        var palabras = new List<byte>();
        for (var i = 0; i < bits.Count; i += 8)
        {
            var valor = 0;
            for (var j = 0; j < 8; j++) valor = (valor << 1) | (bits[i + j] ? 1 : 0);
            palabras.Add((byte)valor);
        }
        for (var relleno = true; palabras.Count < PalabrasDatos; relleno = !relleno)
            palabras.Add(relleno ? (byte)0xEC : (byte)0x11);
        return [.. palabras];
    }

    /// <summary>Palabras de corrección Reed-Solomon sobre GF(256): permiten leer el QR aunque se dañe en parte.</summary>
    public static byte[] CalcularCorreccion(byte[] datos)
    {
        var generador = PolinomioGenerador();
        var resto = new byte[PalabrasCorreccion];
        foreach (var d in datos)
        {
            var factor = (byte)(d ^ resto[0]);
            Array.Copy(resto, 1, resto, 0, PalabrasCorreccion - 1);
            resto[^1] = 0;
            for (var i = 0; i < PalabrasCorreccion; i++) resto[i] ^= Multiplicar(generador[i], factor);
        }
        return resto;
    }

    private static byte[] PolinomioGenerador()
    {
        var polinomio = new byte[PalabrasCorreccion];
        polinomio[^1] = 1;
        byte raiz = 1;
        for (var i = 0; i < PalabrasCorreccion; i++)
        {
            for (var j = 0; j < PalabrasCorreccion; j++)
            {
                polinomio[j] = Multiplicar(polinomio[j], raiz);
                if (j + 1 < PalabrasCorreccion) polinomio[j] ^= polinomio[j + 1];
            }
            raiz = Multiplicar(raiz, 2);
        }
        return polinomio;
    }

    /// <summary>Producto en GF(256) con el polinomio 0x11D que define la norma.</summary>
    public static byte Multiplicar(byte a, byte b)
    {
        var resultado = 0;
        for (var i = 7; i >= 0; i--)
        {
            resultado = (resultado << 1) ^ ((resultado >> 7) * 0x11D);
            resultado ^= ((b >> i) & 1) * a;
        }
        return (byte)resultado;
    }

    // ── Matriz ───────────────────────────────────────────────────────────

    private static void DibujarPatrones(bool[,] m, bool[,] r)
    {
        // Líneas de sincronización: van primero porque los patrones de posición las tapan en las esquinas.
        for (var i = 0; i < Tamano; i++)
        {
            Marcar(m, r, 6, i, i % 2 == 0);
            Marcar(m, r, i, 6, i % 2 == 0);
        }

        // Patrones de posición en tres esquinas, con su separador blanco.
        DibujarBuscador(m, r, 3, 3);
        DibujarBuscador(m, r, Tamano - 4, 3);
        DibujarBuscador(m, r, 3, Tamano - 4);

        // Patrón de alineación de la versión 3.
        for (var dy = -2; dy <= 2; dy++)
            for (var dx = -2; dx <= 2; dx++)
                Marcar(m, r, 22 + dx, 22 + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);

        // Zonas de la información de formato (se escriben al final).
        for (var i = 0; i < 9; i++)
        {
            r[8, i] = true;
            r[i, 8] = true;
        }
        for (var i = 0; i < 8; i++)
        {
            r[8, Tamano - 1 - i] = true;
            r[Tamano - 1 - i, 8] = true;
        }
    }

    private static void DibujarBuscador(bool[,] m, bool[,] r, int cx, int cy)
    {
        for (var dy = -4; dy <= 4; dy++)
            for (var dx = -4; dx <= 4; dx++)
            {
                int x = cx + dx, y = cy + dy;
                if (x is < 0 or >= Tamano || y is < 0 or >= Tamano) continue;
                var distancia = Math.Max(Math.Abs(dx), Math.Abs(dy));
                Marcar(m, r, x, y, distancia != 2 && distancia != 4);
            }
    }

    private static void Marcar(bool[,] m, bool[,] r, int x, int y, bool negro)
    {
        m[y, x] = negro;
        r[y, x] = true;
    }

    /// <summary>Recorre la matriz en zigzag desde la esquina inferior derecha, aplicando la máscara 0.</summary>
    private static void ColocarDatos(bool[,] m, bool[,] r, byte[] palabras)
    {
        var bit = 0;
        for (var derecha = Tamano - 1; derecha >= 1; derecha -= 2)
        {
            if (derecha == 6) derecha = 5;
            var haciaArriba = ((derecha + 1) & 2) == 0;
            for (var vertical = 0; vertical < Tamano; vertical++)
            {
                var y = haciaArriba ? Tamano - 1 - vertical : vertical;
                for (var j = 0; j < 2; j++)
                {
                    var x = derecha - j;
                    if (r[y, x]) continue;
                    var valor = bit < palabras.Length * 8 && ((palabras[bit / 8] >> (7 - bit % 8)) & 1) == 1;
                    bit++;
                    m[y, x] = valor ^ ((x + y) % 2 == 0);
                }
            }
        }
    }

    /// <summary>Nivel M y máscara 0, con su código BCH; va escrito dos veces para resistir daños.</summary>
    private static void DibujarFormato(bool[,] m, bool[,] r)
    {
        const int datos = 0;
        var resto = datos;
        for (var i = 0; i < 10; i++) resto = (resto << 1) ^ ((resto >> 9) * 0x537);
        var formato = ((datos << 10) | resto) ^ 0x5412;
        bool Bit(int i) => ((formato >> i) & 1) == 1;

        for (var i = 0; i <= 5; i++) Marcar(m, r, 8, i, Bit(i));
        Marcar(m, r, 8, 7, Bit(6));
        Marcar(m, r, 8, 8, Bit(7));
        Marcar(m, r, 7, 8, Bit(8));
        for (var i = 9; i < 15; i++) Marcar(m, r, 14 - i, 8, Bit(i));

        for (var i = 0; i < 8; i++) Marcar(m, r, Tamano - 1 - i, 8, Bit(i));
        for (var i = 8; i < 15; i++) Marcar(m, r, 8, Tamano - 15 + i, Bit(i));
        Marcar(m, r, 8, Tamano - 8, true);
    }

    // ── Imagen PNG ───────────────────────────────────────────────────────

    private static byte[] APng(bool[,] modulos)
    {
        var lado = (Tamano + 2 * MargenModulos) * EscalaPng;
        var bytesPorFila = (lado + 7) / 8;
        var crudo = new byte[(bytesPorFila + 1) * lado];
        for (var y = 0; y < lado; y++)
        {
            var inicio = y * (bytesPorFila + 1) + 1;
            for (var x = 0; x < lado; x++)
            {
                int columna = x / EscalaPng - MargenModulos, fila = y / EscalaPng - MargenModulos;
                var negro = columna is >= 0 and < Tamano && fila is >= 0 and < Tamano && modulos[fila, columna];
                if (!negro) crudo[inicio + x / 8] |= (byte)(0x80 >> (x % 8));
            }
        }

        using var comprimido = new MemoryStream();
        using (var zlib = new ZLibStream(comprimido, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(crudo);

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        EscribirBloque(png, "IHDR", [.. EnteroGrande(lado), .. EnteroGrande(lado), 1, 0, 0, 0, 0]);
        EscribirBloque(png, "IDAT", comprimido.ToArray());
        EscribirBloque(png, "IEND", []);
        return png.ToArray();
    }

    private static void EscribirBloque(Stream salida, string tipo, byte[] datos)
    {
        byte[] conTipo = [.. Encoding.ASCII.GetBytes(tipo), .. datos];
        salida.Write(EnteroGrande(datos.Length));
        salida.Write(conTipo);
        salida.Write(EnteroGrande((int)Crc32(conTipo)));
    }

    private static byte[] EnteroGrande(int valor) =>
        [(byte)(valor >> 24), (byte)(valor >> 16), (byte)(valor >> 8), (byte)valor];

    private static uint Crc32(byte[] datos)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var d in datos)
        {
            crc ^= d;
            for (var i = 0; i < 8; i++) crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        return ~crc;
    }
}
