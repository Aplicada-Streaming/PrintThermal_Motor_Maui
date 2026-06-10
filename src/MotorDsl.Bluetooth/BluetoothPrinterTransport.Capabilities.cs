using MotorDsl.Printing;

namespace MotorDsl.Bluetooth;

/// <summary>
/// Parte pura de la deteccion de capacidades: clasificacion del byte de respuesta de status.
/// Vive fuera de cualquier bloque #if y sin dependencias de plataforma para poder testearse
/// en net10.0 (el proyecto de tests la enlaza por fuente). La I/O real (sondeos sobre el
/// socket) vive en BluetoothPrinterTransport.Detection.cs, bajo #if ANDROID.
/// </summary>
public partial class BluetoothPrinterTransport
{
    /// <summary>
    /// Clasifica la respuesta a DLE EOT n=1. null (timeout, los intentos expiraron) -> NotDetected.
    /// Un byte con los bits fijos del status (bit1=1, bit4=1, bit0=0, bit7=0) -> Supported; un
    /// byte que no encaja se trata como ruido (NotDetected). La excepcion al sondear se mapea a
    /// Unknown aguas arriba, no aca.
    /// </summary>
    internal static CapabilitySupport ClassifyStatusResponse(byte? response)
    {
        if (response is null)
            return CapabilitySupport.NotDetected;

        // DLE EOT n=1 devuelve un byte de status con bits fijos: distingue una respuesta real
        // de ruido en la linea. Mascara 0b1001_0011, valor esperado 0b0001_0010.
        byte b = response.Value;
        bool plausible = (b & 0b1001_0011) == 0b0001_0010;
        return plausible ? CapabilitySupport.Supported : CapabilitySupport.NotDetected;
    }
}
