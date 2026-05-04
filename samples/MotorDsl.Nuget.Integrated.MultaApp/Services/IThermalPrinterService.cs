using MotorDsl.Core.Models;

namespace MotorDsl.Nuget.Integrated.MultaApp.Services;

/// <summary>
/// Interface simplificada para el servicio de impresión térmica por Bluetooth.
/// Solo transporta bytes — el formateo lo hace el motor DSL.
/// </summary>
public interface IThermalPrinterService
{
    bool IsConnected { get; }
    Task<List<BluetoothDevice>> ScanDevicesAsync();
    Task<bool> ConnectAsync(string deviceAddress);
    Task DisconnectAsync();
    Task SendBytesAsync(byte[] data, PrinterProfile? profile = null, PrintRetryOptions? retryOptions = null);
}

public class BluetoothDevice
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsPaired { get; set; }
    public override string ToString() => $"{Name} ({Address})";
}
