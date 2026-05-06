# Arquitectura de la Solución — Motor de Renderizado DSL
**Archivo:** arquitectura-solucion_v1.1.md
**Versión:** 1.1
**Fecha:** 2026-05-06
**Autor:** Equipo Técnico
**Estado:** Vigente

---

## Cambios desde v1.0

La versión 1.1 incorpora la separación de la capa de impresión y la introducción
de un orquestador agnóstico de transporte. Los principales cambios respecto a la
v1.0 son:

- **Tres paquetes nuevos** se suman al stack publicado:
  - `MotorDsl.Printing.Abstractions` — contratos de transporte y orquestador.
  - `MotorDsl.Bluetooth` — implementación del transport BT Classic SPP (Android).
  - `MotorDsl.Maui` — controles MAUI, renderers de UI/PDF/raster y error handler.
- **Nuevo contrato `IThermalPrinterService`** (en `MotorDsl.Printing.Abstractions`)
  que reemplaza al servicio que vivía en cada sample. Implementa
  `INotifyPropertyChanged`, expone `ConnectionState`, `CurrentDevice`, `LastError`
  y eventos `ErrorOccurred` / `DevicesDiscovered`.
- **Nuevo extension point `IThermalPrinterTransport`**: el orquestador delega el
  acceso al hardware. Se pueden registrar varios transports (BT, USB, Red, BLE)
  y el servicio enruta cada `PrinterDevice` a su `Kind` correspondiente.
- **Nuevos componentes MAUI bindeables** (`PrinterStatusBadge`, `PrinterPickerView`,
  `PrinterPickerPage`, `MauiRasterPreview`, `MauiDocumentPreview`).
- **Nuevos renderers MAUI**: `RasterPreviewRenderer` (PNG con texto monoespaciado
  y QR) y `BitmapEscPosRenderer` (existente, ahora dentro de `MotorDsl.Maui`).
  `PdfRenderer` agrega QR vía `QrCodeRasterizer`, fallback de barcode-text,
  word-wrap y page-break automático.
- **`MauiPrintErrorHandler`** extiende `DefaultPrintErrorHandler` con eventos
  `RetryAttempted` / `Succeeded` para feedback de UI sin bloquear el retry loop.
- **Fluent DI**: `AddMotorDslMaui()` extiende `MotorDslBuilder` y registra
  rasterizers, renderers y orquestador. `AddBluetoothPrinterTransport()` se aplica
  sobre `IServiceCollection`.

---

## 1. Propósito

Este documento describe la arquitectura técnica del Motor de Renderizado DSL en
su versión 1.1. Define los componentes principales, sus responsabilidades y
cómo interactúan para transformar plantillas DSL y datos en documentos
renderizados (texto, ESC/POS, ESC/POS-bitmap, PDF, raster preview) y enviarlos a
una impresora térmica a través de un transport extensible.

El objetivo es establecer una base arquitectónica modular, extensible y
desacoplada que permita evolucionar el motor sin afectar los renderizadores ni
la capa de transporte.

---

## 2. Alcance

La arquitectura cubre:

- Motor de parsing de DSL (`MotorDsl.Parser`)
- Construcción del modelo abstracto del documento (`MotorDsl.Core`)
- Motor de evaluación y motor de layout (`MotorDsl.Core`)
- Renderizadores texto/ESC/POS básicos (`MotorDsl.Rendering`)
- Renderizadores MAUI: PDF, ESC/POS bitmap, raster preview (`MotorDsl.Maui`)
- Capa de impresión: contratos, orquestador, retry exponencial
  (`MotorDsl.Printing.Abstractions`)
- Transport físico Bluetooth Classic SPP (`MotorDsl.Bluetooth`)
- Componentes MAUI UX bindeables al servicio (`MotorDsl.Maui.Controls`)
- Extensibilidad: renderers custom, transports custom (USB / Red / BLE)

No incluye detalles de infraestructura de despliegue ni configuración de
servidores.

---

## 3. Estilo arquitectónico

Se mantiene la **arquitectura modular basada en capas y pipeline de
procesamiento**, separando:

- Entrada (DSL + datos)
- Procesamiento (parsing, validación, evaluación, layout)
- Modelo abstracto intermedio (`LayoutedDocument`)
- Renderización (múltiples targets)
- **Capa de impresión** (orquestador + transports — nueva en v1.1)

Principios clave:

- Alta cohesión por módulo
- Bajo acoplamiento entre componentes
- Independencia del renderizador y del transport
- Extensibilidad mediante plugins (renderers, transports)
- Pipeline desacoplado

---

## 4. Visión de alto nivel

```text
[Plantilla DSL + Datos]      [Documento Integrado]
   (format: "template")        (format: "integrated")
            │                          │
            ▼                          ▼
                  [Parser DSL]
                         ▼
                  [Modelo Abstracto (AST)]
            │                          │
            ▼                          ▼
        [Evaluator]              (skip Evaluator —
   (resuelve {{}}, loops,         AST ya resuelto)
   conditionals)
            └──────────┬──────────────┘
                       ▼
                  [Motor de Layout]
                       ▼
            ┌──────────┼──────────┬──────────┐
            ▼          ▼          ▼          ▼
        [ESC/POS]    [Text]   [Bitmap     [Raster
                              ESC/POS]    Preview]
            │                                  │
            ▼                                  ▼
                                          [PDF]
            └─────────┬────────────────────────┘
                      ▼
            [IThermalPrinterService]   ← orquestador
                      ▼
            ┌─────────┼────────────┐
            ▼         ▼            ▼
       [BT SPP]    [USB*]      [Red TCP*]   ← IThermalPrinterTransport
            ▼
       Impresora física
```

`*` = transports planificados, no implementados aún.

---

## 5. Componentes principales

### 5.1 Cargador de Plantillas DSL

**Responsabilidad:** obtener plantillas, versionar y cachear.

### 5.2 Validador DSL

**Responsabilidad:** validar estructura sintáctica y reglas (RN-02).

### 5.3 Parser DSL — `MotorDsl.Parser`

**Responsabilidad:** transformar JSON DSL en `DocumentTemplate`.

### 5.4 Modelo Abstracto del Documento — `MotorDsl.Core`

**Responsabilidad:** representar el documento independiente del renderizador
(`DocumentTemplate`, `EvaluatedDocument`, `LayoutedDocument`).

### 5.5 Resolver de Datos / Evaluator — `MotorDsl.Core`

**Responsabilidad:** resolver bindings `{{}}`, loops y conditionals. Se omite en
modo integrado.

### 5.6 Motor de Layout — `MotorDsl.Core`

**Responsabilidad:** producir `LayoutedDocument` aplicando alineación, wrap y
metadatos (QR, barcode, bitmap) según `DeviceProfile` y capabilities.

### 5.7 Renderizadores

| Renderer | Paquete | Target | Salida |
|---|---|---|---|
| TextRenderer | MotorDsl.Rendering | `text` | `string` |
| EscPosRenderer | MotorDsl.Rendering | `escpos` | `byte[]` ESC/POS |
| BitmapEscPosRenderer | MotorDsl.Maui | `escpos-bitmap` | `byte[]` GS v 0 |
| PdfRenderer | MotorDsl.Maui | `pdf` | `byte[]` PDF |
| RasterPreviewRenderer | MotorDsl.Maui | `raster-preview` | `byte[]` PNG |

### 5.8 Gestor de Perfiles de Dispositivo — `MotorDsl.Core`

**Responsabilidad:** definir capabilities (`Width`, `bitmap_max_width_px`,
`supports_bitmap`, etc.) que dirigen el layout y el renderer.

### 5.9 Capa de impresión y transporte (NUEVA en v1.1)

#### 5.9.1 `IThermalPrinterService` (orquestador agnóstico)

Vive en `MotorDsl.Printing.Abstractions`. Implementación: `ThermalPrinterService`.

**Responsabilidad:**

- Mantener el ciclo de vida de la conexión (`Disconnected`, `Scanning`,
  `Connecting`, `Connected`, `Reconnecting`, `Failed`).
- Delegar el descubrimiento, la conexión y la escritura física a uno de los
  `IThermalPrinterTransport` registrados, eligiéndolo por `PrinterDevice.Kind`.
- Aplicar **retry exponencial** con `PrintRetryOptions` y delegar la decisión
  de reintentar a `IPrintErrorHandler`.
