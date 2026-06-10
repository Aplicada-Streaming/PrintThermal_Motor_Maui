# Compatibilidad de Plataformas

**Proyecto:** Motor de Documentos e Impresión basado en DSL
**Documento:** compatibilidad-plataformas_v1.1.md
**Versión:** 1.1
**Estado:** Vigente
**Fecha:** 2026-05-06

---

## Cambios desde v1.0

- Se reorganizó la matriz reflejando los **7 paquetes publicados** (no 4).
- Se eliminó la antigua `ThermalPrinterService` (Bluetooth ad-hoc por sample) y
  se documenta el nuevo modelo: orquestador agnóstico + transports.
- iOS deja de necesitar implementaciones ad-hoc en cada sample: ahora se trata
  con el extension point oficial `IThermalPrinterTransport` y la posibilidad
  de registrar transports BLE / WiFi / AirPrint.
- `BluetoothPrinterTransport` lanza `PlatformNotSupportedException` en iOS de
  forma explícita, en lugar de fallar silenciosamente.
- Sección 6 (portar a iOS) reescrita para reflejar el nuevo flujo
  (registrar `IThermalPrinterTransport` específico de iOS).

---

## 1. Resumen

La librería core del Motor DSL es **multiplataforma** y funciona en cualquier
entorno compatible con .NET 10. Sin embargo, la conectividad Bluetooth Classic
SPP para impresoras térmicas ESC/POS solo está disponible en **Android**.

Este documento detalla la compatibilidad de cada uno de los 7 paquetes por
plataforma, explica las limitaciones técnicas de iOS y describe las alternativas
disponibles para soporte multiplataforma.

---

## 2. Matriz de compatibilidad por paquete

| Paquete | Android | iOS | Windows / Linux / macOS | Notas |
|---|---|---|---|---|
| **MotorDsl.Core** | ✅ | ✅ | ✅ | TFM `net10.0`. Sin dependencias de plataforma. |
| **MotorDsl.Parser** | ✅ | ✅ | ✅ | Usa System.Text.Json. |
| **MotorDsl.Rendering** | ✅ | ✅ | ✅ | TextRenderer + EscPosRenderer. |
| **MotorDsl.Extensions** | ✅ | ✅ | ✅ | DI fluent (`AddMotorDslEngine`, `AddRenderer`). |
| **MotorDsl.Printing.Abstractions** | ✅ | ✅ | ✅ | Contratos puros + orquestador. Independiente de MAUI y plataforma. |
| **MotorDsl.Bluetooth** | ✅ Classic SPP | ❌ Lanza `PlatformNotSupportedException` | ❌ | TFMs `net10.0-android;net10.0-ios`. iOS está como TFM por compilación, pero el código tira excepción explícita. |
| **MotorDsl.Maui** | ✅ | ✅ (sin BT) | ❌ MAUI no soporta desktop nativo aquí | Renderers (PDF, ESC/POS bitmap, raster), controles, error handler. |

### Resumen por plataforma

- **Android:** Soporte completo del stack (DSL + render + impresión BT + PDF + raster).
- **iOS:** DSL + render + PDF + raster + UI MAUI. **Sin impresión Bluetooth Classic** (ver sección 4). Es necesario registrar otro `IThermalPrinterTransport` para imprimir.
- **Windows / Linux / macOS (apps no MAUI):** DSL + render + PDF posibles. Para impresión hay que registrar un transport nativo (USB / Red TCP) — no incluido por el momento.

---

## 3. Núcleo y librerías agnósticas — Multiplataforma

Los paquetes `MotorDsl.Core`, `MotorDsl.Parser`, `MotorDsl.Rendering`,
`MotorDsl.Extensions` y `MotorDsl.Printing.Abstractions` no tienen dependencias
de plataforma. Todo el pipeline funciona en cualquier entorno .NET 10:

1. **Parseo** de plantillas JSON DSL → `DocumentTemplate`.
2. **Evaluación** de bindings y condiciones → `EvaluatedDocument`.
3. **Layout** según perfil de dispositivo → `LayoutedDocument`.
4. **Renderizado** a texto, ESC/POS o (con `MotorDsl.Maui`) PDF / raster / bitmap.
5. **Orquestación de impresión** (`IThermalPrinterService` + transports
   registrados).

