using MotorDsl.Printing;

namespace MotorDsl.Bluetooth;

/// <summary>
/// Estado de la impresora segun el byte de status DLE EOT n=1.
/// LIMITE HONESTO: DLE EOT da liveness/papel/error, NO una cuenta de bytes libres del buffer.
/// </summary>
internal enum PrinterStatus
{
    Ready,    // online y listo para recibir
    Busy,     // offline (ocupada / no lista) en este instante
    Unknown   // status ilegible: timeout, basura o eco -> degradar a pacing fijo
}

/// <summary>
/// Parte pura de la deteccion y del flow control: clasificacion del byte de status, parseo a
/// PrinterStatus y decision de pacing. Vive fuera de cualquier bloque #if y sin dependencias de
/// plataforma para poder testearse en net10.0 (el proyecto de tests la enlaza por fuente). La
/// I/O real (sondeos y gating sobre el socket) vive en BluetoothPrinterTransport.Detection.cs.
/// </summary>
public partial class BluetoothPrinterTransport
{
    /// <summary>
    /// Valida los bits fijos del status real DLE EOT (bit0=0, bit1=1, bit4=1, bit7=0). Sirve de
    /// sanity check para descartar ruido/eco en la linea. Mascara 0b1001_0011, esperado 0b0001_0010.
    /// </summary>
    internal static bool HasValidStatusFixedBits(byte b)
        => (b & 0b1001_0011) == 0b0001_0010;

    /// <summary>
    /// Clasifica la respuesta a DLE EOT n=1 para la deteccion de capacidad de feedback (fase 2).
    /// null (timeout) -> NotDetected; byte con bits fijos validos -> Supported; ruido -> NotDetected.
    /// La excepcion al sondear se mapea a Unknown aguas arriba, no aca.
    /// </summary>
    internal static CapabilitySupport ClassifyStatusResponse(byte? response)
    {
        if (response is null)
            return CapabilitySupport.NotDetected;

        return HasValidStatusFixedBits(response.Value)
            ? CapabilitySupport.Supported
            : CapabilitySupport.NotDetected;
    }

    /// <summary>
    /// Parsea el byte de status DLE EOT n=1 a PrinterStatus. null/timeout -> Unknown; bits fijos
    /// invalidos (basura/eco) -> Unknown; bit3 (0x08, offline) -> Busy; sin offline -> Ready.
    /// </summary>
    internal static PrinterStatus ParseStatusByte(byte? b)
    {
        if (b is null)
            return PrinterStatus.Unknown;

        byte v = b.Value;
        if (!HasValidStatusFixedBits(v))
            return PrinterStatus.Unknown;

        return (v & 0x08) != 0 ? PrinterStatus.Busy : PrinterStatus.Ready;
    }

    /// <summary>
    /// Decide, para un bloque, si aplicar el pacing fijo de fase 1 y si quedar degradado, dado el
    /// estado previo de degradacion y el resultado del status de ese bloque. Una vez degradado
    /// (un Unknown), se queda degradado por el resto del envio. Pura para poder testear el latch.
    /// </summary>
    internal static (bool degraded, bool fixedPacing) NextPacingDecision(bool degraded, PrinterStatus outcome)
    {
        if (degraded)
            return (true, true); // ya degradado: pacing fijo para todo lo que resta

        return outcome switch
        {
            PrinterStatus.Ready => (false, false), // listo: escribir ya, sin pacing fijo
            PrinterStatus.Busy => (false, true),   // sigue ocupada: escribir igual con pacing de respaldo
            _ => (true, true)                      // Unknown: degradar desde este bloque en adelante
        };
    }
}
