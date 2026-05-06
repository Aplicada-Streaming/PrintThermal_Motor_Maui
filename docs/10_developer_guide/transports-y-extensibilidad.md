# Transports y Extensibilidad

Guía técnica para implementar un `IThermalPrinterTransport` propio (USB, WiFi,
BLE, MFi, etc.). Toma como referencia la implementación oficial
`BluetoothPrinterTransport` del paquete `MotorDsl.Bluetooth`.

---

## 1. Arquitectura del transport

El paquete `MotorDsl.Printing.Abstractions` define dos contratos que separan
responsabilidades:

```text
                    ┌──────────────────────────────────────┐
                    │  IThermalPrinterService              │  ← orquestador
                    │   (ThermalPrinterService)            │     INotifyPropertyChanged
                    │   - DiscoverDevicesAsync(kind)       │     retry exponencial
                    │   - ConnectAsync(device)             │     enrutamiento por Kind
                    │   - SendBytesAsync(...)              │
                    └──────────┬───────────────────────────┘
                               │ delega por device.Kind
        ┌──────────────────────┼──────────────────────┐
        ▼                      ▼                      ▼
  ┌─────────────┐        ┌─────────────┐       ┌──────────────┐
  │ BT (kind=   │        │ WiFi (kind= │       │ USB (kind=   │
  │ "bluetooth")│        │  "wifi")    │       │  "usb")      │
  │ — paquete   │        │  — custom   │       │  — custom    │
  │  Bluetooth  │        │             │       │              │
  └─────────────┘        └─────────────┘       └──────────────┘
        │                      │                      │
        ▼                      ▼                      ▼
  Impresora BT          Impresora red         Impresora USB
```

### Reparto de responsabilidades

| Responsabilidad | Vive en |
|---|---|
| Estado lógico (`ConnectionState`, `CurrentDevice`) | Servicio (orquestador). |
| Notificación a UI (`INotifyPropertyChanged`) | Servicio. |
| Retry exponencial + reconexión silenciosa | Servicio + `IPrintErrorHandler`. |
| Enrutamiento por `Kind` | Servicio. |
| **Acceso al hardware / protocolo físico** | **Transport**. |

> Un transport **no debería** implementar retry ni dialogs — es un I/O
> sincrónico-asíncrono dumb. Cualquier excepción que tire el `WriteBytesAsync`
> dispara el retry-loop del servicio.

---

## 2. Contrato `IThermalPrinterTransport`

```csharp
namespace MotorDsl.Printing;

public interface IThermalPrinterTransport
{
    string Kind { get; }                         // "bluetooth", "wifi", "usb", "ble", ...
    bool IsConnected { get; }
    PrinterDevice? CurrentDevice { get; }

    Task<IReadOnlyList<PrinterDevice>> DiscoverAsync(CancellationToken ct = default);
    Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default);
    Task DisconnectAsync();
    Task WriteBytesAsync(byte[] data, PrinterProfile profile, CancellationToken ct = default);
}
```

### Reglas del contrato

- `Kind` debe ser **único** entre todos los transports registrados. El servicio
  hace `FirstOrDefault` por `Kind`, así que duplicados quedan ignorados.
- `DiscoverAsync` debe devolver `PrinterDevice` con `Kind == this.Kind`.
- `ConnectAsync(deviceId, ct)` recibe el `PrinterDevice.Id` que el orquestador
  extrae del device elegido.
- `WriteBytesAsync` recibe `PrinterProfile` con `LineDelayMs`, `ByteDelayMs`,
  `InitDelayMs`, `FinalDelayMs`, `QrDelayMs`, `ImageDelayMs`, `CutDelayMs`,
  `InitCommandDelayMs` — el transport puede usarlos para ajustar timing al
  hardware.
- Cualquier excepción dispara el retry del servicio. Si el error **no es
  retryable**, lanzar `OperationCanceledException` (será respetado) o tipos
  específicos que el `IPrintErrorHandler` reconozca.

