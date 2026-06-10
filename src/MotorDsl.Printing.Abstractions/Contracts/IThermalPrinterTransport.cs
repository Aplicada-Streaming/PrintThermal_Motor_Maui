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

    // Aprovisionamiento de logo en NV. El input gsV0Bytes es el MISMO formato GS v 0 que la app
    // ya produce para imagenes inline. Default no-op para transports no aplicables (iOS, fakes).
    Task<NvLogoResult> ProvisionLogoAsync(byte[] gsV0Bytes, int keycode, CancellationToken ct = default)
        => Task.FromResult(new NvLogoResult(false, null, "transport no soporta NV"));

    Task<bool> IsLogoProvisionedAsync(int keycode, CancellationToken ct = default)
        => Task.FromResult(false);

    Task ClearLogoAsync(int keycode, CancellationToken ct = default)
        => Task.CompletedTask;
}
