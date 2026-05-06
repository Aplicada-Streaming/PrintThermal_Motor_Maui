using MotorDsl.Core.Models;

namespace MotorDsl.Printing;

public interface IThermalPrinterTransport
{
    string Kind { get; }                                    // "bluetooth", "usb", ...
    bool IsConnected { get; }
    PrinterDevice? CurrentDevice { get; }
    Task<IReadOnlyList<PrinterDevice>> DiscoverAsync(CancellationToken ct = default);
    Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default);
    Task DisconnectAsync();
    Task WriteBytesAsync(byte[] data, PrinterProfile profile, CancellationToken ct = default);
}
