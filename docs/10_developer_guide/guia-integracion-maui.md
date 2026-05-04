# Guía de Integración en .NET MAUI

Guía paso a paso para integrar el motor DSL en una aplicación .NET MAUI con impresión Bluetooth.

---

## 1. Agregar referencia al proyecto

### Vía ProjectReference (desarrollo local)

En el `.csproj` de tu app MAUI:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\MotorDsl.Core\MotorDsl.Core.csproj" />
  <ProjectReference Include="..\..\src\MotorDsl.Parser\MotorDsl.Parser.csproj" />
  <ProjectReference Include="..\..\src\MotorDsl.Rendering\MotorDsl.Rendering.csproj" />
  <ProjectReference Include="..\..\src\MotorDsl.Extensions\MotorDsl.Extensions.csproj" />
</ItemGroup>
```

### Vía NuGet (cuando esté publicado)

```xml
<ItemGroup>
  <PackageReference Include="MotorDsl.Extensions" Version="1.0.0" />
</ItemGroup>
```

> El paquete `MotorDsl.Extensions` trae como dependencias transitivas `MotorDsl.Core`, `MotorDsl.Parser` y `MotorDsl.Rendering`.

---

## 2. Registrar con AddMotorDslEngine()

En `MauiProgram.cs`:

```csharp
using MotorDsl.Extensions;

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

        // ── Motor DSL ──────────────────────────────────
        // Registra: IDslParser, IEvaluator, ILayoutEngine,
        //           IRendererRegistry (TextRenderer + EscPosRenderer),
        //           IDocumentEngine
        builder.Services.AddMotorDslEngine();

        // ── Providers (opcional) ───────────────────────
        // Templates precargados
        builder.Services.AddSingleton<ITemplateProvider>(sp =>
        {
            var provider = new InMemoryTemplateProvider();
            provider.Add("ticket-simple", File.ReadAllText("templates/ticket-simple.json"));
            provider.Add("ticket-qr", File.ReadAllText("templates/ticket-con-qr.json"));
            return provider;
        });

        // Perfiles de dispositivo
        builder.Services.AddSingleton<IDeviceProfileProvider>(sp =>
        {
            var provider = new InMemoryDeviceProfileProvider();
            provider.Add(new DeviceProfile("58mm", 32, "escpos"));
            provider.Add(new DeviceProfile("80mm", 48, "escpos"));
            provider.Add(new DeviceProfile("preview", 48, "text"));
            return provider;
        });

        // ── Servicios de la app ────────────────────────
        builder.Services.AddSingleton<IThermalPrinterService, ThermalPrinterService>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
```

### Qué registra `AddMotorDslEngine()`

| Servicio | Implementación | Lifetime | Rol |
|----------|---------------|----------|-----|
| `IDslParser` | `DslParser` | Singleton | Parsea JSON DSL a `DocumentTemplate` |
| `IEvaluator` | `Evaluator` | Singleton | Resuelve bindings y condiciones |
| `ILayoutEngine` | `LayoutEngine` | Singleton | Aplica layout según `DeviceProfile.Width` |
| `IRendererRegistry` | `RendererRegistry` | Singleton | Contiene `TextRenderer` + `EscPosRenderer` |
| `IDocumentEngine` | `DocumentEngine` | Singleton | Orquesta todo el pipeline |

---

## 3. Inyectar y usar IDocumentEngine

### Constructor injection en una ContentPage

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

### Ejemplo — Render a texto (preview/debug)

```csharp
var templateJson = """
{
  "id": "test",
  "version": "1.0",
  "root": {
    "type": "text",
    "text": "Hola {{nombre}}",
    "style": { "align": "center", "bold": true }
  }
}
""";

var data = new Dictionary<string, object> { ["nombre"] = "Mundo" };
var profile = new DeviceProfile("preview", 32, "text");

RenderResult result = _engine.Render(templateJson, data, profile);

