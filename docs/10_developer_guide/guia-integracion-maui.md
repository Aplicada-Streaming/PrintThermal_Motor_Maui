# Guía de Integración en .NET MAUI

Guía paso a paso para integrar el Motor DSL en una aplicación .NET MAUI con
impresión Bluetooth térmica. Cubre los 3 paquetes nuevos
(`MotorDsl.Printing.Abstractions`, `MotorDsl.Bluetooth`, `MotorDsl.Maui`) y los
4 paquetes core (`MotorDsl.Core`, `MotorDsl.Parser`, `MotorDsl.Rendering`,
`MotorDsl.Extensions`).

---

## 1. Introducción

`MotorDsl.Maui` proporciona controles bindeables, renderers de PDF / ESC/POS
bitmap / raster preview y un error handler con eventos para feedback de UI,
todo construido sobre **.NET 10 + .NET MAUI**. El stack opera contra el
contrato `IThermalPrinterService` definido en `MotorDsl.Printing.Abstractions`
y delega el transporte físico (BT, USB, Red, BLE) a uno o más
`IThermalPrinterTransport` registrados.

---

## 2. Instalación de paquetes NuGet

En el `.csproj` de tu app MAUI bastan **dos** PackageReferences explícitos —
los demás llegan como dependencias transitivas de `MotorDsl.Maui`:

```xml
<ItemGroup>
  <PackageReference Include="MotorDsl.Maui" Version="<latest>" />
  <PackageReference Include="MotorDsl.Bluetooth" Version="<latest>" />
</ItemGroup>
```

Los 7 paquetes resultantes (referencia completa):

```xml
<ItemGroup>
  <PackageReference Include="MotorDsl.Core"                  Version="<latest>" />
  <PackageReference Include="MotorDsl.Parser"                Version="<latest>" />
  <PackageReference Include="MotorDsl.Rendering"             Version="<latest>" />
  <PackageReference Include="MotorDsl.Extensions"            Version="<latest>" />
  <PackageReference Include="MotorDsl.Printing.Abstractions" Version="<latest>" />
  <PackageReference Include="MotorDsl.Bluetooth"             Version="<latest>" />
  <PackageReference Include="MotorDsl.Maui"                  Version="<latest>" />
</ItemGroup>
```

> Hasta que `MotorDsl.Maui` y `MotorDsl.Bluetooth` se publiquen en nuget.org,
> los samples del repositorio los consumen vía `<ProjectReference>` (Fase 1 —
> ver `samples/MotorDsl.Nuget.Integrated.MultaApp/MotorDsl.Nuget.Integrated.MultaApp.csproj`).

---

## 3. Configuración en `MauiProgram.cs`

```csharp
using Microsoft.Extensions.Logging;
using MotorDsl.Bluetooth;
using MotorDsl.Core.Models;
using MotorDsl.Extensions;
using MotorDsl.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Motor DSL: pipeline + templates + profiles + renderers MAUI
        builder.Services.AddMotorDslEngine()
            .AddTemplates(t =>
            {
                t.Add("ticket-multa", MultaIntegratedDsl.Document);
            })
            .AddProfiles(p =>
            {
                p.Add(new DeviceProfile("thermal_58mm", 32, "escpos-bitmap"));
                p.Add(new DeviceProfile("preview", 32, "raster-preview"));
                p.Add(new DeviceProfile("a4-pdf", 80, "pdf"));
            })
            .AddMotorDslMaui();

        // Transport Bluetooth (Android Classic SPP)
        builder.Services.AddBluetoothPrinterTransport();

        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
```

### Qué registra `AddMotorDslMaui()`

| Servicio | Implementación | Lifetime | Rol |
|---|---|---|---|
| `IBitmapRasterizer` | `SkiaSharpRasterizer` | Singleton | Convierte imágenes a 1-bit. |
| `QrCodeRasterizer` | (concreto) | Singleton | QR PNG con QRCoder. |
| `IPrintErrorHandler` | `MauiPrintErrorHandler` | Singleton | Reemplaza al default; emite eventos. |
| `IThermalPrinterService` | `ThermalPrinterService` | Singleton | Orquestador de impresión. |
| `IRenderer` (PDF) | `PdfRenderer` | Singleton | Target `pdf`. |
| `IRenderer` (ESC/POS bitmap) | `BitmapEscPosRenderer` | Singleton | Target `escpos-bitmap`. |
| `IRenderer` (raster preview) | `RasterPreviewRenderer` | Singleton | Target `raster-preview`. |

