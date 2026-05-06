#if ANDROID
using Android.Bluetooth;
using Android.Content;
using Java.Util;
using AndroidBluetoothDevice = Android.Bluetooth.BluetoothDevice;
#endif
using MotorDsl.Core.Models;
using MotorDsl.Printing;

namespace MotorDsl.Bluetooth;

/// <summary>
/// Transport Bluetooth Classic SPP (Android).
/// iOS lanza PlatformNotSupportedException porque no soporta Bluetooth clasico SPP.
/// Solo escribe bytes — el retry y la orquestacion se manejan en ThermalPrinterService.
/// </summary>
public class BluetoothPrinterTransport : IThermalPrinterTransport
{
#if ANDROID
    private BluetoothSocket? _socket;
    private BluetoothAdapter? _bluetoothAdapter;
    private System.IO.Stream? _outputStream;
#endif

    private string? _lastDeviceAddress;
    private PrinterDevice? _currentDevice;

    public string Kind => "bluetooth";
    public bool IsConnected { get; private set; }
    public PrinterDevice? CurrentDevice => _currentDevice;

    public BluetoothPrinterTransport()
    {
#if ANDROID
        _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
#endif
    }

    public Task<IReadOnlyList<PrinterDevice>> DiscoverAsync(CancellationToken ct = default)
    {
        var devices = new List<PrinterDevice>();

#if ANDROID
        if (_bluetoothAdapter == null)
            throw new Exception("Bluetooth no esta disponible en este dispositivo");

        if (!_bluetoothAdapter.IsEnabled)
            throw new Exception("Bluetooth esta desactivado. Por favor activalo.");

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
        {
            var ctx = Android.App.Application.Context;
            var permStatus = ctx.CheckSelfPermission(Android.Manifest.Permission.BluetoothConnect);
            if (permStatus != Android.Content.PM.Permission.Granted)
            {
                throw new Exception(
                    "Permiso BLUETOOTH_CONNECT no otorgado. Reinicia la app y acepta los permisos.");
            }
        }

        try
        {
            var bondedDevices = _bluetoothAdapter.BondedDevices;

            if (bondedDevices != null && bondedDevices.Count > 0)
            {
                foreach (AndroidBluetoothDevice device in bondedDevices)
                {
                    var name = device.Name ?? "Dispositivo desconocido";
                    var addr = device.Address ?? "";
                    devices.Add(new PrinterDevice(addr, name, "bluetooth", IsPaired: true));
                }
            }
        }
        catch (Java.Lang.SecurityException)
        {
            return Task.FromResult<IReadOnlyList<PrinterDevice>>(new List<PrinterDevice>());
        }
#elif IOS
        throw new PlatformNotSupportedException("iOS no soporta Bluetooth Classic SPP. Usar BLE o impresion por red.");
#endif
        return Task.FromResult<IReadOnlyList<PrinterDevice>>(devices);
    }

    public async Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default)
    {
#if ANDROID
        try
        {
            if (_socket != null && _socket.IsConnected)
                await DisconnectAsync();

            var device = _bluetoothAdapter!.GetRemoteDevice(deviceId)
                ?? throw new Exception("No se pudo encontrar el dispositivo");

            var uuid = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB")!;
            _socket = device.CreateRfcommSocketToServiceRecord(uuid)!;

            await Task.Run(() => _socket.Connect(), ct);
            _outputStream = _socket.OutputStream;

            IsConnected = true;
            _lastDeviceAddress = deviceId;
            _currentDevice = new PrinterDevice(deviceId, device.Name ?? deviceId, "bluetooth", IsPaired: true);
            return true;
        }
        catch (Exception ex)
        {
            IsConnected = false;
            throw new Exception($"Error al conectar: {ex.Message}", ex);
        }
#elif IOS
        await Task.CompletedTask;
        throw new PlatformNotSupportedException("iOS no soporta Bluetooth Classic SPP. Usar BLE o impresion por red.");
#else
        await Task.CompletedTask;
        return false;
#endif
    }

    public async Task DisconnectAsync()
    {
#if ANDROID
        try
        {
            if (_outputStream != null)
            {
                await _outputStream.FlushAsync();
                _outputStream.Dispose();
                _outputStream = null;
            }

            if (_socket != null)
            {
                _socket.Close();
                _socket.Dispose();
                _socket = null;
            }

            IsConnected = false;
            _currentDevice = null;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al desconectar: {ex.Message}", ex);
        }
#elif IOS
        await Task.CompletedTask;
        throw new PlatformNotSupportedException("iOS no soporta Bluetooth Classic SPP. Usar BLE o impresion por red.");
#else
        await Task.CompletedTask;
#endif
    }

    public async Task WriteBytesAsync(byte[] data, PrinterProfile profile, CancellationToken ct = default)
    {
#if ANDROID
        if (_outputStream == null)
            throw new InvalidOperationException("No hay una impresora conectada");

        await Task.Delay(profile.InitDelayMs, ct);
        var lines = SplitByLineFeed(data);

        foreach (var line in lines)
        {
            await _outputStream!.WriteAsync(line, 0, line.Length, ct);
            await _outputStream.FlushAsync(ct);
            int delayMs = GetDelayForLine(line, profile);
            await Task.Delay(delayMs, ct);
        }

        await Task.Delay(profile.FinalDelayMs, ct);
#elif IOS
        await Task.CompletedTask;
        throw new PlatformNotSupportedException("iOS no soporta Bluetooth Classic SPP. Usar BLE o impresion por red.");
#else
        await Task.CompletedTask;
#endif
    }

    private static int GetDelayForLine(byte[] line, PrinterProfile profile)
    {
        if (line.Length == 0) return 20;
        if (line.Length >= 2 && line[0] == 0x1D && line[1] == 0x28) return profile.QrDelayMs;
        if (line.Length >= 2 && line[0] == 0x1D && line[1] == 0x76) return profile.ImageDelayMs;
        if (line.Length >= 2 && line[0] == 0x1D && line[1] == 0x56) return profile.CutDelayMs;
        if (line.Length >= 2 && line[0] == 0x1B && line[1] == 0x40) return profile.InitCommandDelayMs;
        return profile.LineDelayMs + (line.Length * profile.ByteDelayMs);
    }

    private static List<byte[]> SplitByLineFeed(byte[] data)
    {
        var lines = new List<byte[]>();
        var current = new List<byte>();

        foreach (byte b in data)
        {
            current.Add(b);
            if (b == 0x0A)
            {
                lines.Add(current.ToArray());
                current.Clear();
            }
        }

        if (current.Count > 0)
            lines.Add(current.ToArray());

        return lines;
    }
}