if (result.IsSuccessful)
{
    // result.Output es string cuando Target = "text"
    string texto = result.Output!.ToString()!;
    PreviewLabel.Text = texto;
}
```

### Ejemplo — Render a ESC/POS (impresión térmica)

```csharp
var profile = new DeviceProfile("58mm", 32, "escpos");
RenderResult result = _engine.Render(templateJson, data, profile);

if (result.IsSuccessful)
{
    // result.Output es byte[] cuando Target = "escpos"
    byte[] bytes = (byte[])result.Output!;

    // Helpers para inspección
    string? hex = result.ToHexString();    // "1B 40 1B 61 01 ..."
    string? base64 = result.ToBase64();    // "G0BhAR0n..."
}
```

### ToHexString() y ToBase64()

| Método | Retorna | Cuándo es útil |
|--------|---------|----------------|
| `ToHexString()` | `"1B 40 1B 61 01 ..."` o `null` | Debug visual de comandos ESC/POS. Cada byte separado por espacio. |
| `ToBase64()` | `"G0BhAR0n..."` o `null` | Serialización para almacenamiento o reenvío. Convertir de vuelta con `Convert.FromBase64String()`. |

Ambos retornan `null` si `Output` no es `byte[]`.

### Ejemplo — Render con formato integrado (sin diccionario de datos)

Cuando el JSON ya viene con todos los valores resueltos (`"format": "integrated"`), se usa el overload simplificado `Render(json, profile)` — sin parámetro `data`. Útil cuando el documento se construyó previamente en un backend o un job batch.

```csharp
var integratedJson = """
{
  "id": "ticket-snapshot-001",
  "version": "1.0",
  "format": "integrated",
  "root": {
    "type": "container",
    "layout": "vertical",
    "children": [
      { "type": "text", "value": "MI TIENDA",      "style": { "align": "center", "bold": true } },
      { "type": "text", "value": "Cliente: Juan Pérez" },
      { "type": "text", "value": "Total: $12.345" }
    ]
  }
}
""";

var profile = new DeviceProfile("58mm", 32, "escpos");

// Nota: este overload no recibe `data` — el JSON ya está resuelto.
RenderResult result = _engine.Render(integratedJson, profile);

