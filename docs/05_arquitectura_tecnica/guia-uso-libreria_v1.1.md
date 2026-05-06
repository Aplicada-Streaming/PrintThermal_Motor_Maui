# Guía de Uso de la Librería — Motor DSL de Renderizado
**Archivo:** guia-uso-libreria_v1.1.md
**Versión:** 1.1
**Fecha:** 2026-05-06
**Autor:** Equipo Técnico
**Estado:** Vigente

---

## Cambios desde v1.0

- Quickstart ahora cubre los **3 paquetes nuevos** (`MotorDsl.Printing.Abstractions`,
  `MotorDsl.Bluetooth`, `MotorDsl.Maui`).
- Se reemplazaron los ejemplos del antiguo `IThermalPrinterService` (que vivía en
  cada sample) por la **API nueva** del paquete `MotorDsl.Printing.Abstractions`
  (`DiscoverDevicesAsync`, `ConnectAsync(PrinterDevice)`, `ReconnectAsync`,
  `SendBytesAsync(byte[], PrinterProfile?, PrintRetryOptions?)`,
  eventos `ErrorOccurred` / `DevicesDiscovered`).
- Nuevo target de profile `escpos-bitmap` (renderer MAUI) y `raster-preview`
  (renderer MAUI para vista pixelada).
- Configuración fluent unificada: `AddMotorDslEngine().AddMotorDslMaui()`.

---

## 1. Propósito

Este documento describe cómo utilizar la librería del Motor DSL desde una
aplicación cliente .NET (MAUI, APIs, servicios backend, herramientas batch).

Incluye:

- Instalación y configuración
- Inicialización del motor
- Ejecución de renderizado
- **Conexión e impresión vía `IThermalPrinterService`**
- Uso de perfiles de dispositivo
- Ejemplos prácticos
- Buenas prácticas de integración

---

## 2. Requisitos

- .NET 10 SDK
- Workload `maui` (sólo si se usa `MotorDsl.Maui`)
- Contenedor de inyección de dependencias estándar de .NET

---

## 3. Instalación

Para una app .NET MAUI con impresión Bluetooth, basta con declarar dos paquetes:

```xml
<ItemGroup>
  <PackageReference Include="MotorDsl.Maui" Version="<latest>" />
  <PackageReference Include="MotorDsl.Bluetooth" Version="<latest>" />
</ItemGroup>
```

`MotorDsl.Maui` trae como dependencias transitivas `MotorDsl.Core`,
`MotorDsl.Parser`, `MotorDsl.Rendering`, `MotorDsl.Extensions` y
`MotorDsl.Printing.Abstractions`.

Para una app de consola sin Bluetooth (sólo render):

```bash
dotnet add package MotorDsl.Extensions
```

---

## 4. Configuración inicial

Registrar los servicios del motor + impresión:

```csharp
using Microsoft.Extensions.DependencyInjection;
using MotorDsl.Bluetooth;
using MotorDsl.Core.Models;
using MotorDsl.Extensions;
using MotorDsl.Maui;

var services = new ServiceCollection();

services.AddMotorDslEngine()
    .AddProfiles(p =>
    {
        p.Add(new DeviceProfile("thermal_58mm", 32, "escpos-bitmap"));
        p.Add(new DeviceProfile("preview", 32, "raster-preview"));
        p.Add(new DeviceProfile("a4-pdf", 80, "pdf"));
    })
    .AddMotorDslMaui();           // PDF + ESC/POS bitmap + raster preview + IThermalPrinterService

services.AddBluetoothPrinterTransport();   // Transport BT (Android Classic SPP)

var sp = services.BuildServiceProvider();
```

Para una app .NET MAUI, este registro va dentro de `MauiProgram.CreateMauiApp()`
sobre `builder.Services`.

---

## 5. Resolución del motor y servicios

```csharp
var engine  = sp.GetRequiredService<IDocumentEngine>();
var printer = sp.GetRequiredService<IThermalPrinterService>();
```

---

## 6. Uso básico del motor

### Modo template (con datos)

```csharp
var templateJson = @"{
  ""id"": ""hola"",
  ""version"": ""1.0"",
  ""root"": {
    ""type"": ""text"",
    ""text"": ""Hola {{Nombre}}"",
    ""style"": { ""align"": ""center"", ""bold"": true }
  }
}";

var data    = new Dictionary<string, object> { ["Nombre"] = "Mundo" };
var profile = new DeviceProfile("thermal_58mm", 32, "escpos-bitmap");

var result = engine.Render(templateJson, data, profile);
if (result.IsSuccessful)
{
    byte[] bytes = (byte[])result.Output!;
    // listo para enviar a impresora
}
```

