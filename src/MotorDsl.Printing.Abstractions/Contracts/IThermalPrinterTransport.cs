using MotorDsl.Core.Models;

namespace MotorDsl.Printing;

public interface IThermalPrinterTransport
{
    string Kind { get; }                                    // "bluetooth", "usb", ...
    bool IsConnected { get; }
    PrinterDevice? CurrentDevice { get; }

    // Capacidades detectadas al conectar; null hasta conectar/detectar. Los transports
    // no aplicables (iOS, fakes) devuelven null.
    PrinterCapabilities? Capabilities => null;
    Task<IReadOnlyList<PrinterDevice>> DiscoverAsync(CancellationToken ct = default);
    Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default);
    Task DisconnectAsync();
    Task WriteBytesAsync(byte[] data, PrinterProfile profile, CancellationToken ct = default);
}
