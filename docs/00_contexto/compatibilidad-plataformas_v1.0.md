# Compatibilidad de Plataformas

**Proyecto:** Motor de Documentos e Impresión basado en DSL
**Documento:** compatibilidad-plataformas_v1.0.md
**Versión:** 1.0
**Estado:** Histórico
**Fecha:** 2026-03-30

> **⚠️ Documento histórico — versión 1.0.** Para la versión actual con los nuevos paquetes (`MotorDsl.Printing.Abstractions`, `MotorDsl.Bluetooth`, `MotorDsl.Maui`) ver [compatibilidad-plataformas_v1.1.md](./compatibilidad-plataformas_v1.1.md).

---

## 1. Resumen

La librería core del motor DSL es **multiplataforma** y funciona en cualquier entorno compatible con .NET Standard / .NET 10. Sin embargo, la conectividad Bluetooth para impresoras térmicas ESC/POS solo está disponible en **Android**.

Este documento detalla la compatibilidad de cada componente por plataforma, explica las limitaciones técnicas de iOS y describe las alternativas disponibles para desarrolladores que necesiten soporte multiplataforma.

---

## 2. Matriz de compatibilidad

| Componente | Android | iOS | Windows | Notas |
|---|---|---|---|---|
| **MotorDsl.Core** (parser, evaluator, layout engine) | ✅ | ✅ | ✅ | Sin dependencias de plataforma |
| **MotorDsl.Parser** (JSON DSL → DocumentTemplate) | ✅ | ✅ | ✅ | Usa System.Text.Json |
| **MotorDsl.Rendering** (TextRenderer, EscPosRenderer) | ✅ | ✅ | ✅ | Genera bytes ESC/POS en cualquier plataforma |
| **MotorDsl.Extensions** (DI, fluent API) | ✅ | ✅ | ✅ | Microsoft.Extensions.DependencyInjection |
| **ThermalPrinterService** (Bluetooth SPP) | ✅ | ❌ | ❌ | Requiere Bluetooth clásico (SPP) |
| **SkiaSharp** (rasterización de imágenes) | ✅ | ✅ | ✅ | NuGet multiplataforma |
| **QuestPDF** (generación PDF) | ✅ | ✅ | ✅ | NuGet multiplataforma |

### Resumen por plataforma

* **Android:** Soporte completo — pipeline DSL + impresión Bluetooth + PDF + imágenes.
* **iOS:** Pipeline DSL + PDF + imágenes. Sin impresión Bluetooth directa a térmicas (ver sección 4).
* **Windows:** Pipeline DSL + PDF + imágenes. Sin Bluetooth (posible vía USB/WiFi con implementación custom).

---

## 3. Librería core — Multiplataforma

La librería core (`MotorDsl.Core`, `MotorDsl.Parser`, `MotorDsl.Rendering`, `MotorDsl.Extensions`) no tiene dependencias de plataforma. Todo el pipeline de procesamiento funciona en cualquier entorno .NET:

1. **Parseo** de plantillas JSON DSL → `DocumentTemplate`
2. **Evaluación** de bindings y condiciones → `EvaluatedDocument`
3. **Layout** según perfil de dispositivo → `LayoutedDocument`
4. **Renderizado** a texto plano o bytes ESC/POS → `RenderResult`

Un desarrollador puede usar la librería en iOS para generar documentos y enviarlos por un transporte alternativo (WiFi, API REST, etc.), o generar PDFs con un renderer custom como `PdfRenderer`.

---

## 4. iOS — Limitaciones de Bluetooth

### El problema

Apple restringe el acceso a **Bluetooth clásico** (perfil SPP — Serial Port Profile) desde aplicaciones de terceros. Las impresoras térmicas ESC/POS estándar utilizan SPP para la comunicación serie.

iOS solo expone **Bluetooth Low Energy (BLE)** a través del framework CoreBluetooth. La mayoría de impresoras térmicas económicas (58mm y 80mm) no soportan BLE.

Esto **no** es una limitación de .NET MAUI ni de la librería — es una restricción del sistema operativo iOS impuesta por Apple.

### Impacto concreto

* `ThermalPrinterService` usa `Android.Bluetooth.BluetoothSocket` (API nativa Android) → no existe equivalente en iOS para Bluetooth clásico.
* Las apps de ejemplo (`MotorDsl.SampleApp`, `MotorDsl.MultaApp`) son Android-only para la funcionalidad de impresión.
* La generación de documentos (texto, ESC/POS bytes, PDF) funciona sin problemas en iOS.

---

## 5. Alternativas para iOS

| Alternativa | Protocolo | Complejidad | Costo |
|---|---|---|---|
| **Impresoras WiFi** | TCP socket | Media | Bajo — muchas térmicas tienen WiFi |
| **AirPrint** | AirPrint (nativo iOS) | Baja | Bajo — usa PDF del renderer |
| **Hardware MFi** | Bluetooth clásico (certificado Apple) | Alta | Alto — impresoras MFi son más caras |
| **BLE (impresoras compatibles)** | Bluetooth Low Energy | Alta | Medio — solo modelos recientes |
| **API REST / servidor de impresión** | HTTP | Media | Bajo — requiere servidor intermedio |

### Detalle de cada alternativa

#### Impresoras WiFi

Algunas impresoras térmicas (ej. Epson TM-T20III, Star TSP100) soportan conexión WiFi. El motor genera los mismos bytes ESC/POS; solo cambia el transporte:

```csharp
// Ejemplo conceptual — enviar ESC/POS por TCP
using var client = new TcpClient();
await client.ConnectAsync("192.168.1.100", 9100);
await client.GetStream().WriteAsync(escposBytes);
```

#### AirPrint vía PDF

Usar `PdfRenderer` para generar un PDF y enviarlo a una impresora AirPrint nativa de iOS. No usa ESC/POS — el documento se imprime como PDF estándar.

#### Hardware MFi (Made for iPhone)

Impresoras certificadas bajo el programa MFi de Apple permiten comunicación Bluetooth clásica desde iOS. Marcas como Star Micronics ofrecen modelos MFi. Requieren SDK propietario del fabricante.

#### BLE (Bluetooth Low Energy)

Algunas impresoras modernas soportan BLE además de SPP. Requiere implementar un `IThermalPrinterService` custom que use `CoreBluetooth` en iOS. El soporte BLE varía mucho entre marcas y modelos.

---

## 6. Qué necesita un desarrollador para portar a iOS

Si un equipo necesita soporte iOS con impresión, estos son los pasos:

1. **Elegir transporte:** WiFi (TCP socket), AirPrint (PDF), MFi (SDK fabricante) o BLE (CoreBluetooth).
2. **Implementar `IThermalPrinterService`** para la plataforma elegida. La interfaz es la misma:
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
3. **Registrar la implementación iOS** en DI con platform-specific code:
   ```csharp
   #if IOS
   builder.Services.AddSingleton<IThermalPrinterService, WiFiPrinterService>();
   #elif ANDROID
   builder.Services.AddSingleton<IThermalPrinterService, ThermalPrinterService>();
   #endif
   ```
4. **La librería core no cambia.** Todo el pipeline DSL → ESC/POS funciona exactamente igual. Solo cambia cómo se envían los bytes al dispositivo.

---

## 7. Protocolos de conexión por plataforma

| Protocolo | Android | iOS | Windows | Impresoras típicas |
|---|---|---|---|---|
| **Bluetooth clásico (SPP)** | ✅ Nativo | ❌ Bloqueado por Apple | ⚠️ Con stack BT | Mayoría de térmicas 58mm/80mm |
| **Bluetooth Low Energy (BLE)** | ✅ | ✅ CoreBluetooth | ✅ | Modelos recientes/premium |
| **WiFi (TCP socket)** | ✅ | ✅ | ✅ | Epson TM-T20III, Star TSP100 |
| **USB** | ⚠️ OTG | ❌ | ✅ | Modelos de escritorio |
| **AirPrint** | ❌ | ✅ Nativo | ❌ | Impresoras de red compatibles |

---

## 8. Estado de implementación iOS en apps de ejemplo

### Sprint 08 — Soporte iOS añadido (TK-iOS)

| Elemento | Estado | Detalle |
|---|---|---|
| `TargetFrameworks` MultaApp | ✅ | `net10.0-android;net10.0-ios` |
| `TargetFrameworks` SampleApp | ✅ | `net10.0-android;net10.0-ios` |
| `Platforms/iOS/Info.plist` (ambas apps) | ✅ | Creado con orientaciones y UIDeviceFamily |
| `Platforms/iOS/AppDelegate.cs` (ambas apps) | ✅ | `MauiUIApplicationDelegate` + `CreateMauiApp()` |
| `Platforms/iOS/Program.cs` (ambas apps) | ✅ | `UIApplication.Main` entry point |
| `ThermalPrinterService.SendBytesAsync` | ✅ | `#if IOS` muestra `DisplayAlert` y retorna |
| `ThermalPrinterService.ShowIosNotSupportedAsync()` | ✅ | Helper que notifica al usuario |
| `MainPage.xaml` MultaApp — Frame BT | ✅ | `IsVisible="{OnPlatform Android=True, iOS=False}"` |
| `MainPage.xaml` MultaApp — BtnImprimir | ✅ | `IsVisible="{OnPlatform Android=True, iOS=False}"` |
| `MainPage.xaml` MultaApp — aviso iOS | ✅ | Frame naranja explicativo visible solo en iOS |
| `MainPage.xaml` SampleApp — ScanButton | ✅ | `IsVisible="{OnPlatform Android=True, iOS=False}"` |
| `MainPage.xaml` SampleApp — sección Conectar | ✅ | Grid + Label ocultados en iOS |
| `MainPage.xaml` SampleApp — PrintButton | ✅ | `IsVisible="{OnPlatform Android=True, iOS=False}"` |
| `MainPage.xaml.cs` MultaApp — `OnAppearing` | ✅ | `#elif IOS` evita llamar a `AutoConnectBluetoothAsync()` |
| Build Android (0 errores) | ✅ | Verificado con `dotnet build -f net10.0-android` |
| 185 tests unitarios | ✅ | Sin regresiones (`dotnet test`) |

> **Nota:** La compilación para `net10.0-ios` requiere macOS con Xcode instalado. En Windows solo puede validarse la estructura del proyecto; el build completo se ejecuta en un agente macOS de CI/CD.

---

## 9. Control de cambios

| Versión | Fecha      | Descripción |
|---------|------------|-------------|
| 1.0     | 2026-03-30 | Versión inicial — documentación de compatibilidad iOS/Android/Windows |
| 1.1     | 2026-04-01 | Sprint 08 — implementación de soporte iOS en SampleApp y MultaApp |

---

**Fin del documento**