if (result.IsSuccessful)
{
    byte[] bytes = (byte[])result.Output!;
    await _printer.SendBytesAsync(bytes);
}
```

| Aspecto | Modo clásico | Modo integrado |
|---|---|---|
| Llamada | `_engine.Render(json, data, profile)` | `_engine.Render(json, profile)` |
| JSON de entrada | tiene `{{placeholders}}`, `loop`, `conditional` | tiene `value` resueltos, sin loops ni conditionals |
| Diccionario de datos | requerido | no aplica |
| Pipeline interno | Parse → Validate → **Evaluate** → Layout → Render | Parse → Validate → Layout → Render |

---

## 4. Enviar a impresora Bluetooth

### Separación de responsabilidades

| Componente | Responsabilidad |
|------------|----------------|
| **Motor DSL** (librería) | Genera `byte[]` con comandos ESC/POS a partir de un template + datos + perfil. |
| **App MAUI** (consumidora) | Conecta a la impresora BT, envía los bytes, maneja errores de comunicación. |

La librería no conoce Bluetooth. Tu app es la que conecta, envía y desconecta.

### Flujo completo

```csharp
// 1. Preparar datos
var templateJson = templateProvider.GetTemplate("ticket-simple");
var data = new Dictionary<string, object>
{
    ["nombreNegocio"] = "Mi Café",
    ["fecha"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
    ["items"] = new List<Dictionary<string, object>>
    {
        new() { ["nombre"] = "Café", ["cantidad"] = "2",
                ["precio"] = "150.00", ["total"] = "300.00" },
        new() { ["nombre"] = "Medialunas", ["cantidad"] = "6",
                ["precio"] = "50.00", ["total"] = "300.00" }
    },
    ["subtotal"] = "600.00",
    ["impuesto"] = "126.00",
    ["total"] = "726.00",
    ["footer"] = "Gracias por su compra!"
};

// 2. Renderizar
var profile = new DeviceProfile("58mm", 32, "escpos");
RenderResult result = _engine.Render(templateJson!, data, profile);

// 3. Verificar resultado
if (!result.IsSuccessful)
{
    await DisplayAlert("Error", string.Join("\n", result.Errors), "OK");
    return;
}

// 4. Enviar a impresora
byte[] escposBytes = (byte[])result.Output!;
var printerProfile = PrinterProfile.Thermal58mm;

try
{
    await _printer.SendBytesAsync(escposBytes, printerProfile);
    await DisplayAlert("OK", "Ticket impreso", "OK");
}
catch (Exception ex)
{
    await DisplayAlert("Error BT", ex.Message, "OK");
}
```

### IThermalPrinterService — interfaz de referencia

```csharp
public interface IThermalPrinterService
{
    bool IsConnected { get; }
    Task<List<BluetoothDevice>> ScanDevicesAsync();
    Task<bool> ConnectAsync(string deviceAddress);
    Task DisconnectAsync();
    Task SendBytesAsync(byte[] data, PrinterProfile? profile = null);
}
```

Esta interfaz vive en la app consumidora (`MotorDsl.SampleApp.Services`), no en la librería core. Cada app puede implementar su propia versión.

---

## 5. Manejo de errores

### Errores de renderizado

```csharp
RenderResult result = _engine.Render(templateJson, data, profile);

if (!result.IsSuccessful)
{
    // result.Errors contiene los mensajes de error
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"ERROR: {error}");
    }
    // Errores típicos:
    // - "Parse failed: ..." → JSON inválido o estructura DSL incorrecta
    // - "Render failed: ..." → Error durante evaluación/layout/render
}

// Warnings no bloquean el render pero avisan de situaciones
foreach (var warning in result.Warnings)
{
    Console.WriteLine($"WARNING: {warning}");
}
```

### Errores de impresión

```csharp
try
{
    await _printer.SendBytesAsync(escposBytes, printerProfile);
}
catch (InvalidOperationException)
{
    // No hay impresora conectada
    await DisplayAlert("Error", "Conectá una impresora primero", "OK");
}
catch (Exception ex)
{
    // Error de comunicación BT — guardar para reenvío
    string base64 = result.ToBase64()!;
    await SaveForRetryAsync(base64);  // Guardar en SQLite, preferences, etc.

    await DisplayAlert("Error", $"Falló la impresión: {ex.Message}", "OK");
}
```

### Reenvío con ToBase64()

```csharp
// Guardar
string base64 = result.ToBase64()!;
Preferences.Set("pending_print", base64);

// Recuperar y reenviar
string saved = Preferences.Get("pending_print", "");
if (!string.IsNullOrEmpty(saved))
{
    byte[] bytes = Convert.FromBase64String(saved);
    await _printer.SendBytesAsync(bytes, PrinterProfile.Thermal58mm);
    Preferences.Remove("pending_print");
}
```

---

## 6. Tips y troubleshooting

### Debuggear un template con TextRenderer

Antes de enviar a la impresora, siempre podés previsualizar con texto plano:

```csharp
// Usar "text" en vez de "escpos" para ver el output legible
var previewProfile = new DeviceProfile("debug", 32, "text");
var result = _engine.Render(templateJson, data, previewProfile);
Console.WriteLine(result.Output);
```

Esto produce la salida como texto plano (sin comandos binarios), ideal para verificar layout, alineación y bindings.

### Binding no resuelto (UNRESOLVED)

Si un binding no encuentra su dato en el diccionario, el motor lo deja como `{{UNRESOLVED:ruta}}` en la salida:

```
Cliente: {{UNRESOLVED:cliente.nombre}}
```

**Causas comunes:**
- Typo en el binding: `{{clinte.nombre}}` en vez de `{{cliente.nombre}}`
- El dato no fue incluido en el diccionario
- En un loop, usar la ruta sin el alias: `{{nombre}}` en vez de `{{item.nombre}}`

**Solución:** verificar que la clave en el diccionario de datos coincida exactamente con la ruta del binding.

### Ajustar Width para tu impresora

Si el texto no entra en la línea o sobra espacio:

1. Imprimir el template de medición (ver guía de perfiles)
2. Contar los caracteres que entran
3. Actualizar el `Width` en tu `DeviceProfile`

Recordá: 58mm ≈ 32 chars, 80mm ≈ 48 chars con fuente estándar.

### Permisos Android para Bluetooth

Tu `AndroidManifest.xml` necesita estos permisos:

```xml
<!-- Bluetooth (legacy API < 31) -->
<uses-permission android:name="android.permission.BLUETOOTH" />
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN" />

