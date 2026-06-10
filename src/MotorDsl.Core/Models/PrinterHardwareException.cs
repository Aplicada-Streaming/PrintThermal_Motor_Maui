namespace MotorDsl.Core.Models;

/// <summary>
/// Fallo de hardware inequivoco de la impresora detectado por status (fin de papel, tapa
/// abierta). Se clasifica como <see cref="PrintErrorType.Hardware"/>, que el handler por
/// defecto NO reintenta: no tiene sentido reintentar sin papel o con la tapa abierta.
/// </summary>
public class PrinterHardwareException : Exception
{
    public PrinterHardwareException(string message) : base(message) { }
    public PrinterHardwareException(string message, Exception inner) : base(message, inner) { }
}