---

## 4. iOS — Limitaciones de Bluetooth Classic

### El problema

Apple restringe el acceso a **Bluetooth clásico** (perfil SPP — Serial Port
Profile) desde aplicaciones de terceros. Las impresoras térmicas ESC/POS
estándar utilizan SPP para la comunicación serie.

iOS solo expone **Bluetooth Low Energy (BLE)** a través del framework
CoreBluetooth. La mayoría de impresoras térmicas económicas (58 mm y 80 mm) no
soportan BLE.

Esto **no** es una limitación de .NET MAUI ni de la librería — es una
restricción del sistema operativo iOS impuesta por Apple.

### Comportamiento concreto en `MotorDsl.Bluetooth`

- En Android: `BluetoothPrinterTransport` usa `Android.Bluetooth.BluetoothSocket`
  + UUID SPP (`00001101-0000-1000-8000-00805F9B34FB`).
- En iOS: cada método (`DiscoverAsync`, `ConnectAsync`, `WriteBytesAsync`,
  `DisconnectAsync`) lanza `PlatformNotSupportedException` con mensaje
  *"iOS no soporta Bluetooth Classic SPP. Usar BLE o impresion por red."*

### Impacto

- Una app MAUI multiplataforma puede registrar `AddBluetoothPrinterTransport`
  sin romper iOS, pero al intentar `DiscoverDevicesAsync(kind: "bluetooth")` o
  `ConnectAsync(...)` el orquestador propaga la excepción y la UI debe
  manejarla (ej.: mostrando un aviso *"Impresión Bluetooth no disponible en iOS"*).
- La generación de documentos (texto, ESC/POS bytes, PDF, raster preview)
  funciona sin problemas en iOS.

---

## 5. Alternativas para iOS

| Alternativa | Protocolo | Complejidad | Costo | Cómo se integra |
|---|---|---|---|---|
| **Impresoras WiFi** | TCP socket port 9100 | Media | Bajo | Implementar `IThermalPrinterTransport` con `TcpClient`. |
| **AirPrint** | AirPrint nativo | Baja | Bajo | Renderer PDF + `Launcher.OpenAsync`. |
| **Hardware MFi** | BT Classic certificado | Alta | Alto | SDK propietario del fabricante envuelto en `IThermalPrinterTransport`. |
| **BLE** | Bluetooth Low Energy | Alta | Medio | `IThermalPrinterTransport` con `CoreBluetooth` (Plugin.BLE u otro). |
| **API REST / servidor** | HTTP | Media | Bajo | Render local + POST de bytes a un servidor que imprime. |

### Detalle de cada alternativa

#### 5.1 Impresoras WiFi (recomendado para iOS)

Algunas impresoras térmicas (Epson TM-T20III, Star TSP100, etc.) soportan WiFi.
El motor genera los mismos bytes ESC/POS; solo cambia el transport.

```csharp
public class TcpPrinterTransport : IThermalPrinterTransport
{
    public string Kind => "wifi";
    // DiscoverAsync escanea subred / mDNS; ConnectAsync abre TcpClient;
    // WriteBytesAsync escribe en el NetworkStream
}

builder.Services.AddSingleton<IThermalPrinterTransport, TcpPrinterTransport>();
```

#### 5.2 AirPrint vía PDF

Usar el `PdfRenderer` de `MotorDsl.Maui` para generar un PDF y lanzarlo con
`Launcher.OpenAsync` — el sistema operativo gestiona AirPrint de forma nativa.

#### 5.3 BLE (impresoras compatibles)

Implementar un `IThermalPrinterTransport` con `Kind="ble"` que utilice
`CoreBluetooth` (a través de Plugin.BLE u otra abstracción multiplataforma).

---

## 6. Qué necesita un desarrollador para portar a iOS

Si un equipo necesita soporte iOS con impresión, estos son los pasos:

