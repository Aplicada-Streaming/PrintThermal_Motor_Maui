using System.ComponentModel;
using MotorDsl.Core.Models;

namespace MotorDsl.Printing;

public interface IThermalPrinterService : INotifyPropertyChanged
{
    bool IsConnected { get; }
    PrinterDevice? CurrentDevice { get; }
    PrinterConnectionState ConnectionState { get; }
    string? LastError { get; }

    // Capacidades de la impresora actualmente vinculada (delega en el transport activo);
    // null si no hay transport o no se detectaron.
    PrinterCapabilities? CurrentCapabilities { get; }

    // Historial acotado de fallos de impresion recientes (mas nuevo al final).
    IReadOnlyList<PrintFailureEntry> RecentFailures { get; }

    // Limpia el historial de fallos.
    void ClearFailures();

    event EventHandler<PrintErrorEventArgs>? ErrorOccurred;
    event EventHandler<DevicesDiscoveredEventArgs>? DevicesDiscovered;

    Task<IReadOnlyList<PrinterDevice>> DiscoverDevicesAsync(string? kind = null, CancellationToken ct = default);
    Task<bool> ConnectAsync(PrinterDevice device, CancellationToken ct = default);
    Task DisconnectAsync();
    Task<bool> ReconnectAsync(CancellationToken ct = default);
    Task SendBytesAsync(byte[] data, PrinterProfile? profile = null, PrintRetryOptions? retry = null, CancellationToken ct = default);

    // Aprovisionamiento de logo NV (delega en el transport activo). Si no hay transport o no
    // esta conectado, ProvisionLogoAsync devuelve NvLogoResult(false, ...).
    Task<NvLogoResult> ProvisionLogoAsync(byte[] gsV0Bytes, int keycode, CancellationToken ct = default);
    Task<bool> IsLogoProvisionedAsync(int keycode, CancellationToken ct = default);
    Task ClearLogoAsync(int keycode, CancellationToken ct = default);
}