- Notificar cambios mediante `INotifyPropertyChanged` + eventos
  `ErrorOccurred` / `DevicesDiscovered`.

```csharp
public interface IThermalPrinterService : INotifyPropertyChanged
{
    bool IsConnected { get; }
    PrinterDevice? CurrentDevice { get; }
    PrinterConnectionState ConnectionState { get; }
    string? LastError { get; }

    event EventHandler<PrintErrorEventArgs>? ErrorOccurred;
    event EventHandler<DevicesDiscoveredEventArgs>? DevicesDiscovered;

    Task<IReadOnlyList<PrinterDevice>> DiscoverDevicesAsync(
        string? kind = null, CancellationToken ct = default);
    Task<bool> ConnectAsync(PrinterDevice device, CancellationToken ct = default);
    Task DisconnectAsync();
    Task<bool> ReconnectAsync(CancellationToken ct = default);
    Task SendBytesAsync(byte[] data, PrinterProfile? profile = null,
        PrintRetryOptions? retry = null, CancellationToken ct = default);
}
```

#### 5.9.2 `IThermalPrinterTransport` (extension point físico)

Implementación concreta del medio físico. Cada transport declara un `Kind`
(`"bluetooth"`, `"usb"`, `"wifi"`, ...) y maneja sólo su propio canal.

```csharp
public interface IThermalPrinterTransport
{
    string Kind { get; }
    bool IsConnected { get; }
    PrinterDevice? CurrentDevice { get; }
    Task<IReadOnlyList<PrinterDevice>> DiscoverAsync(CancellationToken ct = default);
    Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default);
    Task DisconnectAsync();
    Task WriteBytesAsync(byte[] data, PrinterProfile profile, CancellationToken ct = default);
}
```

#### 5.9.3 `BluetoothPrinterTransport` — `MotorDsl.Bluetooth`

Implementación Android Classic SPP (UUID `00001101-...`). En iOS lanza
`PlatformNotSupportedException`. Solo escribe bytes; el retry y la orquestación
viven en el servicio.

#### 5.9.4 `IPrintErrorHandler` y `MauiPrintErrorHandler`

Decide si un error es transitorio (retry) o terminal (abort). El handler MAUI
extiende el `DefaultPrintErrorHandler` con logging y eventos para feedback de
UI (`RetryAttempted`, `Succeeded`).

### 5.10 Componentes MAUI UX (NUEVA en v1.1)

Viven en `MotorDsl.Maui.Controls`. Todos se bindean a un `IThermalPrinterService`
y reflejan su estado automáticamente vía `INotifyPropertyChanged`.

| Componente | Tipo | Rol |
|---|---|---|
| `PrinterStatusBadge` | `ContentView` | Badge con color + texto que refleja `ConnectionState` y `CurrentDevice`. |
| `PrinterPickerView` | `ContentView` | Botón Escanear + `CollectionView` de `PrinterDevice`. Al tocar uno, llama `ConnectAsync`. Soporta `FilterKind` y `AutoConnectIfSingle`. |
| `PrinterPickerPage` | `ContentPage` | Página modal que envuelve el picker con un Cancelar. |
| `MauiDocumentPreview` | `ContentView` | Preview tipográfico de un `LayoutedDocument` (alineación, bold, QR/barcode/bitmap como placeholders). |
| `MauiRasterPreview` | `ContentView` | Muestra el PNG producido por `RasterPreviewRenderer` con `ZoomFactor` para vista pixelada. |

Estos controles eliminan la necesidad de que cada sample reimplemente UI de
escaneo/conexión/preview. Un consumidor sólo bindea
`muic:PrinterStatusBadge Service="{Binding Printer}"`.

### 5.11 Orquestador del Motor

`IDocumentEngine` coordina el pipeline DSL (Parse → Evaluate → Layout → Render)
y devuelve un `RenderResult`. **No** conoce la impresora: el envío al hardware
lo realiza la app consumidora a través de `IThermalPrinterService`.

---

## 6. Flujo principal — Renderización + Impresión

