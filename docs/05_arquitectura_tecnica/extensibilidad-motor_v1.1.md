# Extensibilidad del Motor — Librería DSL de Renderizado
**Archivo:** extensibilidad-motor_v1.1.md
**Versión:** 1.1
**Fecha:** 2026-05-06
**Autor:** Equipo Técnico
**Estado:** Vigente

---

## Cambios desde v1.0

- Nuevo extension point **`IThermalPrinterTransport`** (en
  `MotorDsl.Printing.Abstractions`) para agregar transports físicos custom
  (USB, Red, BLE) sin tocar el orquestador.
- `BluetoothPrinterTransport` (paquete `MotorDsl.Bluetooth`) ahora es la
  implementación de referencia y queda como modelo a copiar.
- Nueva extensión fluent **`AddMotorDslMaui()`** que encadena al
  `MotorDslBuilder` y registra los renderers MAUI (`PdfRenderer`,
  `BitmapEscPosRenderer`, `RasterPreviewRenderer`) más el orquestador
  `IThermalPrinterService`.
- **`MauiPrintErrorHandler`** demuestra cómo extender el handler de errores
  vía `DefaultPrintErrorHandler` agregando eventos para feedback de UI.
- Nuevos renderers MAUI registrables vía `AddRenderer<T>()`.

---

## 1. Propósito

Este documento describe los mecanismos de extensibilidad del Motor DSL de
renderizado en su versión 1.1. Define cómo terceros pueden ampliar el
comportamiento del motor sin modificar su núcleo, manteniendo desacoplamiento
y compatibilidad entre versiones.

La extensibilidad permite:

- Incorporar nuevos renderizadores (`IRenderer`).
- Agregar nuevos tipos de nodos DSL (`DocumentNode`).
- Extender capacidades de layout (`ILayoutEngine`).
- **Agregar transports de impresión** (`IThermalPrinterTransport`) — nuevo en v1.1.
- Reemplazar el handler de errores (`IPrintErrorHandler`).
- Integrar nuevos perfiles de dispositivo.

---

## 2. Principios de extensibilidad

- **Open/Closed Principle**: abierto a extensión, cerrado a modificación.
- **Inversión de dependencias**: el core depende de abstracciones.
- **Plug-in architecture**: componentes reemplazables.
- **Desacoplamiento por `Target` / `Kind`**: cada plugin se identifica con un
  string (renderer.Target, transport.Kind) y se enruta automáticamente.
- **Registro explícito** vía DI (`IServiceCollection`).

---

## 3. Puntos de extensión principales

| # | Punto de extensión | Contrato | Paquete |
|---|---|---|---|
| 1 | Renderizadores | `IRenderer` | `MotorDsl.Core` |
| 2 | Nodos DSL custom | `DocumentNode` | `MotorDsl.Core` |
| 3 | Layout engine | `ILayoutEngine` | `MotorDsl.Core` |
| 4 | Resolver de datos | `IDataResolver` | `MotorDsl.Core` |
| 5 | Profile provider | `IDeviceProfileProvider` | `MotorDsl.Core` |
| 6 | **Transport físico** | **`IThermalPrinterTransport`** | **`MotorDsl.Printing.Abstractions`** |
| 7 | **Error handler** | **`IPrintErrorHandler`** | **`MotorDsl.Core`** |
| 8 | Bitmap rasterizer | `IBitmapRasterizer` | `MotorDsl.Core` |

---

## 4. Extensión: Renderizadores

### Objetivo

Generar salidas en distintos formatos (UI, ESC/POS, PDF, raster, HTML, etc.).

### Contrato

```csharp
public interface IRenderer
{
    string Target { get; }
    RenderResult Render(LayoutedDocument document, DeviceProfile profile);
}
```

### Registro

Vía fluent en `MotorDslBuilder`:

```csharp
builder.Services.AddMotorDslEngine()
    .AddRenderer<HtmlRenderer>();
```

### Ejemplo

```csharp
public class HtmlRenderer : IRenderer
{
    public string Target => "html";

    public RenderResult Render(LayoutedDocument document, DeviceProfile profile)
    {
        var result = new RenderResult(Target);
        // construir HTML
        result.Output = "<html>...</html>";
        return result;
    }
}
```

El consumidor lo activa creando un `DeviceProfile(name, width, "html")`.

---

## 5. Extensión: Nuevos nodos DSL

Heredar de `DocumentNode`, agregar propiedades específicas y extender el parser
(`IDslParser`) para reconocerlos.

```csharp
public class BarcodeNode : DocumentNode
{
    public string Value { get; set; } = "";
    public string Format { get; set; } = "EAN13";
}
```

---

## 6. Extensión: Layout Engine

Permite reorganizar nodos antes del render (reflow, márgenes dinámicos,
adaptación a dispositivo).

```csharp
public interface ILayoutEngine
{
    LayoutedDocument ApplyLayout(EvaluatedDocument document, DeviceProfile profile);
}
```

Reemplazar el default vía `services.Replace<ILayoutEngine, MyLayoutEngine>()`.

