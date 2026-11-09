using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;

namespace GestionGimnasios.Web.Tests;

/// <summary>Reglas de la renovación con QR y generación del código (RF-05, HU-50).</summary>
public class PagosQrTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 8);

    [Fact]
    public void Orden_sin_comprobante_no_se_puede_confirmar() =>
        Assert.Contains("comprobante", PagosQrService.MotivoNoConfirmable(new OrdenPagoQr()));

    [Fact]
    public void Orden_pendiente_con_comprobante_se_puede_confirmar() =>
        Assert.Null(PagosQrService.MotivoNoConfirmable(new OrdenPagoQr { ComprobanteEn = DateTime.UtcNow }));

    [Theory]
    [InlineData(EstadosOrdenPago.Confirmada)]
    [InlineData(EstadosOrdenPago.Rechazada)]
    [InlineData(EstadosOrdenPago.Vencida)]
    public void Orden_ya_cerrada_no_se_vuelve_a_confirmar(string estado) =>
        Assert.NotNull(PagosQrService.MotivoNoConfirmable(new OrdenPagoQr { Estado = estado, ComprobanteEn = DateTime.UtcNow }));

    [Fact]
    public void Renovar_un_plan_vigente_suma_desde_su_vencimiento() =>
        Assert.Equal(new DateOnly(2027, 1, 20), PagosQrService.FinRenovado(new DateOnly(2026, 11, 20), Hoy, 2));

    [Fact]
    public void Renovar_un_plan_vencido_cuenta_desde_hoy() =>
        Assert.Equal(new DateOnly(2026, 12, 7), PagosQrService.FinRenovado(new DateOnly(2026, 10, 1), Hoy, 1));

    [Fact]
    public void Monto_es_el_precio_por_los_meses() =>
        Assert.Equal(360.30m, PagosQrService.CalcularMonto(120.10m, 3));

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Meses_fuera_de_rango_es_error_400(int meses) =>
        Assert.Equal(400, Assert.Throws<ErrorNegocio>(() => PagosQrService.ValidarMeses(meses)).Estado);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Comprobante_sin_referencia_es_error_400(string referencia) =>
        Assert.Equal(400, Assert.Throws<ErrorNegocio>(() => PagosQrService.ValidarReferencia(referencia)).Estado);

    [Fact]
    public void Texto_del_qr_identifica_la_orden_y_cabe_en_el_codigo()
    {
        var texto = PagosQrService.TextoQr("7F3A91C2", 119999.88m);
        Assert.Equal("GG-PAGO|7F3A91C2|119999.88|BOB", texto);
        Assert.True(texto.Length <= CodigoQr.CapacidadBytes);
    }

    [Fact]
    public void Matriz_tiene_los_patrones_de_posicion()
    {
        var m = CodigoQr.GenerarMatriz("GG-PAGO|7F3A91C2|120.00|BOB");
        Assert.Equal(29, m.GetLength(0));
        foreach (var (fila, columna) in new[] { (0, 0), (0, 22), (22, 0) })
        {
            for (var i = 0; i < 7; i++)
            {
                Assert.True(m[fila, columna + i]);
                Assert.True(m[fila + 6, columna + i]);
                Assert.True(m[fila + i, columna]);
                Assert.True(m[fila + i, columna + 6]);
            }
            Assert.False(m[fila + 1, columna + 1]);
            Assert.True(m[fila + 3, columna + 3]);
        }

        // Línea de sincronización entre los patrones.
        Assert.True(m[6, 8]);
        Assert.False(m[6, 9]);
    }

    [Fact]
    public void Correccion_de_errores_cumple_reed_solomon()
    {
        var datos = Enumerable.Range(1, 44).Select(i => (byte)(i * 5)).ToArray();
        byte[] palabra = [.. datos, .. CodigoQr.CalcularCorreccion(datos)];

        // Una palabra válida se anula en las 26 raíces del polinomio generador (2^0 a 2^25).
        byte raiz = 1;
        for (var i = 0; i < 26; i++)
        {
            byte suma = 0;
            foreach (var coeficiente in palabra) suma = (byte)(CodigoQr.Multiplicar(suma, raiz) ^ coeficiente);
            Assert.Equal(0, suma);
            raiz = CodigoQr.Multiplicar(raiz, 2);
        }
    }

    [Fact]
    public void Texto_demasiado_largo_se_rechaza() =>
        Assert.Throws<ArgumentException>(() => CodigoQr.GenerarMatriz(new string('A', CodigoQr.CapacidadBytes + 1)));

    [Fact]
    public void Png_tiene_la_firma_del_formato()
    {
        var png = CodigoQr.GenerarPng("GG-PAGO|7F3A91C2|120.00|BOB");
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png[..4]);
    }
}
