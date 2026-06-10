#if ANDROID
using MotorDsl.Core.Models;
using MotorDsl.Printing;

namespace MotorDsl.Bluetooth;

/// <summary>
/// Rama ANDROID de la deteccion de capacidades (fase 2) y del flow control por status (fase 3):
/// sondeos y consultas sobre el socket (DLE EOT, GS ( L, GS I). TODO esta defendido: nunca hace
/// fallar la conexion por un sondeo y NINGUNA lectura bloquea: los reads se gatean por Available()
/// y solo se lee cuando hay datos disponibles (sin Read huerfanos). La logica pura de parseo de
/// status y de decision de pacing vive en BluetoothPrinterTransport.Capabilities.cs.
/// </summary>
public partial class BluetoothPrinterTransport
{
    // Flow control: cada cuantos bloques consultar el status. 1 = antes de cada bloque; se puede
    // subir para reducir latencia a costa de menos gating.
    private const int POLL_EVERY_N_CHUNKS = 1;
    // Cuantas veces reintentar el status esperando Ready antes de escribir igual (no deadlock).
    private const int MAX_READY_POLLS = 10;
    // Delay corto entre polls de status mientras la impresora reporta Busy.
    private const int READY_POLL_DELAY_MS = 50;
    // Timeout acotado de cada consulta de status (300-400ms): el envio nunca cuelga por polling.
    private const int STATUS_TIMEOUT_MS = 350;

    // ─────────────────────────────────────────────────────────────────────────────
    // Deteccion de capacidades (fase 2), ahora sobre los reads gateados de fase 3.
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Deteccion puntual al conectar, acotada en tiempo: (1) vacia el buffer de entrada, (2) sondea
    /// feedback, (3) sondea NV, (4) intenta ModelId best-effort.
    /// </summary>
    private async Task<PrinterCapabilities> DetectCapabilitiesAsync(CancellationToken ct)
    {
        DrainInput(); // (1) descartar respuestas viejas pendientes (no bloquea, no lanza)

        var feedback = await ProbeStatusFeedbackAsync(ct);          // (2)
        var (nvGraphics, nvKind) = await ProbeNvGraphicsAsync(ct);  // (3)
        var modelId = await ProbeModelIdAsync(ct);                  // (4)

        return new PrinterCapabilities(feedback, nvGraphics, nvKind, modelId);
    }

