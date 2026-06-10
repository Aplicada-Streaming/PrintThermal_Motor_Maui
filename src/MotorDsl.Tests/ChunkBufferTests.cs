using System.Linq;
using MotorDsl.Bluetooth;

namespace MotorDsl.Tests;

/// <summary>
/// Cubre BluetoothPrinterTransport.ChunkBuffer: el chunker de tamano fijo que reemplazo
/// al viejo SplitByLineFeed. Lo importante es que parte por TAMANO y no por contenido,
/// asi que datos con 0x0A intercalados (bitmap escpos) se reensamblan exactos.
/// </summary>
public class ChunkBufferTests
{
    [Fact]
    public void Reensamblar_segmentos_reproduce_el_array_original()
    {
        var data = Enumerable.Range(0, 1000).Select(i => (byte)(i % 256)).ToArray();

        var reensamblado = BluetoothPrinterTransport.ChunkBuffer(data, 256)
            .SelectMany(s => s)
            .ToArray();

        Assert.Equal(data, reensamblado);
    }

    [Fact]
    public void No_se_parte_por_LineFeed_con_0x0A_intercalados()
    {
        // Caso clave: bytes 0x0A salpicados por todo el buffer. El chunker de tamano fijo
        // NO debe usarlos como separador, a diferencia del viejo SplitByLineFeed.
        var data = new byte[700];
        for (int i = 0; i < data.Length; i++)
            data[i] = (i % 3 == 0) ? (byte)0x0A : (byte)(i % 256);

        var segmentos = BluetoothPrinterTransport.ChunkBuffer(data, 256).ToArray();
        var reensamblado = segmentos.SelectMany(s => s).ToArray();

        Assert.Equal(data, reensamblado);
        Assert.All(segmentos, s => Assert.True(s.Count <= 256));
        // 700 bytes / 256 -> 256 + 256 + 188, no una particion guiada por los 0x0A.
        Assert.Equal(3, segmentos.Length);
        Assert.Equal(new[] { 256, 256, 188 }, segmentos.Select(s => s.Count).ToArray());
    }

    [Fact]
    public void Ningun_segmento_excede_el_tamano_maximo()
    {
        var data = new byte[513];

        var segmentos = BluetoothPrinterTransport.ChunkBuffer(data, 128).ToList();

        Assert.All(segmentos, s => Assert.True(s.Count <= 128));
        Assert.Equal(data.Length, segmentos.Sum(s => s.Count));
    }

    [Fact]
    public void Array_vacio_no_produce_segmentos()
    {
        var segmentos = BluetoothPrinterTransport.ChunkBuffer(Array.Empty<byte>(), 256).ToArray();

        Assert.Empty(segmentos);
    }

    [Fact]
    public void Size_mayor_que_el_array_produce_un_unico_segmento()
    {
        var data = new byte[] { 1, 2, 3, 0x0A, 5 };

        var segmentos = BluetoothPrinterTransport.ChunkBuffer(data, 256).ToArray();

        Assert.Single(segmentos);
        Assert.True(segmentos[0].Count <= 256);
        Assert.Equal(data, segmentos[0].ToArray());
    }

    [Fact]
    public void Size_invalido_lanza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BluetoothPrinterTransport.ChunkBuffer(new byte[] { 1, 2, 3 }, 0).ToArray());
    }
}