---

## 7. Extension points de transporte (NUEVO en v1.1)

### Objetivo

Permitir enviar bytes ESC/POS por canales distintos a Bluetooth (USB, red TCP,
BLE, MFi, AirPrint) sin modificar el orquestador.

### Contrato `IThermalPrinterTransport`

Vive en `MotorDsl.Printing.Abstractions`.

```csharp
public interface IThermalPrinterTransport
{
    string Kind { get; }                                   // "bluetooth", "usb", "wifi", ...
    bool IsConnected { get; }
    PrinterDevice? CurrentDevice { get; }

    Task<IReadOnlyList<PrinterDevice>> DiscoverAsync(CancellationToken ct = default);
    Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default);
    Task DisconnectAsync();
    Task WriteBytesAsync(byte[] data, PrinterProfile profile, CancellationToken ct = default);
}
```

### Cómo enruta el orquestador

`ThermalPrinterService` recibe **todos** los `IThermalPrinterTransport`
registrados y elige uno por `device.Kind`:

```csharp
var transport = _transports.FirstOrDefault(t =>
    string.Equals(t.Kind, device.Kind, StringComparison.OrdinalIgnoreCase));
```

`DiscoverDevicesAsync(kind)` permite filtrar por un kind específico, o pasar
`null` para descubrir en todos los transports.

### Implementación de referencia: `BluetoothPrinterTransport`

```csharp
public class BluetoothPrinterTransport : IThermalPrinterTransport
{
    public string Kind => "bluetooth";
    public bool IsConnected { get; private set; }
    public PrinterDevice? CurrentDevice { get; private set; }

    public Task<IReadOnlyList<PrinterDevice>> DiscoverAsync(CancellationToken ct = default)
    {
        // Enumera BondedDevices del adapter Android, devuelve PrinterDevice(addr, name, "bluetooth")
        // En iOS: throw new PlatformNotSupportedException(...)
    }

    public async Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default)
    {
        // CreateRfcommSocketToServiceRecord(SPP_UUID).Connect()
        // Guardar OutputStream para WriteBytesAsync
    }

    public async Task WriteBytesAsync(byte[] data, PrinterProfile profile, CancellationToken ct = default)
    {
        // Escribir línea por línea con delays según el profile (LineDelayMs, ByteDelayMs, etc.)
    }
}
```

### Ejemplo: escribir un transport USB

```csharp
public class UsbPrinterTransport : IThermalPrinterTransport
{
    public string Kind => "usb";
    public bool IsConnected { get; private set; }
    public PrinterDevice? CurrentDevice { get; private set; }

    public Task<IReadOnlyList<PrinterDevice>> DiscoverAsync(CancellationToken ct = default)
    {
        // En Android: UsbManager.DeviceList con filtro por VID/PID conocidos
        // En Windows: WinUSB / SerialPort.GetPortNames()
        // Devolver PrinterDevice(deviceId, name, "usb", IsPaired: false)
    }

    public Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default) { /* ... */ }
    public Task DisconnectAsync() { /* ... */ }
    public Task WriteBytesAsync(byte[] data, PrinterProfile profile, CancellationToken ct = default) { /* ... */ }
}
```

### Registro en DI

```csharp
builder.Services.AddBluetoothPrinterTransport();          // del paquete MotorDsl.Bluetooth
builder.Services.AddSingleton<IThermalPrinterTransport, UsbPrinterTransport>();
```

El servicio descubrirá automáticamente ambos. Una sola llamada a
`DiscoverDevicesAsync()` retorna dispositivos de los dos. `ConnectAsync(device)`
elige el transport correcto según `device.Kind`.

---

## 8. Extensión: Resolución de datos

```csharp
public interface IDataResolver
{
    object? Resolve(object? data, string path);
    IEnumerable<object> ResolveCollection(object? data, string path);
}
```

Estrategias: reflection, JSON path, expression-based, custom mapping.

---

## 9. Extensión: Proveedores de perfiles

```csharp
public interface IDeviceProfileProvider
{
    DeviceProfile? GetProfile(string name);
    IEnumerable<DeviceProfile> GetAll();
}
```

Los perfiles también pueden registrarse fluent vía
`MotorDslBuilder.AddProfiles(p => p.Add(...))`.

---

## 10. Extensión: Error handler de impresión

`IPrintErrorHandler` decide si un error es transitorio (retry) o terminal
(abort), y notifica el ciclo de vida.

```csharp
public interface IPrintErrorHandler
{
    Task<bool> HandleErrorAsync(PrintError error);   // retry?
    void OnRetryAttempt(PrintError error);
    void OnPrintSuccess(int totalAttempts);
}
```

`MauiPrintErrorHandler` (incluido en `MotorDsl.Maui`) extiende
`DefaultPrintErrorHandler` agregando eventos `RetryAttempted` / `Succeeded` que
una app puede consumir para mostrar snackbars/toasts:

```csharp
public class MauiPrintErrorHandler : DefaultPrintErrorHandler
{
    public event EventHandler<PrintError>? RetryAttempted;
    public event EventHandler<int>? Succeeded;

    public override Task<bool> HandleErrorAsync(PrintError error) { /* log + base */ }
    public override void OnRetryAttempt(PrintError error) { /* base + invoke event */ }
    public override void OnPrintSuccess(int totalAttempts) { /* base + invoke event */ }
}
```

`AddMotorDslMaui()` reemplaza el handler por defecto con éste vía
`services.Replace(ServiceDescriptor.Singleton<IPrintErrorHandler, MauiPrintErrorHandler>())`.

---

## 11. Registro de extensiones (Dependency Injection)

```csharp
builder.Services.AddMotorDslEngine()
    .AddTemplates(t => { /* ... */ })
    .AddProfiles(p => { /* ... */ })
    .AddRenderer<MyHtmlRenderer>()
    .AddMotorDslMaui();                                 // PDF + ESC/POS bitmap + raster + service

builder.Services.AddBluetoothPrinterTransport();        // BT Android (paquete MotorDsl.Bluetooth)
builder.Services.AddSingleton<IThermalPrinterTransport, UsbPrinterTransport>(); // USB custom

// Reemplazar handler de errores:
builder.Services.Replace(
    ServiceDescriptor.Singleton<IPrintErrorHandler, MyCustomErrorHandler>());
```

---

## 12. Descubrimiento de extensiones

- Registro explícito manual (recomendado).
- Auto-discovery por reflection (opcional, para plugins externos).
- Carga de plugins externos (assembly scanning) — escenario avanzado.

---

## 13. Versionado de extensiones

Recomendaciones:

- Mantener compatibilidad de interfaces.
- Evitar breaking changes en contratos públicos.
- Introducir nuevas interfaces en lugar de modificar las existentes.
- Versionar plugins independientemente del core.

---

## 14. Buenas prácticas

- Implementar extensiones como componentes pequeños y cohesivos.
- Evitar lógica de negocio compleja dentro de renderizadores.
- Mantener nodos DSL simples y composables.
- Mantener transports **ignorantes del retry**: el retry es responsabilidad
  del orquestador.
- Documentar cada extensión públicamente.
- Registrar extensiones de forma explícita.

---

## 15. Casos de uso de extensibilidad

- Renderizar a nuevos formatos (HTML, PDF custom, imagen).
- **Soportar nuevos canales de impresión** (USB, BLE, AirPrint, MFi, red TCP).
- Introducir nuevos nodos DSL (QR, barcode, gráficos).
- Adaptar layout para nuevos dispositivos.
- Conectar con distintas fuentes de datos.
- Implementar telemetría de impresión a través de un `IPrintErrorHandler` custom.

---

## 16. Riesgos conocidos

- Múltiples transports con el mismo `Kind` causan que sólo el primero registrado
  sea elegible (`FirstOrDefault`). Validar `Kind` único en testing.
- Plugins mal diseñados pueden afectar performance.
- Conflictos entre implementaciones múltiples de una misma interfaz.
- Incompatibilidades entre versiones de contratos.

---

## 17. Evolución prevista

- Transports oficiales adicionales (USB, BLE, Red TCP) en próximas versiones.
- Sistema formal de plugins con metadata.
- Sandbox para ejecución de extensiones.
- Hot-reload de extensiones en runtime.

---

## 18. Compatibilidad con el formato integrado

El formato integrado (`DocumentTemplate.Format == "integrated"`) sigue
beneficiándose transparentemente de los puntos de extensión existentes:

- **Renderizadores custom**: cualquier `IRenderer` registrado funciona idéntico
  para ambos formatos. El renderer recibe un `LayoutedDocument` y no inspecciona
  el `Format` del template original.
- **Layout engines custom**: una implementación alternativa de `ILayoutEngine`
  se aplica de la misma forma a documentos clásicos e integrados.
- **Profile providers / validators**: el `IProfileValidator` se invoca antes
  del Layout en ambas modalidades.
- **Transports**: son agnósticos al modo (sólo ven `byte[]`).

El único punto de extensión que **no aplica** al modo integrado es el
`IDataValidator`, ya que no hay diccionario de datos que validar.

---

## 19. Relación con otros documentos

- `arquitectura-solucion_v1.1.md`
- `contratos-del-motor_v1.0.md`
- `modelo-datos-logico_v1.0.md`
- `flujo-ejecucion-motor_v1.0.md`
- `guia-uso-libreria_v1.1.md`
- `docs/10_developer_guide/transports-y-extensibilidad.md` — guía paso a paso.
- `docs/10_developer_guide/componentes-ux-maui.md`
- RC-04, RC-05, RC-06

---

## 20. Historial de versiones

| Versión | Fecha      | Autor          | Cambios                                                                |
| ------- | ---------- | -------------- | ---------------------------------------------------------------------- |
| 1.0     | 2026-03-28 | Equipo Técnico | Definición inicial.                                                    |
| 1.1     | 2026-05-06 | Equipo Técnico | Extension point `IThermalPrinterTransport`, `MauiPrintErrorHandler`, renderers MAUI. |

---