### Qué registra `AddBluetoothPrinterTransport()`

| Servicio | Implementación | Lifetime |
|---|---|---|
| `IThermalPrinterTransport` (Kind=`bluetooth`) | `BluetoothPrinterTransport` | Singleton |

---

## 4. Permisos Android

`Platforms/Android/AndroidManifest.xml`:

```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
  <!-- Bluetooth (legacy API < 31) -->
  <uses-permission android:name="android.permission.BLUETOOTH" />
  <uses-permission android:name="android.permission.BLUETOOTH_ADMIN" />

  <!-- Bluetooth (API 31+ / Android 12+) -->
  <uses-permission android:name="android.permission.BLUETOOTH_SCAN"
                   android:usesPermissionFlags="neverForLocation" />
  <uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />

  <!-- Ubicación (necesario en Android < 12 para escaneo) -->
  <uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />

  <application android:allowBackup="true" android:supportsRtl="true">
  </application>
</manifest>
```

En **runtime** (Android 12+) se deben solicitar `BLUETOOTH_SCAN` y
`BLUETOOTH_CONNECT` antes de invocar `DiscoverDevicesAsync`:

```csharp
var activity = Platform.CurrentActivity!;
string[] btPermissions =
{
    Manifest.Permission.BluetoothScan,
    Manifest.Permission.BluetoothConnect
};
ActivityCompat.RequestPermissions(activity, btPermissions, requestCode: 1);
```

---

## 5. Uso del servicio `IThermalPrinterService`

### 5.1 Inyección

```csharp
public partial class MainPage : ContentPage
{
    private readonly IDocumentEngine _engine;
    private readonly IThermalPrinterService _printer;

    public MainPage(IDocumentEngine engine, IThermalPrinterService printer)
    {
        InitializeComponent();
        _engine = engine;
        _printer = printer;
    }
}
```

### 5.2 Descubrir y conectar

```csharp
IReadOnlyList<PrinterDevice> devices =
    await _printer.DiscoverDevicesAsync(kind: "bluetooth");

if (devices.Count > 0)
{
    bool ok = await _printer.ConnectAsync(devices[0]);
    if (ok) Console.WriteLine($"Conectado: {_printer.CurrentDevice?.Name}");
}
```

### 5.3 Renderizar y enviar

```csharp
var profile = new DeviceProfile("thermal_58mm", 32, "escpos-bitmap");
profile.SetCapability("supports_bitmap", true);
profile.SetCapability("bitmap_max_width_px", 320);

RenderResult result = _engine.Render(jsonDsl, profile);
if (result.IsSuccessful && _printer.IsConnected)
{
    byte[] bytes = (byte[])result.Output!;
    await _printer.SendBytesAsync(bytes);
}
```

### 5.4 Reconectar

```csharp
if (!_printer.IsConnected && _printer.CurrentDevice is not null)
    await _printer.ReconnectAsync();
```

### 5.5 Eventos

```csharp
_printer.DevicesDiscovered += (_, args) =>
    DevicesLabel.Text = $"Descubiertos: {args.Devices.Count}";

_printer.ErrorOccurred += (_, args) =>
    ErrorLabel.Text = $"{args.Error.Type}: {args.Error.Message}";

_printer.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(IThermalPrinterService.ConnectionState))
        Dispatcher.Dispatch(() => StateLabel.Text = _printer.ConnectionState.ToString());
};
```

### 5.6 Retry exponencial

```csharp
await _printer.SendBytesAsync(
    bytes,
    profile: PrinterProfile.Real58HB6,
    retry:   new PrintRetryOptions { MaxRetries = 5, InitialDelayMs = 200 });
```

El servicio aplica retry exponencial (`InitialDelayMs * 2^(attempt-1)`) e
intenta reconectar el transport entre intentos. La decisión final de retry vs
abort la toma el `IPrintErrorHandler` registrado.