---

## 3. Ejemplo paso a paso — `WiFiPrinterTransport`

Implementación de un transport TCP socket para impresoras térmicas con
conector de red (Epson TM-T20III, Star TSP100, etc.). Las impresoras de red
generalmente exponen el puerto **9100** (raw socket).

### 3.1 Esqueleto

```csharp
using System.Net;
using System.Net.Sockets;
using MotorDsl.Core.Models;
using MotorDsl.Printing;

namespace YourApp.Transports;

public class WiFiPrinterTransport : IThermalPrinterTransport
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private PrinterDevice? _currentDevice;

    public string Kind => "wifi";
    public bool IsConnected => _client?.Connected == true;
    public PrinterDevice? CurrentDevice => _currentDevice;

    // ... métodos a continuación
}
```

### 3.2 `DiscoverAsync` — escanear subred

Estrategia simple: barrer `192.168.x.0/24` haciendo `TcpClient.ConnectAsync` con
timeout corto al puerto 9100. Las que respondan se reportan como dispositivos.

```csharp
public async Task<IReadOnlyList<PrinterDevice>> DiscoverAsync(CancellationToken ct = default)
{
    var found = new List<PrinterDevice>();

    var localIp = Dns.GetHostEntry(Dns.GetHostName())
        .AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
    if (localIp is null) return found;

    var prefix = string.Join(".", localIp.ToString().Split('.').Take(3));
    var tasks = Enumerable.Range(1, 254).Select(async host =>
    {
        var ip = $"{prefix}.{host}";
        try
        {
            using var probe = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMilliseconds(150));
            await probe.ConnectAsync(IPAddress.Parse(ip), 9100, cts.Token);
            if (probe.Connected)
                lock (found) found.Add(new PrinterDevice(ip, $"WiFi Printer {ip}", "wifi"));
        }
        catch { /* silenciar — host no responde */ }
    });

    await Task.WhenAll(tasks);
    return found;
}
```

> En producción es habitual confiar en mDNS/Bonjour (`_pdl-datastream._tcp`) en
> lugar de barrer la subred. La idea es la misma.

### 3.3 `ConnectAsync` — abrir socket TCP

```csharp
public async Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default)
{
    try
    {
        if (_client != null) await DisconnectAsync();

        _client = new TcpClient();
        await _client.ConnectAsync(IPAddress.Parse(deviceId), 9100, ct);
        _stream = _client.GetStream();
        _currentDevice = new PrinterDevice(deviceId, $"WiFi Printer {deviceId}", "wifi");
        return true;
    }
    catch
    {
        await DisconnectAsync();
        throw;
    }
}
```

### 3.4 `WriteBytesAsync` — escribir bytes

```csharp
public async Task WriteBytesAsync(byte[] data, PrinterProfile profile, CancellationToken ct = default)
{
    if (_stream is null)
        throw new InvalidOperationException("WiFi printer no conectada.");

    await Task.Delay(profile.InitDelayMs, ct);
    await _stream.WriteAsync(data, 0, data.Length, ct);
    await _stream.FlushAsync(ct);
    await Task.Delay(profile.FinalDelayMs, ct);
}
```

> Si la impresora pierde paquetes con flush a velocidad máxima, replicar la
> técnica del `BluetoothPrinterTransport`: dividir por LF (0x0A) y aplicar
> `LineDelayMs + (line.Length * ByteDelayMs)` por línea.

### 3.5 `DisconnectAsync`

```csharp
public Task DisconnectAsync()
{
    try
    {
        _stream?.Dispose();
        _client?.Close();
        _client?.Dispose();
    }
    finally
    {
        _stream = null;
        _client = null;
        _currentDevice = null;
    }
    return Task.CompletedTask;
}
```

---

## 4. Registro en DI

### 4.1 Registro como `IThermalPrinterTransport`