1. **Elegir transport:** WiFi (TCP socket), AirPrint (PDF), MFi (SDK
   fabricante) o BLE (CoreBluetooth).
2. **Implementar `IThermalPrinterTransport`** para la plataforma elegida.
   El contrato es:
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
3. **Registrar la implementación iOS** en DI con compilación condicional:
   ```csharp
   #if IOS
       builder.Services.AddSingleton<IThermalPrinterTransport, TcpPrinterTransport>();
   #elif ANDROID
       builder.Services.AddBluetoothPrinterTransport();
   #endif
   ```
4. **La librería core no cambia.** El orquestador `IThermalPrinterService`
   detecta el transport por `device.Kind` automáticamente. Solo cambia cómo se
   envían los bytes al dispositivo.

---

## 7. Protocolos de conexión por plataforma

| Protocolo | Android | iOS | Windows | Impresoras típicas |
|---|---|---|---|---|
| **Bluetooth clásico (SPP)** | ✅ Nativo (paquete `MotorDsl.Bluetooth`) | ❌ Bloqueado por Apple | ⚠️ Posible vía SerialPort | Mayoría de térmicas 58mm/80mm |
| **Bluetooth Low Energy (BLE)** | ✅ Custom transport | ✅ Custom transport (CoreBluetooth) | ✅ | Modelos recientes/premium |
| **WiFi (TCP socket)** | ✅ Custom transport | ✅ Custom transport | ✅ | Epson TM-T20III, Star TSP100 |
| **USB** | ⚠️ OTG (custom transport) | ❌ | ✅ Custom transport | Modelos de escritorio |
| **AirPrint** | ❌ | ✅ Nativo (vía PDF) | ❌ | Impresoras de red compatibles |

---

## 8. Estado de implementación de transports

| Transport | Estado | Paquete |
|---|---|---|
| Bluetooth Classic SPP (Android) | ✅ Implementado | `MotorDsl.Bluetooth` |
| Bluetooth Classic SPP (iOS) | ❌ Imposible (restricción del SO) | — |
| BLE | ⏳ Planificado | — |
| USB | ⏳ Planificado | — |
| Red TCP (puerto 9100) | ⏳ Planificado | — |
| AirPrint | ✅ Disponible vía `PdfRenderer` + `Launcher` | `MotorDsl.Maui` |

---

## 9. Compatibilidad de los samples

| Sample | Android | iOS |
|---|---|---|
| MotorDsl.SampleApp | ✅ Render (servicio BT local/stub, sin transport BT del paquete) | ✅ Render (BT inactivo) |
| MotorDsl.MultaApp | ✅ Render (servicio BT local/stub, sin transport BT del paquete) | ✅ Render (BT inactivo) |
| MotorDsl.Integrated.MultaApp | ✅ Render (servicio BT local/stub, sin transport BT del paquete) | ✅ Render (BT inactivo) |
| MotorDsl.Nuget.MultaApp | ✅ Render + BT (transport BT registrado vía `AddBluetoothPrinterTransport`) | ✅ Render (BT inactivo) |
| MotorDsl.Nuget.Integrated.MultaApp | ✅ Render + BT (transport BT registrado vía `AddBluetoothPrinterTransport`) | ✅ Render (BT inactivo) |

> **Patrón recomendado** para ocultar la UI de Bluetooth en iOS: aplicar
> `IsVisible="{OnPlatform Android=True, iOS=False}"` a los controles de Bluetooth
> y mostrar un aviso explicativo en iOS. Los controles `muic:*`
> (`PrinterStatusBadge` / `PrinterPickerView` / `MauiRasterPreview`) solo aparecen
> en las 2 apps `Nuget.*`.

---

## 10. Control de cambios

| Versión | Fecha      | Descripción |
|---------|------------|-------------|
| 1.0     | 2026-03-30 | Versión inicial — documentación de compatibilidad iOS/Android/Windows. |
| 1.1     | 2026-05-06 | Reorganización post-refactor: 7 paquetes, transports como extension point, iOS lanza `PlatformNotSupportedException` explícita. |

---

**Fin del documento**
