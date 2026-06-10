using MotorDsl.Bluetooth;
using MotorDsl.Core.Models;

namespace MotorDsl.Tests;

/// <summary>
/// Cubre la parte pura del flow control por status de fase 3: parseo del byte DLE EOT n=1 a
/// PrinterStatus, el latch de degradacion (tras un Unknown se usa pacing fijo el resto del envio)
/// y el mapeo de PrinterHardwareException a PrintErrorType.Hardware.
/// </summary>
public class StatusFlowControlTests
{
    // ── ParseStatusByte ──

    [Theory]
    [InlineData(0x12)] // bits fijos validos, online (bit3=0)
    [InlineData(0x16)]
    public void ParseStatusByte_ValidOnline_IsReady(int b)
        => Assert.Equal(PrinterStatus.Ready, BluetoothPrinterTransport.ParseStatusByte((byte)b));

    [Theory]
    [InlineData(0x1A)] // 0x12 | 0x08 (offline) -> Busy
    [InlineData(0x1E)] // bits fijos validos + offline
    public void ParseStatusByte_Offline_IsBusy(int b)
        => Assert.Equal(PrinterStatus.Busy, BluetoothPrinterTransport.ParseStatusByte((byte)b));

    [Theory]
    [InlineData(0x00)] // bits fijos invalidos -> basura/eco
    [InlineData(0xFF)]
    public void ParseStatusByte_InvalidFixedBits_IsUnknown(int b)
        => Assert.Equal(PrinterStatus.Unknown, BluetoothPrinterTransport.ParseStatusByte((byte)b));

    [Fact]
    public void ParseStatusByte_Null_Timeout_IsUnknown()
        => Assert.Equal(PrinterStatus.Unknown, BluetoothPrinterTransport.ParseStatusByte(null));

    // ── Latch de degradacion ──

    [Fact]
    public void Pacing_Degrades_After_Unknown_AndStaysDegraded()
    {
        // Secuencia simulada: a partir del Unknown (indice 2), todos los bloques usan pacing fijo,
        // aunque despues el status vuelva a Ready/Busy.
        var sequence = new[]
        {
            PrinterStatus.Ready, PrinterStatus.Ready, PrinterStatus.Unknown,
            PrinterStatus.Ready, PrinterStatus.Busy
        };

        bool degraded = false;
        var fixedPacingPerChunk = new List<bool>();
        foreach (var s in sequence)
        {
            bool fixedPacing;
            (degraded, fixedPacing) = BluetoothPrinterTransport.NextPacingDecision(degraded, s);
            fixedPacingPerChunk.Add(fixedPacing);
        }

        Assert.Equal(new[] { false, false, true, true, true }, fixedPacingPerChunk);
        Assert.True(degraded);
    }

    [Fact]
    public void Pacing_Ready_NoFixed_Busy_Fixed_WithoutDegrading()
    {
        var (d1, f1) = BluetoothPrinterTransport.NextPacingDecision(false, PrinterStatus.Ready);
        Assert.False(d1);
        Assert.False(f1); // Ready: escribir ya, sin pacing fijo

        var (d2, f2) = BluetoothPrinterTransport.NextPacingDecision(false, PrinterStatus.Busy);
        Assert.False(d2); // Busy no degrada (puede recuperarse)
        Assert.True(f2);  // pero aplica pacing de respaldo
    }

    // ── Mapeo de error de hardware ──

    [Fact]
    public void PrintError_FromException_MapsPrinterHardwareException_ToHardware()
    {
        var ex = new PrinterHardwareException("paper out");
        var error = PrintError.FromException(ex, attempt: 1, maxAttempts: 3);

        Assert.Equal(PrintErrorType.Hardware, error.Type);
        Assert.Contains("paper out", error.Message);
        Assert.Same(ex, error.InnerException);
    }

    [Fact]
    public async Task DefaultPrintErrorHandler_DoesNotRetry_OnHardware()
    {
        var handler = new MotorDsl.Core.Printing.DefaultPrintErrorHandler();
        var error = PrintError.FromException(new PrinterHardwareException("cover open"), 1, 3);

        var shouldRetry = await handler.HandleErrorAsync(error);

        Assert.False(shouldRetry); // sin papel / tapa abierta: no tiene sentido reintentar
    }
}