<!-- Bluetooth (API 31+ / Android 12+) -->
<uses-permission android:name="android.permission.BLUETOOTH_SCAN"
                 android:usesPermissionFlags="neverForLocation" />
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />

<!-- Ubicación (necesario para escaneo BT en Android < 12) -->
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
```

Además, en **runtime** (Android 12+) debés solicitar `BLUETOOTH_SCAN` y `BLUETOOTH_CONNECT` antes de escanear o conectar:

```csharp
var status = await Permissions.RequestAsync<Permissions.Bluetooth>();
if (status != PermissionStatus.Granted)
{
    await DisplayAlert("Permiso", "Se necesita Bluetooth para imprimir", "OK");
    return;
}
```

En Android < 12, solicitar `Permissions.LocationWhenInUse` en su lugar.

---

## 7. Compatibilidad de plataformas

La librería core es multiplataforma (.NET Standard), pero la conectividad Bluetooth para impresoras térmicas solo está disponible en Android.

| Componente | Android | iOS | Windows |
|---|---|---|---|
| MotorDsl.Core (parser, evaluator, layout) | ✅ | ✅ | ✅ |
| MotorDsl.Rendering (text, ESC/POS) | ✅ | ✅ | ✅ |
| ThermalPrinterService (Bluetooth SPP) | ✅ | ❌ | ❌ |
| SkiaSharp (rasterización de imágenes) | ✅ | ✅ | ✅ |
| QuestPDF (generación PDF) | ✅ | ✅ | ✅ |

### ¿Por qué iOS no soporta Bluetooth para térmicas?

Apple restringe el acceso a **Bluetooth clásico** (perfil SPP — Serial Port Profile) desde aplicaciones de terceros. Las impresoras térmicas ESC/POS estándar utilizan SPP para la comunicación serie. iOS solo expone **Bluetooth Low Energy (BLE)** a través de CoreBluetooth, y la mayoría de impresoras térmicas económicas no soportan BLE.

Esto no es una limitación de .NET MAUI ni de la librería — es una restricción del sistema operativo iOS.

### Alternativas para iOS

Si necesitás imprimir desde iOS, estas son las opciones disponibles:

| Alternativa | Descripción |
|---|---|
| **Impresoras WiFi** | Algunas impresoras térmicas soportan conexión WiFi. El motor genera los mismos bytes ESC/POS, solo cambia el transporte (TCP socket en vez de Bluetooth). |
| **AirPrint** | Usar el renderer PDF (`PdfRenderer`) y enviar el PDF a una impresora compatible con AirPrint. No usa ESC/POS. |
| **Hardware MFi** | Impresoras certificadas por Apple bajo el programa MFi (Made for iPhone) permiten comunicación Bluetooth clásica, pero son significativamente más caras. |
| **BLE (impresoras compatibles)** | Algunas impresoras modernas soportan BLE además de SPP. Requiere implementar un `IThermalPrinterService` que use CoreBluetooth. |

> **Nota:** La librería core funciona perfectamente en iOS para generar documentos en formato texto o PDF. Solo la capa de transporte Bluetooth es Android-only.
