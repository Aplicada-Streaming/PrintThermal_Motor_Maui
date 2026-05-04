#if ANDROID
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Java.Util;
using Microsoft.Maui.ApplicationModel;
using AndroidBluetoothDevice = Android.Bluetooth.BluetoothDevice;
#endif
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;

namespace MotorDsl.Nuget.Integrated.MultaApp.Services;

/// <summary>
/// Servicio de impresión térmica para Android (reutilizado de SampleApp).
/// Sprint 08 | TK-66
/// </summary>
public class ThermalPrinterService : IThermalPrinterService
{
#if ANDROID
    private BluetoothSocket? _socket;
    private BluetoothAdapter? _bluetoothAdapter;
    private System.IO.Stream? _outputStream;
#endif

    private readonly IPrintErrorHandler _errorHandler;
    private string? _lastDeviceAddress;

    public bool IsConnected { get; private set; }

    public ThermalPrinterService(IPrintErrorHandler errorHandler)
    {
        _errorHandler = errorHandler;
#if ANDROID
        _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
#endif
    }

    public Task<List<BluetoothDevice>> ScanDevicesAsync()
    {
        var devices = new List<BluetoothDevice>();

#if ANDROID
        if (_bluetoothAdapter == null)
            throw new Exception("Bluetooth no está disponible en este dispositivo");

        if (!_bluetoothAdapter.IsEnabled)
            throw new Exception("Bluetooth está desactivado. Por favor actívalo.");

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
        {
            var activity = Platform.CurrentActivity!;
            var permStatus = AndroidX.Core.Content.ContextCompat.CheckSelfPermission(activity,
                    Android.Manifest.Permission.BluetoothConnect);
            if (permStatus != (int)Android.Content.PM.Permission.Granted)
            {
                throw new Exception(
                    "Permiso BLUETOOTH_CONNECT no otorgado. Reiniciá la app y aceptá los permisos.");
            }
        }

        try
        {
            var bondedDevices = _bluetoothAdapter.BondedDevices;

            if (bondedDevices != null && bondedDevices.Count > 0)
            {
                foreach (AndroidBluetoothDevice device in bondedDevices)
                {
                    devices.Add(new BluetoothDevice
                    {
                        Name = device.Name ?? "Dispositivo desconocido",
                        Address = device.Address ?? "",
                        IsPaired = true
                    });
                }
            }
        }
        catch (Java.Lang.SecurityException)
        {
            return Task.FromResult(new List<BluetoothDevice>());
        }
#endif
        return Task.FromResult(devices);
    }

    public async Task<bool> ConnectAsync(string deviceAddress)
    {
#if ANDROID
        try
        {
            if (_socket != null && _socket.IsConnected)
                await DisconnectAsync();

            var device = _bluetoothAdapter!.GetRemoteDevice(deviceAddress)
                ?? throw new Exception("No se pudo encontrar el dispositivo");

            var uuid = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB")!;
            _socket = device.CreateRfcommSocketToServiceRecord(uuid)!;

            await Task.Run(() => _socket.Connect());
            _outputStream = _socket.OutputStream;

            IsConnected = true;
            _lastDeviceAddress = deviceAddress;
            return true;
        }
        catch (Exception ex)
        {
            IsConnected = false;
            throw new Exception($"Error al conectar: {ex.Message}", ex);
        }
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
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al desconectar: {ex.Message}", ex);
        }
#else
        await Task.CompletedTask;
#endif
    }

    public async Task SendBytesAsync(byte[] data, PrinterProfile? profile = null, PrintRetryOptions? retryOptions = null)
    {
#if IOS
        await ShowIosNotSupportedAsync();
        return;
#elif ANDROID
        profile ??= PrinterProfile.Thermal58mm;
        retryOptions ??= new PrintRetryOptions();

        for (int attempt = 1; attempt <= retryOptions.MaxRetries; attempt++)
        {
            try
            {
                if (_outputStream == null)
                    throw new InvalidOperationException("No hay una impresora conectada");

                await SendBytesInternalAsync(data, profile);
                _errorHandler.OnPrintSuccess(attempt);
                return;
            }
            catch (Exception ex)
            {
                var error = PrintError.FromException(ex, attempt, retryOptions.MaxRetries);
                var shouldRetry = await _errorHandler.HandleErrorAsync(error);

                if (!shouldRetry || attempt >= retryOptions.MaxRetries)
                    throw new Exception($"Print failed after {attempt} attempt(s): {error.Message}", ex);

                _errorHandler.OnRetryAttempt(error);

                int delayMs = retryOptions.InitialDelayMs * (1 << (attempt - 1));
                await Task.Delay(delayMs);

                await TryReconnectAsync();
            }
        }
#else
        await Task.CompletedTask;
#endif
    }

#if ANDROID
    private async Task SendBytesInternalAsync(byte[] data, PrinterProfile profile)
    {
        await Task.Delay(profile.InitDelayMs);
        var lines = SplitByLineFeed(data);

        foreach (var line in lines)
        {
            await _outputStream!.WriteAsync(line, 0, line.Length);
            await _outputStream.FlushAsync();
            int delayMs = GetDelayForLine(line, profile);
            await Task.Delay(delayMs);
        }

        await Task.Delay(profile.FinalDelayMs);
    }

    private async Task TryReconnectAsync()
    {
        if (IsConnected && _socket?.IsConnected == true) return;
        if (string.IsNullOrEmpty(_lastDeviceAddress)) return;

        try { await ConnectAsync(_lastDeviceAddress); }
        catch { /* Reconnection failed — next retry will handle it */ }
    }
#endif

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

#if IOS
    /// <summary>
    /// iOS: Bluetooth clásico SPP no soportado. Muestra alerta al usuario.
    /// </summary>
    private static Task ShowIosNotSupportedAsync()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (Application.Current?.MainPage is Page page)
                await page.DisplayAlert(
                    "No disponible en iOS",
                    "La impresión por Bluetooth clásico (SPP) no está disponible en iOS. " +
                    "Use la función \u2018Ver PDF\u2019 para generar el documento.",
                    "OK");
        });
        return Task.CompletedTask;
    }
#endif
}
