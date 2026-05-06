using System.ComponentModel;
using MotorDsl.Core.Models;

namespace MotorDsl.Printing;

public interface IThermalPrinterService : INotifyPropertyChanged
{
    bool IsConnected { get; }
    PrinterDevice? CurrentDevice { get; }
    PrinterConnectionState ConnectionState { get; }
    string? LastError { get; }

    event EventHandler<PrintErrorEventArgs>? ErrorOccurred;
    event EventHandler<DevicesDiscoveredEventArgs>? DevicesDiscovered;

    Task<IReadOnlyList<PrinterDevice>> DiscoverDevicesAsync(string? kind = null, CancellationToken ct = default);
    Task<bool> ConnectAsync(PrinterDevice device, CancellationToken ct = default);
    Task DisconnectAsync();
    Task<bool> ReconnectAsync(CancellationToken ct = default);
    Task SendBytesAsync(byte[] data, PrinterProfile? profile = null, PrintRetryOptions? retry = null, CancellationToken ct = default);
}
