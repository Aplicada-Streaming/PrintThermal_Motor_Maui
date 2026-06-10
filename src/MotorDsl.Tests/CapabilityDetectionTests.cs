using MotorDsl.Bluetooth;
using MotorDsl.Printing;

namespace MotorDsl.Tests;

/// <summary>
/// Cubre la parte PURA de la deteccion de capacidades: ClassifyStatusResponse, que mapea el
/// byte de respuesta de DLE EOT n=1 (o el null del timeout) a un CapabilitySupport.
/// </summary>
public class CapabilityDetectionTests
{
    [Fact]
    public void ClassifyStatusResponse_Null_Timeout_IsNotDetected()
        => Assert.Equal(CapabilitySupport.NotDetected, BluetoothPrinterTransport.ClassifyStatusResponse(null));

    [Theory]
    [InlineData(0x12)] // bits fijos del status (bit1=1, bit4=1, bit0=0, bit7=0)
    [InlineData(0x16)] // online, sin errores
    [InlineData(0x1E)]
    public void ClassifyStatusResponse_PlausibleByte_IsSupported(int b)
        => Assert.Equal(CapabilitySupport.Supported, BluetoothPrinterTransport.ClassifyStatusResponse((byte)b));

    [Theory]
    [InlineData(0x00)] // ruido: no encaja con los bits fijos
    [InlineData(0xFF)]
    public void ClassifyStatusResponse_ImplausibleByte_IsNotDetected(int b)
        => Assert.Equal(CapabilitySupport.NotDetected, BluetoothPrinterTransport.ClassifyStatusResponse((byte)b));
}
