namespace MotorDsl.Printing;

/// <summary>
/// Entrada del historial de fallos de impresion para los reportes de diagnostico. Guarda la
/// identidad CRUDA del dispositivo (el enmascarado se aplica al construir el reporte, no aca)
/// y NUNCA el payload: solo su longitud en bytes.
/// </summary>
public record PrintFailureEntry(
    DateTimeOffset Timestamp,
    string DeviceId,
    string DeviceName,
    string DeviceKind,
    PrinterCapabilities Capabilities,   // snapshot al momento del fallo
    string ErrorType,                   // del PrintError
    string ErrorMessage,                // del PrintError
    int Attempts,
    int BytesLength,                    // longitud, NUNCA el payload
    string? RenderTarget);              // puede ser null en esta fase