### Modo integrated (JSON pre-resuelto)

```csharp
var integratedJson = @"{
  ""id"": ""snapshot"",
  ""version"": ""1.0"",
  ""format"": ""integrated"",
  ""root"": {
    ""type"": ""container"",
    ""children"": [
      { ""type"": ""text"", ""value"": ""MI TIENDA"", ""style"": { ""align"": ""center"" } }
    ]
  }
}";

var profile = new DeviceProfile("thermal_58mm", 32, "escpos-bitmap");
var result  = engine.Render(integratedJson, profile);   // sin parámetro `data`
```

---

## 7. Conexión y envío a impresora

### 7.1 Descubrir dispositivos

```csharp
IReadOnlyList<PrinterDevice> devices =
    await printer.DiscoverDevicesAsync(kind: "bluetooth");

foreach (var d in devices)
    Console.WriteLine($"{d.Name} ({d.Id}) — kind={d.Kind}, paired={d.IsPaired}");
```

`kind` es opcional. Si se pasa `null`, descubre con todos los transports
registrados.

### 7.2 Conectar

```csharp
PrinterDevice device = devices.First();
bool ok = await printer.ConnectAsync(device);

if (ok)
    Console.WriteLine($"Conectado: {printer.CurrentDevice?.Name}");
```

`printer.IsConnected`, `printer.CurrentDevice` y `printer.ConnectionState` se
actualizan automáticamente y disparan `INotifyPropertyChanged`.

### 7.3 Enviar bytes

```csharp
byte[] bytes = (byte[])result.Output!;
await printer.SendBytesAsync(bytes);
```

Sobrecargas:

```csharp
await printer.SendBytesAsync(
    bytes,
    profile: PrinterProfile.Real58HB6,
    retry:   new PrintRetryOptions { MaxRetries = 5, InitialDelayMs = 200 });
```

El servicio aplica retry exponencial y delega la decisión al
`IPrintErrorHandler` registrado.

### 7.4 Reconectar

```csharp
bool ok = await printer.ReconnectAsync();   // usa CurrentDevice
```

### 7.5 Eventos

```csharp
printer.DevicesDiscovered += (_, args) =>
    Console.WriteLine($"Descubiertos {args.Devices.Count}");

printer.ErrorOccurred += (_, args) =>
    Console.WriteLine($"[Print Error] {args.Error.Type}: {args.Error.Message}");

printer.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(IThermalPrinterService.ConnectionState))
        Console.WriteLine($"Estado → {printer.ConnectionState}");
};
```

---

## 8. Uso de perfiles de dispositivo

```csharp
var profile = new DeviceProfile("58HB6", 32, "escpos-bitmap");
profile.SetCapability("supports_bitmap", true);
profile.SetCapability("bitmap_max_width_px", 320);
profile.SetCapability("bitmap_binarization_threshold", 128);
```

Targets disponibles:

| Target | Output | Renderer | Paquete |
|---|---|---|---|
| `text` | `string` | TextRenderer | MotorDsl.Rendering |
| `escpos` | `byte[]` ESC/POS | EscPosRenderer | MotorDsl.Rendering |
| `escpos-bitmap` | `byte[]` GS v 0 | BitmapEscPosRenderer | MotorDsl.Maui |
| `pdf` | `byte[]` PDF | PdfRenderer | MotorDsl.Maui |
| `raster-preview` | `byte[]` PNG | RasterPreviewRenderer | MotorDsl.Maui |

---

## 9. Manejo de resultados

```csharp
if (result.IsSuccessful)
{
    if (result.Output is byte[] bytes) { /* binario */ }
    if (result.Output is string text)  { /* texto */ }
}
else
{
    foreach (var err in result.Errors) Console.WriteLine($"ERROR: {err}");
}

foreach (var w in result.Warnings) Console.WriteLine($"WARN: {w}");

// Helpers
string? hex    = result.ToHexString();   // "1B 40 ..." si Output es byte[]
string? base64 = result.ToBase64();      // para email/API
```

---

## 10. Manejo de errores de impresión

```csharp
try
{
    await printer.SendBytesAsync(bytes);
}
catch (Exception ex)
{
    // Stack del retry agotado o de la conexión
    Console.WriteLine($"Falló impresión: {ex.Message}");
    // Opcional: persistir bytes para reenvío
    File.WriteAllText("pending.b64", Convert.ToBase64String(bytes));
}
```

El `IPrintErrorHandler` decide qué tipos son retryables. Para feedback en UI,
usar `MauiPrintErrorHandler` y suscribirse a `RetryAttempted` / `Succeeded`.

---