```csharp
using Microsoft.Extensions.DependencyInjection;
using YourApp.Transports;

builder.Services.AddSingleton<IThermalPrinterTransport, WiFiPrinterTransport>();
```

### 4.2 Combinar con otros transports

El servicio acepta múltiples implementaciones — la inyección por
`IEnumerable<IThermalPrinterTransport>` los recolecta todos.

```csharp
builder.Services.AddBluetoothPrinterTransport();                              // Kind="bluetooth"
builder.Services.AddSingleton<IThermalPrinterTransport, WiFiPrinterTransport>();   // Kind="wifi"
```

Con esto, una sola llamada a `printer.DiscoverDevicesAsync()` devuelve dispositivos de **ambos**. El servicio elige el transport correcto al conectarse según `device.Kind`.

### 4.3 Compilación condicional por plataforma

```csharp
#if ANDROID
    builder.Services.AddBluetoothPrinterTransport();
    builder.Services.AddSingleton<IThermalPrinterTransport, WiFiPrinterTransport>();
#elif IOS
    builder.Services.AddSingleton<IThermalPrinterTransport, WiFiPrinterTransport>();
    builder.Services.AddSingleton<IThermalPrinterTransport, IOSBlePrinterTransport>();
#endif
```

### 4.4 Patrón fluent (recomendado para reuso)

Si planeás distribuir el transport como librería, expone una extensión:

```csharp
public static class WiFiPrinterServiceCollectionExtensions
{
    public static IServiceCollection AddWiFiPrinterTransport(this IServiceCollection services)
    {
        services.AddSingleton<IThermalPrinterTransport, WiFiPrinterTransport>();
        return services;
    }
}
```

Y en la app:

```csharp
builder.Services.AddBluetoothPrinterTransport()
                .AddWiFiPrinterTransport();
```

---

## 5. Buenas prácticas

- **Mantener el transport tonto**: solo I/O. Retry, reconexión y notificación
  viven en el orquestador.
- **No mezclar `Kind`s**: un solo `Kind` por implementación. Si la misma clase
  soporta dos canales (BLE + Classic), exponer dos transports distintos.
- **Respetar `CancellationToken`**: propagarlo a todos los `Task.Delay` y a los
  `WriteAsync`/`ReadAsync`.
- **Catchear excepciones específicas y relanzarlas**: el orquestador necesita
  ver la excepción para decidir si retry. No silenciar.
- **Probar con tests de integración**: simular pérdida de conexión durante un
  `WriteBytesAsync` para validar que el retry-loop reconecta.

---

## 6. Lista de chequeo para un transport nuevo

- [ ] `Kind` único y consistente (`"wifi"`, `"usb"`, `"ble"`, ...).
- [ ] `DiscoverAsync` devuelve `PrinterDevice` con `Kind == this.Kind`.
- [ ] `ConnectAsync` cierra cualquier conexión previa antes de abrir una nueva.
- [ ] `WriteBytesAsync` respeta los delays del `PrinterProfile`.
- [ ] `DisconnectAsync` no falla si no hay conexión activa.
- [ ] `IsConnected` siempre refleja la realidad del recurso subyacente.
- [ ] Excepciones bien tipadas (no atrapar `Exception` y devolver `false`).
- [ ] Test unitario con mock del recurso físico.

---

## 7. Referencias

- [`BluetoothPrinterTransport.cs`](https://github.com/Aplicada-Streaming/PrintThermal_Motor_Maui/blob/main/src/MotorDsl.Bluetooth/BluetoothPrinterTransport.cs) — implementación de referencia.
- [Arquitectura de la Solución (v1.1)](../05_arquitectura_tecnica/arquitectura-solucion_v1.1.md) — sección 5.9.
- [Extensibilidad del Motor (v1.1)](../05_arquitectura_tecnica/extensibilidad-motor_v1.1.md) — sección 7.
- [Compatibilidad de Plataformas (v1.1)](../00_contexto/compatibilidad-plataformas_v1.1.md) — sección 5 (alternativas iOS).