---

## 6. Componentes MAUI

Todos los controles viven en
`xmlns:muic="clr-namespace:MotorDsl.Maui.Controls;assembly=MotorDsl.Maui"`.

### 6.1 `PrinterStatusBadge`

Refleja el estado del servicio con color y texto.

```xml
<muic:PrinterStatusBadge x:Name="StatusBadge" />
```

```csharp
StatusBadge.Service = _printer;   // basta una vez
```

### 6.2 `PrinterPickerView`

Botón Escanear + lista de dispositivos. Conecta automáticamente al tocar uno.

```xml
<muic:PrinterPickerView x:Name="DevicePicker"
                        FilterKind="bluetooth"
                        AutoConnectIfSingle="True" />
```

```csharp
DevicePicker.Service = _printer;
DevicePicker.DeviceSelected += (_, dev) => Status.Text = $"Conectado a {dev.Name}";
DevicePicker.ScanError    += (_, ex)  => Status.Text = $"BT Error: {ex.Message}";
await DevicePicker.ScanAsync();   // disparar escaneo manual
```

### 6.3 `MauiRasterPreview`

Muestra el PNG producido por `RasterPreviewRenderer` con zoom configurable.

```xml
<muic:MauiRasterPreview x:Name="RasterPreview" ZoomFactor="2" />
```

```csharp
var profile = new DeviceProfile("preview", 32, "raster-preview");
var result = _engine.Render(jsonDsl, profile);
RasterPreview.ImageBytes = (byte[])result.Output!;
```

### 6.4 `MauiDocumentPreview`

Vista previa tipográfica del `LayoutedDocument` (alineación, bold, marcadores
para QR / barcode / bitmap).

```xml
<muic:MauiDocumentPreview x:Name="DocPreview" />
```

```csharp
DocPreview.Document = layoutedDocument;
```

### 6.5 `PrinterPickerPage` (modal)

`ContentPage` que envuelve `PrinterPickerView` con un Cancelar.

```csharp
var page = new PrinterPickerPage(_printer, filterKind: "bluetooth");
await Navigation.PushModalAsync(page);
```

---

## 7. Manejo de errores

### 7.1 `MauiPrintErrorHandler`

Lo registra automáticamente `AddMotorDslMaui()`. Para suscribirse a sus
eventos, resolvelo del DI:

```csharp
var handler = sp.GetRequiredService<IPrintErrorHandler>() as MauiPrintErrorHandler;
if (handler is not null)
{
    handler.RetryAttempted += (_, error) =>
        Toast.Make($"Reintentando ({error.Attempt}/{error.MaxAttempts})").Show();

    handler.Succeeded += (_, attempts) =>
        Toast.Make($"Impreso en {attempts} intento(s)").Show();
}
```

### 7.2 Eventos del servicio

```csharp
_printer.ErrorOccurred += async (_, args) =>
{
    var err = args.Error;
    await DisplayAlert("Error de impresión",
        $"{err.Type}\nIntento {err.Attempt}/{err.MaxAttempts}\n{err.Message}",
        "OK");
};
```

### 7.3 Errores de render

```csharp
RenderResult result = _engine.Render(json, profile);
if (!result.IsSuccessful)
{
    foreach (var err in result.Errors)
        await DisplayAlert("Error", err, "OK");
}
foreach (var w in result.Warnings)
    System.Diagnostics.Debug.WriteLine($"WARN: {w}");
```

### 7.4 Persistencia para reenvío

```csharp
try
{
    await _printer.SendBytesAsync(bytes);
}
catch
{
    Preferences.Set("pending_print_b64", Convert.ToBase64String(bytes));
}
```

---

## 8. Plataformas soportadas

| Componente | Android | iOS | Windows |
|---|---|---|---|
| `MotorDsl.Core` / `Parser` / `Rendering` / `Extensions` | ✅ | ✅ | ✅ |
| `MotorDsl.Printing.Abstractions` (orquestador) | ✅ | ✅ | ✅ |
| `MotorDsl.Bluetooth` (BT Classic SPP) | ✅ | ❌ `PlatformNotSupportedException` | ❌ |
| `MotorDsl.Maui` controles + renderers | ✅ | ✅ (sin BT) | ❌ MAUI desktop fuera de alcance |
| `PdfRenderer` (target `pdf`) | ✅ | ✅ | ✅ |
| `RasterPreviewRenderer` (target `raster-preview`) | ✅ | ✅ | ✅ |