## 11. Integración en aplicaciones MAUI

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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Bindear los controles MAUI al servicio
        StatusBadge.Service  = _printer;
        DevicePicker.Service = _printer;
    }

    private async void OnImprimirClicked(object? sender, EventArgs e)
    {
        var profile = new DeviceProfile("thermal_58mm", 32, "escpos-bitmap");
        var result  = _engine.Render(jsonDsl, profile);
        if (result.IsSuccessful && _printer.IsConnected)
            await _printer.SendBytesAsync((byte[])result.Output!);
    }
}
```

XAML:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:muic="clr-namespace:MotorDsl.Maui.Controls;assembly=MotorDsl.Maui">
    <VerticalStackLayout Padding="12" Spacing="10">
        <muic:PrinterStatusBadge x:Name="StatusBadge" />
        <muic:PrinterPickerView  x:Name="DevicePicker" FilterKind="bluetooth" />
        <Button Text="Imprimir" Clicked="OnImprimirClicked" />
    </VerticalStackLayout>
</ContentPage>
```

Más detalles en `docs/10_developer_guide/guia-integracion-maui.md`.

---

## 12. Buenas prácticas

- Mantener templates DSL simples y reutilizables.
- Separar datos de presentación.
- Definir perfiles de dispositivo claros con capabilities explícitos.
- Validar datos antes del render.
- Cachear `DocumentTemplate` si el DSL no cambia.
- Reutilizar el `IThermalPrinterService` (singleton) entre páginas.
- No bloquear la UI: `SendBytesAsync` puede tardar varios segundos en BT.

---

## 13. Organización recomendada del proyecto

```text
App
 ├── MauiProgram.cs              ← AddMotorDslEngine + AddMotorDslMaui + AddBluetoothPrinterTransport
 ├── Templates/                  ← strings DSL JSON (template o integrated)
 ├── Models/                     ← DTOs / data dictionaries
 ├── Pages/                      ← Bind controles MAUI al servicio
 ├── Resources/Raw/              ← profiles si vienen de archivo
 └── Platforms/Android/AndroidManifest.xml   ← permisos BT
```

---

## 14. Performance

- Reutilizar instancias del motor (singleton).
- Evitar parsing repetido (cachear `DocumentTemplate`).
- Minimizar evaluaciones de expresiones complejas en el DSL.
- Usar perfiles adecuados al dispositivo (no enviar bitmaps a impresoras sin
  `supports_bitmap=true`).

---

## 15. Extensibilidad desde el consumidor

- Renderers custom: `MotorDslBuilder.AddRenderer<T>()`.
- **Transports custom**: `IServiceCollection.AddSingleton<IThermalPrinterTransport, MyTransport>()`.
- DataResolver, DeviceProfileProvider, PrintErrorHandler reemplazables.

Ver `extensibilidad-motor_v1.1.md` y
`docs/10_developer_guide/transports-y-extensibilidad.md`.

---

## 16. Troubleshooting

### No se encuentra el renderer

- Verificar registro vía `AddRenderer<T>()` o `AddMotorDslMaui()`.
- Verificar `Target` del `DeviceProfile`.

### `InvalidOperationException: No IThermalPrinterTransport registered`

- Falta `services.AddBluetoothPrinterTransport()` u otro transport.

### `InvalidOperationException: No hay un transport activo`

- Llamar `await printer.ConnectAsync(device)` antes de `SendBytesAsync`.

### Permisos BT denegados (Android 12+)

- Solicitar `BLUETOOTH_SCAN` y `BLUETOOTH_CONNECT` en runtime.

### Bindings UNRESOLVED

- Typo en la ruta del binding.
- Dato faltante en el diccionario.

---

## 17. Relación con otros documentos

- `arquitectura-solucion_v1.1.md`
- `contratos-del-motor_v1.0.md`
- `modelo-datos-logico_v1.0.md`
- `flujo-ejecucion-motor_v1.0.md`
- `extensibilidad-motor_v1.1.md`
- `compatibilidad-plataformas_v1.1.md`
- `docs/10_developer_guide/guia-integracion-maui.md`
- `docs/10_developer_guide/componentes-ux-maui.md`
- `docs/10_developer_guide/render-pixelado-y-pdf.md`

---

## 18. Historial de versiones

| Versión | Fecha      | Autor          | Cambios                                                       |
| ------- | ---------- | -------------- | ------------------------------------------------------------- |
| 1.0     | 2026-03-28 | Equipo Técnico | Guía inicial de uso.                                          |
| 1.1     | 2026-05-06 | Equipo Técnico | Quickstart con paquetes nuevos, API `IThermalPrinterService` redefinida, transports. |

---