```text
Entrada:
  - JSON DSL (template o integrated)
  - Datos (sólo modo template)
  - DeviceProfile

Pipeline DSL (síncrono):
  Parser → [Evaluator] → LayoutEngine → Renderer  → RenderResult.Output (byte[] o string)

Pipeline de impresión (asíncrono):
  IThermalPrinterService.DiscoverDevicesAsync(kind)
            ↓
  IThermalPrinterService.ConnectAsync(device)
            ↓
  IThermalPrinterService.SendBytesAsync(bytes, profile, retry)
            ↓
  IThermalPrinterTransport.WriteBytesAsync(...)   ← BT / USB / Red
```

---

## 7. Decisiones técnicas clave

| Decisión | Elección | Motivo |
|---|---|---|
| Arquitectura | Modular + Pipeline | Extensibilidad |
| Modelo intermedio | `LayoutedDocument` | Independencia del render |
| DSL | Declarativo JSON | Flexibilidad |
| Renderizadores | Plug-in basados en `Target` | Escalabilidad |
| Layout engine | Separado | Reutilización |
| Perfil dispositivo | Capabilities map | Adaptabilidad |
| **Capa de impresión** | **Orquestador + transports (`Kind`)** | **Multi-transport sin tocar el core** |
| **Notificación a UI** | **`INotifyPropertyChanged` + eventos** | **Bindings MAUI directos** |
| **Retry de envío** | **Exponencial con `IPrintErrorHandler`** | **Resiliencia ante errores BT** |

---

## 8. Consideraciones de calidad

### Extensibilidad

- Nuevos renderizadores se agregan vía `MotorDslBuilder.AddRenderer<T>()`.
- **Nuevos transports se agregan vía DI** (`AddSingleton<IThermalPrinterTransport, MyTransport>()`)
  sin tocar el orquestador.
- Componentes MAUI son `ContentView`/`ContentPage` puros, reusables.

### Mantenibilidad

- Separación estricta: pipeline DSL ↔ pipeline de impresión.
- Validaciones centralizadas (`TemplateValidator`, `DataValidator`,
  `ProfileValidator`).

### Escalabilidad

- Pipeline DSL stateless.
- Service singleton tolera múltiples llamadas concurrentes (DiscoverDevices /
  Send) gestionadas por estado interno.

### Observabilidad

- `MauiPrintErrorHandler` loguea cada attempt vía `Debug.WriteLine`.
- Eventos `ErrorOccurred` / `DevicesDiscovered` permiten reportería externa.

---

## 9. Riesgos conocidos

- iOS no soporta BT Classic SPP — si la app necesita imprimir en iOS, debe
  registrar otro transport (BLE, WiFi, AirPrint).
- BT Classic depende de `BondedDevices` previo (pairing manual del usuario).
- Permisos runtime Android 12+ (`BLUETOOTH_CONNECT`, `BLUETOOTH_SCAN`)
  obligatorios.
- `RasterPreviewRenderer` requiere SkiaSharp; aumenta el tamaño de la app.

---

## 10. Evolución prevista

- Transport USB (Android OTG / Windows).
- Transport Red TCP (raw socket port 9100).
- Transport BLE (CoreBluetooth iOS, Android BLE).
- Renderer raster con nearest-neighbor real (`SKCanvasView` interactivo).
- Cache de modelo abstracto.
- Editor visual de plantillas.

---

## 11. Relación con otros documentos

- `extensibilidad-motor_v1.1.md` — guía de transports / renderers custom.
- `guia-uso-libreria_v1.1.md` — uso desde aplicaciones .NET.
- `compatibilidad-plataformas_v1.1.md` — matriz Android / iOS / Windows.
- `docs/10_developer_guide/guia-integracion-maui.md` — paso a paso MAUI.
- `docs/10_developer_guide/componentes-ux-maui.md` — referencia de los controles.
- `docs/10_developer_guide/transports-y-extensibilidad.md` — escribir un
  transport nuevo.

---

## 12. Historial de versiones

| Versión | Fecha      | Autor          | Cambios                                                       |
| ------- | ---------- | -------------- | ------------------------------------------------------------- |
| 1.0     | 2026-03-28 | Equipo Técnico | Versión inicial de arquitectura.                              |
| 1.1     | 2026-05-06 | Equipo Técnico | Capa de impresión, transports, controles MAUI, renderers MAUI.|

---
