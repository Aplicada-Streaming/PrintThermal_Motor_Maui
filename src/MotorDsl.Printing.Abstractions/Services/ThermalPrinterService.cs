using System.ComponentModel;
using System.Runtime.CompilerServices;
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;

namespace MotorDsl.Printing;

/// <summary>
/// Orquestador de impresion termica agnostico de plataforma.
/// Delega el transporte fisico a uno o mas IThermalPrinterTransport registrados.
/// Maneja el ciclo de vida de la conexion, retry exponencial y notificacion via INotifyPropertyChanged.
/// </summary>
public class ThermalPrinterService : IThermalPrinterService
{
    private readonly IThermalPrinterTransport[] _transports;
    private readonly IPrintErrorHandler _errorHandler;

    private IThermalPrinterTransport? _activeTransport;
    private bool _isConnected;
    private PrinterDevice? _currentDevice;
    private PrinterConnectionState _connectionState = PrinterConnectionState.Disconnected;
    private string? _lastError;

    public bool IsConnected
    {
        get => _isConnected;
        private set => SetProperty(ref _isConnected, value);
    }

    public PrinterDevice? CurrentDevice
    {
        get => _currentDevice;
        private set => SetProperty(ref _currentDevice, value);
    }

    public PrinterConnectionState ConnectionState
    {
        get => _connectionState;
        private set => SetProperty(ref _connectionState, value);
    }

    public string? LastError
    {
        get => _lastError;
        private set => SetProperty(ref _lastError, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<PrintErrorEventArgs>? ErrorOccurred;
    public event EventHandler<DevicesDiscoveredEventArgs>? DevicesDiscovered;

    public ThermalPrinterService(IEnumerable<IThermalPrinterTransport> transports, IPrintErrorHandler errorHandler)
    {
        _transports = transports?.ToArray() ?? Array.Empty<IThermalPrinterTransport>();
        if (_transports.Length == 0)
            throw new InvalidOperationException(
                "No IThermalPrinterTransport registered. Register at least one (e.g., AddBluetoothPrinterTransport).");

        _errorHandler = errorHandler;
    }

    public async Task<IReadOnlyList<PrinterDevice>> DiscoverDevicesAsync(string? kind = null, CancellationToken ct = default)
    {
        var wasConnected = IsConnected;
        ConnectionState = PrinterConnectionState.Scanning;

        var combined = new List<PrinterDevice>();
        try
        {
            foreach (var transport in _transports)
            {
                if (kind != null && !string.Equals(transport.Kind, kind, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var devices = await transport.DiscoverAsync(ct);
                    if (devices != null)
                        combined.AddRange(devices);
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    var error = PrintError.FromException(ex, 1, 1);
                    ErrorOccurred?.Invoke(this, new PrintErrorEventArgs(error));
                }
            }

            DevicesDiscovered?.Invoke(this, new DevicesDiscoveredEventArgs(combined));
        }
        finally
        {
            if (!wasConnected)
                ConnectionState = PrinterConnectionState.Disconnected;
            else
                ConnectionState = PrinterConnectionState.Connected;
        }

        return combined;
    }

    public async Task<bool> ConnectAsync(PrinterDevice device, CancellationToken ct = default)
    {
        var transport = _transports.FirstOrDefault(t =>
            string.Equals(t.Kind, device.Kind, StringComparison.OrdinalIgnoreCase));

        if (transport == null)
        {
            LastError = $"No transport registered for kind '{device.Kind}'.";
            ConnectionState = PrinterConnectionState.Failed;
            var err = new PrintError(PrintErrorType.Protocol, LastError, null, 1, 1);
            ErrorOccurred?.Invoke(this, new PrintErrorEventArgs(err));
            return false;
        }

        ConnectionState = PrinterConnectionState.Connecting;
        try
        {
            var ok = await transport.ConnectAsync(device.Id, ct);
            if (ok)
            {
                _activeTransport = transport;
                IsConnected = true;
                CurrentDevice = device;
                ConnectionState = PrinterConnectionState.Connected;
                LastError = null;
                return true;
            }

            ConnectionState = PrinterConnectionState.Failed;
            return false;
        }
        catch (Exception ex)
        {
            ConnectionState = PrinterConnectionState.Failed;
            LastError = ex.Message;
            var error = PrintError.FromException(ex, 1, 1);
            ErrorOccurred?.Invoke(this, new PrintErrorEventArgs(error));
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_activeTransport != null)
        {
            try { await _activeTransport.DisconnectAsync(); }
            catch (Exception ex)
            {
                LastError = ex.Message;
                var error = PrintError.FromException(ex, 1, 1);
                ErrorOccurred?.Invoke(this, new PrintErrorEventArgs(error));
            }
        }

        _activeTransport = null;
        IsConnected = false;
        CurrentDevice = null;
        ConnectionState = PrinterConnectionState.Disconnected;
    }

    public async Task<bool> ReconnectAsync(CancellationToken ct = default)
    {
        if (CurrentDevice == null) return false;

        ConnectionState = PrinterConnectionState.Reconnecting;
        return await ConnectAsync(CurrentDevice, ct);
    }

    public async Task SendBytesAsync(byte[] data, PrinterProfile? profile = null, PrintRetryOptions? retry = null, CancellationToken ct = default)
    {
        profile ??= PrinterProfile.Thermal58mm;
        retry ??= new PrintRetryOptions();

        if (_activeTransport == null)
            throw new InvalidOperationException("No hay un transport activo. Llamar ConnectAsync primero.");

        for (int attempt = 1; attempt <= retry.MaxRetries; attempt++)
        {
            try
            {
                await _activeTransport.WriteBytesAsync(data, profile, ct);
                _errorHandler.OnPrintSuccess(attempt);
                return;
            }
            catch (Exception ex)
            {
                var error = PrintError.FromException(ex, attempt, retry.MaxRetries);
                ErrorOccurred?.Invoke(this, new PrintErrorEventArgs(error));
                var shouldRetry = await _errorHandler.HandleErrorAsync(error);
                if (!shouldRetry || attempt >= retry.MaxRetries)
                    throw new Exception($"Print failed after {attempt} attempt(s): {error.Message}", ex);
                _errorHandler.OnRetryAttempt(error);
                int delayMs = retry.InitialDelayMs * (1 << (attempt - 1));
                await Task.Delay(delayMs, ct);
                // intento de reconexion (silencioso)
                try { if (CurrentDevice != null) await ReconnectInternalAsync(ct); } catch { }
            }
        }
    }

    private async Task ReconnectInternalAsync(CancellationToken ct)
    {
        if (_activeTransport == null || CurrentDevice == null) return;
        if (_activeTransport.IsConnected) return;
        try { await _activeTransport.ConnectAsync(CurrentDevice.Id, ct); }
        catch { /* silencio: el siguiente intento se encargara */ }
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