    /// <summary>
    /// Sondea feedback de status: envia DLE EOT n=1, espera 1 byte ~400ms, hasta 3 veces. Supported
    /// si vuelve un byte plausible; NotDetected si los 3 expiran; Unknown ante excepcion.
    /// </summary>
    private async Task<CapabilitySupport> ProbeStatusFeedbackAsync(CancellationToken ct)
    {
        try
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                await WriteRawAsync(new byte[] { 0x10, 0x04, 0x01 }, ct); // DLE EOT n=1
                var b = await ReadStatusByteAsync(400);
                if (ClassifyStatusResponse(b) == CapabilitySupport.Supported)
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
    /// Sondea NV graphics SOLO con la consulta no intrusiva de capacidad (GS ( L). Si responde ->
    /// Supported/"gsl"; si no -> NotDetected.
    /// </summary>
    private async Task<(CapabilitySupport, string?)> ProbeNvGraphicsAsync(CancellationToken ct)
    {
        try
        {
            // GS ( L pL pH m fn  con m=48 (0x30), fn=48 (0x30): "transmit the remaining capacity
            // of the NV graphics area". Es de solo lectura, no define ni imprime nada.
            await WriteRawAsync(new byte[] { 0x1D, 0x28, 0x4C, 0x02, 0x00, 0x30, 0x30 }, ct);
            var resp = await ReadAvailableAsync(8, 400);
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
    /// Intenta el ModelId via GS I n (transmit printer ID) con timeout corto. Best-effort: lo que
    /// vuelva se guarda como string limpio, o null. No condiciona nada.
    /// </summary>
    private async Task<string?> ProbeModelIdAsync(CancellationToken ct)
    {
        try
        {
            await WriteRawAsync(new byte[] { 0x1D, 0x49, 0x43 }, ct); // GS I 67: model name
            var resp = await ReadAvailableAsync(24, 250);
            if (resp == null || resp.Length == 0) return null;

            var text = System.Text.Encoding.ASCII.GetString(resp).Trim('\0', ' ', '\r', '\n', '\t');
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch
        {
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Flow control por status (fase 3).
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Consulta el estado real: DrainInput, envia DLE EOT n=1 y parsea 1 byte. El WRITE del comando
    /// NO se traga: si el socket esta muerto, la IOException sube y WriteBytesAsync invalida +
    /// reconecta ANTES de escribir el bloque a ciegas. La LECTURA si es best-effort: si falla o
    /// expira, ParseStatusByte(null) devuelve Unknown (degrada, no aborta).
    /// </summary>
    private async Task<PrinterStatus> QueryStatusAsync(int timeoutMs, CancellationToken ct)
    {
        DrainInput();
        await WriteRawAsync(new byte[] { 0x10, 0x04, 0x01 }, ct); // DLE EOT n=1
        var b = await ReadStatusByteAsync(timeoutMs);
        return ParseStatusByte(b);
    }

    /// <summary>
    /// Poll de QueryStatusAsync hasta Ready o hasta MAX_READY_POLLS. Ready -> Ready; sigue Busy tras
    /// los reintentos -> Busy (se escribira igual, con pacing de respaldo); status ilegible ->
    /// Unknown (degradar al pacing fijo por el resto del envio).
    /// </summary>
    private async Task<PrinterStatus> WaitUntilReadyAsync(CancellationToken ct)
    {
        for (int poll = 0; poll < MAX_READY_POLLS; poll++)
        {
            var status = await QueryStatusAsync(STATUS_TIMEOUT_MS, ct);
            if (status == PrinterStatus.Unknown) return PrinterStatus.Unknown; // ilegible -> degradar
            if (status == PrinterStatus.Ready) return PrinterStatus.Ready;
            await Task.Delay(READY_POLL_DELAY_MS, ct); // Busy: esperar y reintentar
        }
        return PrinterStatus.Busy; // sigue Busy tras MAX_READY_POLLS: no deadlockear, escribir igual
    }

    /// <summary>
    /// Fast-fail SOLO ante señal inequivoca de fin de papel o tapa abierta (DLE EOT n=4 sensor de
    /// papel, n=2 causa offline). Lanza PrinterHardwareException para que el servicio lo clasifique
    /// como Hardware y NO reintente. Ante lectura ambigua, invalida o fallo de read: NO falla, sigue
    /// como si estuviera Ready (degradar). El WRITE puede tirar IOException (socket muerto): sube y
    /// WriteBytesAsync invalida+reconecta. Se chequea UNA vez antes del envio para no inflar latencia.
    /// </summary>
    private async Task CheckHardwareFastFailAsync(CancellationToken ct)
    {
        // Sensor de papel (n=4): paper end = bits 5,6 set (0x60).
        DrainInput();
        await WriteRawAsync(new byte[] { 0x10, 0x04, 0x04 }, ct); // DLE EOT n=4
        var paper = await ReadStatusByteAsync(STATUS_TIMEOUT_MS);
        if (paper is byte pb && HasValidStatusFixedBits(pb) && (pb & 0x60) == 0x60)
            throw new PrinterHardwareException("paper out");

        // Causa offline (n=2): tapa abierta = bit2 (0x04); paper-end stop = bit5 (0x20).
        DrainInput();
        await WriteRawAsync(new byte[] { 0x10, 0x04, 0x02 }, ct); // DLE EOT n=2
        var off = await ReadStatusByteAsync(STATUS_TIMEOUT_MS);
        if (off is byte ob && HasValidStatusFixedBits(ob))
        {
            if ((ob & 0x04) != 0) throw new PrinterHardwareException("cover open");
            if ((ob & 0x20) != 0) throw new PrinterHardwareException("paper out");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers de I/O: escritura cruda y lecturas gateadas por Available() (no bloquean).
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Escribe una secuencia de comando cruda (sin pacing) y hace flush.</summary>
    private async Task WriteRawAsync(byte[] cmd, CancellationToken ct)
    {
        var output = _outputStream;
        if (output == null) return;
        await output.WriteAsync(cmd, 0, cmd.Length, ct);
        await output.FlushAsync(ct);
    }

    /// <summary>
    /// Descarta los bytes pendientes en el stream de entrada usando Available() para NO bloquear.
    /// Best-effort: nunca lanza. Evita que respuestas viejas contaminen el proximo read de status.
    /// </summary>
    private void DrainInput()
    {
        try
        {
            var java = _javaInputStream;
            if (java == null) return;

            var buf = new byte[256];
            for (int guard = 0; guard < 32; guard++)
            {
                int avail = java.Available();
                if (avail <= 0) break;
                int n = java.Read(buf, 0, Math.Min(avail, buf.Length));
                if (n <= 0) break;
            }
        }
        catch { /* swallow: best-effort, nunca lanza */ }
    }

    /// <summary>
    /// Espera hasta que Available()>0 o se cumpla el timeout; recien ahi lee lo disponible (hasta
    /// maxLen bytes) SIN bloquear. Devuelve null si expira. NUNCA bloquea indefinidamente ni lanza.
    /// Gatear por Available() es lo que evita los Read huerfanos del patron de la fase 2.
    /// </summary>
    private async Task<byte[]?> ReadAvailableAsync(int maxLen, int timeoutMs)
    {
        try
        {
            var java = _javaInputStream;
            if (java == null) return null;

            const int pollMs = 10;
            int waited = 0;
            while (true)
            {
                int avail = java.Available();
                if (avail > 0)
                {
                    int toRead = Math.Min(avail, maxLen);
                    var buf = new byte[toRead];
                    int n = java.Read(buf, 0, toRead);
                    if (n <= 0) return null;
                    if (n == buf.Length) return buf;

                    var trimmed = new byte[n];
                    Array.Copy(buf, trimmed, n);
                    return trimmed;
                }

                if (waited >= timeoutMs) return null; // timeout: nada disponible
                await Task.Delay(pollMs);
                waited += pollMs;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Lee 1 byte de status gateado por Available(); null si expira. No bloquea ni lanza.</summary>
    private async Task<byte?> ReadStatusByteAsync(int timeoutMs)
    {
        var r = await ReadAvailableAsync(1, timeoutMs);
        return r is { Length: > 0 } ? r[0] : (byte?)null;
    }
}
#endif
