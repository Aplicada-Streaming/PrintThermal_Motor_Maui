using System.ComponentModel;
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;
using MotorDsl.Printing;

namespace MotorDsl.Tests;

/// <summary>Rasterizador fake determinista para tests del renderer: 2 bytes/fila x 2 dots.</summary>
internal sealed class FakeRasterizer : IBitmapRasterizer
{
    public RasterizedImage Rasterize(string source, int widthPixels)
        => new(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, widthBytes: 2, heightDots: 2);
}

/// <summary>Transport fake para tests del servicio: opcionalmente lanza al escribir.</summary>
internal sealed class FakeTransport : IThermalPrinterTransport
{
    public bool ThrowOnWrite { get; set; }
    public string Kind => "fake";
    public bool IsConnected { get; private set; }
    public PrinterDevice? CurrentDevice { get; private set; }
    public PrinterCapabilities? Capabilities { get; set; }

    public Task<IReadOnlyList<PrinterDevice>> DiscoverAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PrinterDevice>>(Array.Empty<PrinterDevice>());

    public Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default)
    {
        IsConnected = true;
        CurrentDevice = new PrinterDevice(deviceId, "Fake", "fake");
        return Task.FromResult(true);
    }

    public Task DisconnectAsync()
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task WriteBytesAsync(byte[] data, PrinterProfile profile, CancellationToken ct = default)
    {
        if (ThrowOnWrite) throw new System.IO.IOException("Broken pipe (fake)");
        return Task.CompletedTask;
    }
}

/// <summary>Stub del servicio para tests del DiagnosticsBuilder: solo expone estado.</summary>
internal sealed class FakePrinterService : IThermalPrinterService
{
    public bool IsConnected => true;
    public PrinterDevice? CurrentDevice { get; set; }
    public PrinterConnectionState ConnectionState { get; set; } = PrinterConnectionState.Connected;
    public string? LastError => null;
    public PrinterCapabilities? CurrentCapabilities { get; set; }
    public IReadOnlyList<PrintFailureEntry> RecentFailures { get; set; } = Array.Empty<PrintFailureEntry>();
    public void ClearFailures() { }

#pragma warning disable CS0067 // eventos requeridos por la interfaz, no usados en el stub
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<PrintErrorEventArgs>? ErrorOccurred;
    public event EventHandler<DevicesDiscoveredEventArgs>? DevicesDiscovered;
#pragma warning restore CS0067

    public Task<IReadOnlyList<PrinterDevice>> DiscoverDevicesAsync(string? kind = null, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<bool> ConnectAsync(PrinterDevice device, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task DisconnectAsync() => throw new NotImplementedException();
    public Task<bool> ReconnectAsync(CancellationToken ct = default) => throw new NotImplementedException();
    public Task SendBytesAsync(byte[] data, PrinterProfile? profile = null, PrintRetryOptions? retry = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<NvLogoResult> ProvisionLogoAsync(byte[] gsV0Bytes, int keycode, CancellationToken ct = default)
        => Task.FromResult(new NvLogoResult(false, null, "fake"));
    public Task<bool> IsLogoProvisionedAsync(int keycode, CancellationToken ct = default)
        => Task.FromResult(false);
    public Task ClearLogoAsync(int keycode, CancellationToken ct = default)
        => Task.CompletedTask;
}