iOS funciona para render + PDF + raster + UI MAUI. Para imprimir desde iOS
hay que registrar otro `IThermalPrinterTransport` (BLE / WiFi / AirPrint vía
PDF). Ver
[`compatibilidad-plataformas_v1.1.md`](../00_contexto/compatibilidad-plataformas_v1.1.md)
sección 6.

---

## 8.1 Diagnóstico y reporte de fallos

`MotorDsl.Maui` expone `IDiagnosticsReportProvider` (registrado automáticamente
en `AddMotorDslMaui()`) para capturar un snapshot de versiones de librería,
info de app y dispositivo, impresora vinculada y permisos. Pensado para flujos
de soporte: ver en pantalla, imprimir como ticket térmico o compartir vía
Share API. Patrón típico (3 botones):

```csharp
public MainPage(IDocumentEngine engine,
                IThermalPrinterService printer,
                IDiagnosticsReportProvider diagnostics)
{
    InitializeComponent();
    _engine = engine;
    _printer = printer;
    _diagnostics = diagnostics;
}

private void OnDiagnosticoVerClicked(object? s, EventArgs e)
{
    var report  = _diagnostics.Build(notes: "captura manual");
    var dsl     = _diagnostics.ToDslJson(report, paperWidthChars: 32);
    var profile = new DeviceProfile("preview", 32, "raster-preview");
    profile.SetCapability("bitmap_max_width_px", 384);
    var result  = _engine.Render(dsl, profile);
    if (result.IsSuccessful && result.Output is byte[] bytes)
        RasterPreview.ImageBytes = bytes;
}

private async void OnDiagnosticoImprimirClicked(object? s, EventArgs e)
{
    var report  = _diagnostics.Build(notes: "imprimir diag");
    var dsl     = _diagnostics.ToDslJson(report);
    var profile = new DeviceProfile("58HB6", 32, "escpos-bitmap");
    profile.SetCapability("supports_bitmap", true);
    profile.SetCapability("bitmap_max_width_px", 320);
    var result  = _engine.Render(dsl, profile);
    if (result.IsSuccessful && _printer.IsConnected)
        await _printer.SendBytesAsync((byte[])result.Output!);
}

private async void OnDiagnosticoReportarClicked(object? s, EventArgs e)
{
    var report = _diagnostics.Build(notes: "Reporte de fallo");
    var text   = _diagnostics.ToPlainText(report);
    await Share.Default.RequestAsync(new ShareTextRequest
    {
        Title = "Reporte de fallo MotorDsl",
        Text  = text
    });
}
```

Detalles de privacidad (`includePii`, `MaskMac`), customización, decorator y
limitaciones conocidas en [`diagnostics.md`](diagnostics.md).

---

## 9. Referencias cruzadas

- [Arquitectura de la Solución (v1.1)](../05_arquitectura_tecnica/arquitectura-solucion_v1.1.md)
- [Extensibilidad del Motor (v1.1)](../05_arquitectura_tecnica/extensibilidad-motor_v1.1.md)
- [Guía de Uso de la Librería (v1.1)](../05_arquitectura_tecnica/guia-uso-libreria_v1.1.md)
- [Compatibilidad de Plataformas (v1.1)](../00_contexto/compatibilidad-plataformas_v1.1.md)
- [Componentes UX MAUI](componentes-ux-maui.md)
- [Render Pixelado y PDF](render-pixelado-y-pdf.md)
- [Transports y Extensibilidad](transports-y-extensibilidad.md)
- [Diagnóstico y reporte de fallos](diagnostics.md)
- [Ejemplo: MultaApp NuGet](../11_examples/ejemplo-03-multaapp-nuget.md)
- [Ejemplo: Multa Integrada](../11_examples/ejemplo-03-multa-integrada.md)
