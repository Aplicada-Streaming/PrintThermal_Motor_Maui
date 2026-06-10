#if ANDROID
using MotorDsl.Printing;

namespace MotorDsl.Bluetooth;

/// <summary>
/// Rama ANDROID de la deteccion de capacidades: sondeos sobre el socket (DLE EOT, GS ( L,
/// GS I) con timeouts duros. TODO esta defendido: la deteccion nunca hace fallar la conexion
/// y ninguna lectura puede colgar. La clasificacion pura del status vive en
/// BluetoothPrinterTransport.Capabilities.cs.
/// </summary>
public partial class BluetoothPrinterTransport
{
    /// <summary>
    /// Deteccion puntual al conectar, acotada en tiempo (worst case ~2s): (1) vacia el buffer
    /// de entrada, (2) sondea feedback, (3) sondea NV, (4) intenta ModelId best-effort.
    /// </summary>
    private async Task<PrinterCapabilities> DetectCapabilitiesAsync(CancellationToken ct)
    {
        // (1) descartar respuestas viejas pendientes
        await DrainInputAsync(ct);

        // (2) feedback de status (DLE EOT)
        var feedback = await ProbeStatusFeedbackAsync(ct);

        // (3) NV graphics (consulta NO intrusiva via GS ( L)
        var (nvGraphics, nvKind) = await ProbeNvGraphicsAsync(ct);

        // (4) ModelId best-effort (GS I)
        var modelId = await ProbeModelIdAsync(ct);

        return new PrinterCapabilities(feedback, nvGraphics, nvKind, modelId);
    }

    /// <summary>
    /// Vacia el buffer de entrada descartando bytes pendientes. Lectura acotada por timeout
    /// corto: cuando no hay nada, ReadWithTimeoutAsync devuelve null y se corta. Nunca lanza.
    /// </summary>
    private async Task DrainInputAsync(CancellationToken ct)
    {
        try
        {
            for (int i = 0; i < 3; i++)
            {
                var chunk = await ReadWithTimeoutAsync(64, 50, ct);
                if (chunk == null || chunk.Length == 0) break;
            }
        }
        catch { /* best-effort: nunca propagar desde la deteccion */ }
    }

    /// <summary>
    /// Sondea feedback de status: envia DLE EOT n=1, espera 1 byte con timeout ~400ms, hasta 3
    /// veces. Supported si vuelve un byte plausible; NotDetected si los 3 expiran; Unknown ante
    /// excepcion.
    /// </summary>
    private async Task<CapabilitySupport> ProbeStatusFeedbackAsync(CancellationToken ct)
    {
        try
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                await WriteRawAsync(new byte[] { 0x10, 0x04, 0x01 }, ct); // DLE EOT n=1
                var resp = await ReadWithTimeoutAsync(1, 400, ct);
                var status = ClassifyStatusResponse(resp is { Length: > 0 } ? resp[0] : (byte?)null);
                if (status == CapabilitySupport.Supported)
                    return CapabilitySupport.Supported;
            }
            return CapabilitySupport.NotDetected; // los 3 intentos expiraron / ruido
        }
        catch
        {
            return CapabilitySupport.Unknown; // excepcion al sondear
        }
    }

    /// <summary>
    /// Sondea NV graphics SOLO con la consulta no intrusiva de capacidad (GS ( L, transmitir la
    /// capacidad del area NV graphics). Si responde -> Supported/"gsl"; si no -> NotDetected.
    /// </summary>
    private async Task<(CapabilitySupport, string?)> ProbeNvGraphicsAsync(CancellationToken ct)
    {
        try
        {
            // GS ( L pL pH m fn  con m=48 (0x30), fn=48 (0x30): "transmit the remaining capacity
            // of the NV graphics area". Es de solo lectura, no define ni imprime nada.
            await WriteRawAsync(new byte[] { 0x1D, 0x28, 0x4C, 0x02, 0x00, 0x30, 0x30 }, ct);
            var resp = await ReadWithTimeoutAsync(8, 400, ct);
            if (resp != null && resp.Length > 0)
                return (CapabilitySupport.Supported, "gsl");

            // La familia FS (FS q / FS p) NO se puede sondear de forma no intrusiva al conectar
            // (definir/imprimir es intrusivo); su soporte real se confirmara en la fase 4 al
            // aprovisionar (define+verify). Por eso aca queda NotDetected y NvGraphicsKind null.
            return (CapabilitySupport.NotDetected, null);
        }
        catch
        {
            return (CapabilitySupport.Unknown, null);
        }
    }

    /// <summary>
    /// Intenta el ModelId via GS I n (transmit printer ID) con timeout corto. Best-effort: lo
    /// que vuelva se guarda como string limpio, o null. No condiciona nada.
    /// </summary>
    private async Task<string?> ProbeModelIdAsync(CancellationToken ct)
    {
        try
        {
            await WriteRawAsync(new byte[] { 0x1D, 0x49, 0x43 }, ct); // GS I 67: model name
            var resp = await ReadWithTimeoutAsync(24, 250, ct);
            if (resp == null || resp.Length == 0) return null;

            var text = System.Text.Encoding.ASCII.GetString(resp).Trim('\0', ' ', '\r', '\n', '\t');
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Escribe una secuencia de comando cruda (sin pacing) y hace flush.</summary>
    private async Task WriteRawAsync(byte[] cmd, CancellationToken ct)
    {
        var output = _outputStream;
        if (output == null) return;
        await output.WriteAsync(cmd, 0, cmd.Length, ct);
        await output.FlushAsync(ct);
    }

    /// <summary>
    /// Lee hasta <paramref name="expectedLen"/> bytes con timeout DURO: el Read bloqueante corre
    /// en una tarea de fondo y se cursa contra un Task.Delay. Si gana el timeout devuelve null
    /// sin lanzar. Devuelve el subarray realmente leido (puede ser mas corto que expectedLen).
    ///
    /// ADVERTENCIA: el Read huerfano puede quedar vivo bloqueado hasta que lleguen bytes o se
    /// cierre el socket, y podria consumir una respuesta posterior. Esto es aceptable para la
    /// deteccion best-effort de esta fase; la fase 3 implementara el loop de status definitivo
    /// con manejo propio y unico del InputStream.
    /// </summary>
    private async Task<byte[]?> ReadWithTimeoutAsync(int expectedLen, int timeoutMs, CancellationToken ct)
    {
        var input = _inputStream;
        if (input == null) return null;

        var buffer = new byte[expectedLen];
        var readTask = Task.Run(() =>
        {
            try { return input.Read(buffer, 0, expectedLen); }
            catch { return -1; }
        }, ct);

        var winner = await Task.WhenAny(readTask, Task.Delay(timeoutMs, ct));
        if (winner != readTask)
            return null; // gano el timeout: dejamos el Read huerfano (ver advertencia)

        int n;
        try { n = await readTask; }
        catch { return null; }

        if (n <= 0) return null;
        if (n == expectedLen) return buffer;

        var trimmed = new byte[n];
        Array.Copy(buffer, trimmed, n);
        return trimmed;
    }
}
#endif
