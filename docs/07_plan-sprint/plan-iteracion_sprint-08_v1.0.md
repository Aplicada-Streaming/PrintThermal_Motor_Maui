# 📄 Plan de Iteración — Sprint 08
**Archivo:** plan-iteracion_sprint-08_v1.0.md  
**Versión:** 1.0  
**Fecha:** 2026-07-08

## 📋 Información general

* Proyecto: Motor DSL para generación y renderizado de documentos (ESC/POS / PDF / Preview)
* Sprint: 08
* Duración: 2 semanas
* Fecha inicio: 08/07/2026
* Fecha fin: 21/07/2026
* Objetivo del sprint: Implementar MotorDsl.MultaApp con soporte de imágenes bitmap, renderers custom (PDF y ESC/POS con rasterización) y fluent API para extensibilidad.

---

## 🎯 Objetivo del Sprint

Construir la **segunda app de ejemplo** — un acta de infracción de tránsito completo — que demuestre todas las capacidades avanzadas del motor:

* Soporte de ImageNode bitmap en el pipeline de layout
* Contrato `IBitmapRasterizer` para que el cliente inyecte la rasterización
* Fluent API `.AddRenderer<T>()` para registrar renderers custom
* App MAUI completa con preview, hex dump, PDF y exportación REST
* BitmapEscPosRenderer (SkiaSharp) y PdfRenderer (PdfSharpCore) implementados por la app

👉 Este sprint habilita **CU-08** (Renderizar vista previa UI — multa), **CU-09** (Renderizar PDF), **CU-10** (Enviar a impresora BT — con logo), y valida la **extensibilidad del motor** documentada en la arquitectura.

### Decisión de arquitectura

> **La librería core (`MotorDsl.Core`) NO agrega dependencias de terceros.**
>
> * `IBitmapRasterizer`: el cliente implementa la rasterización (con SkiaSharp, ImageSharp, o lo que prefiera)
> * `PdfRenderer`: el cliente implementa `IRenderer` con PdfSharpCore u otra librería PDF
> * El core solo provee los contratos y el pipeline — las dependencias pesadas quedan del lado de la app

---

## 🧩 Historias / tareas comprometidas

| ID    | Tipo     | Descripción                                                                          | Prioridad | Estimación |
| ----- | -------- | ------------------------------------------------------------------------------------ | --------- | ---------- |
| US-29 | Historia | Como desarrollador, quiero renderizar imágenes bitmap en tickets ESC/POS             | Alta      | 8 pts      |
| US-30 | Historia | Como desarrollador, quiero generar PDF de un documento sin que la librería core dependa de PdfSharpCore | Alta      | 5 pts      |
| US-31 | Historia | Como desarrollador, quiero una app de ejemplo avanzada que demuestre todas las funcionalidades | Alta      | 13 pts     |
| TK-59 | Técnica  | Soporte ImageNode bitmap en LayoutEngine — metadata: source, width, height, is_bitmap | Alta      | 5 pts      |
| TK-60 | Técnica  | Definir `IBitmapRasterizer` en `MotorDsl.Core/Contracts`                             | Alta      | 3 pts      |
| TK-61 | Técnica  | Fluent API `.AddRenderer<T>()` en `MotorDslBuilder`                                  | Alta      | 3 pts      |
| TK-62 | Técnica  | Crear `docs/11_examples/` con documentación de ejemplos                              | Media     | 2 pts      |
| TK-63 | Técnica  | Crear `samples/MotorDsl.MultaApp/` con estructura base y `MultaDsl.cs` (template + datos hardcodeados) | Alta      | 5 pts      |
| TK-64 | Técnica  | `BitmapEscPosRenderer` en MultaApp con SkiaSharp para rasterización de imágenes      | Alta      | 8 pts      |
| TK-65 | Técnica  | `PdfRenderer` en MultaApp con PdfSharpCore                                           | Alta      | 8 pts      |
| TK-66 | Técnica  | `MauiProgram.cs` de MultaApp con DI completo (motor + renderers custom + servicios)  | Alta      | 3 pts      |
| TK-67 | Técnica  | `MainPage.xaml` de MultaApp con pestañas: Preview MAUI / Hex dump / PDF / Exportar API | Alta      | 5 pts      |
| TK-68 | Técnica  | Tests unitarios: `IBitmapRasterizer`, fluent API `.AddRenderer<T>()`, ImageNode layout | Alta      | 5 pts      |

**Total estimado: 56 story points**

---

## 🏗️ Alcance técnico del sprint

### TK-59 — Soporte ImageNode bitmap en LayoutEngine

* `LayoutEngine` actualmente pasa `ImageNode` como nodo evaluado sin procesamiento especial
* Agregar lógica para marcar la imagen en `LayoutedDocument.NodeLayoutInfo` (diccionario `string → LayoutInfo`). No existen clases `LayoutedNode`/`LayoutedImageNode`: el LayoutEngine escribe flags en `LayoutInfo.DeviceMetadata` y el texto en `LayoutInfo.WrappedText`:

```csharp
// LayoutInfo del nodo imagen (dentro de LayoutedDocument.NodeLayoutInfo)
info.DeviceMetadata["is_bitmap"]     = true;                  // bitmap base64
info.DeviceMetadata["bitmap_source"] = source;               // data:image/png;base64,... o URL
info.DeviceMetadata["bitmap_width"]  = width;
// para QR:     is_qr / qr_data    | para barcode: is_barcode / barcode_data
info.WrappedText = "[BITMAP: source_truncado]";              // o "[IMG-DEGRADED:...]"
```

* Para `TextRenderer`: `WrappedText` lleva el placeholder `[BITMAP: source_truncado]`
* Para `EscPosRenderer` base: ignorar (la rasterización la hace el renderer custom)
* El `LayoutEngine` calcula posición y dimensiones dentro del ancho del perfil

### TK-60 — IBitmapRasterizer

* Nuevo contrato en `MotorDsl.Core/Contracts`:

```csharp
/// <summary>
/// Rasterize images for ESC/POS printers.
/// The client injects the implementation (e.g., SkiaSharp).
/// </summary>
public interface IBitmapRasterizer
{
    /// <summary>
    /// Converts an image source (base64 data URI or path) to ESC/POS bitmap bytes.
    /// Uses GS v 0 command format for raster bit image.
    /// </summary>
    /// <param name="source">Image source (data:image/png;base64,... or file path)</param>
    /// <param name="widthPixels">Target width in pixels (based on printer DPI)</param>
    /// <returns>ESC/POS byte sequence for the rasterized image</returns>
    byte[] Rasterize(string source, int widthPixels);
}
```

* La librería core no provee implementación — solo el contrato
* El cliente (MultaApp) implementa con SkiaSharp
* `BitmapEscPosRenderer` recibe `IBitmapRasterizer` por DI

### TK-61 — Fluent API .AddRenderer\<T\>()

* Agregar método genérico a `MotorDslBuilder`:

```csharp
public MotorDslBuilder AddRenderer<TRenderer>()
    where TRenderer : class, IRenderer
{
    Services.AddSingleton<IRenderer, TRenderer>();
    return this;
}
```

* Permite registrar renderers custom de forma idiomática:

```csharp
builder.Services.AddMotorDslEngine()
    .AddRenderer<BitmapEscPosRenderer>()
    .AddRenderer<PdfRenderer>()
    .AddTemplates(t => t.Add("acta-infraccion", MultaDsl.Template))
    .AddProfiles(p => { ... });
```

* `RendererRegistry` se actualiza para resolver `IEnumerable<IRenderer>` del DI

### TK-62 — Documentación de ejemplos (ya hecho)

* `docs/11_examples/README.md` — índice de ejemplos ✅
* `docs/11_examples/ejemplo-01-simple.md` — documentación SampleApp ✅
* `docs/11_examples/ejemplo-02-multa.md` — documentación de diseño MultaApp ✅
* `docs/10_developer_guide/integracion-api-rest.md` — patrón REST ✅

### TK-63 — Estructura base MultaApp + MultaDsl.cs

* Crear proyecto MAUI en `samples/MotorDsl.MultaApp/`
* Agregar al solution `PrintThermalDriver.sln`
* `Templates/MultaDsl.cs` con:
  * Template DSL completo del acta de infracción (documentado en ejemplo-02-multa.md)
  * `GetSampleData()` con datos hardcodeados de ejemplo
* Dependencias NuGet: SkiaSharp 3.119.2, PdfSharpCore 1.3.67

### TK-64 — BitmapEscPosRenderer

* Archivo: `samples/MotorDsl.MultaApp/Renderers/BitmapEscPosRenderer.cs`
* Implementa `IRenderer` con `Target = "escpos-bitmap"`
* Usa `IBitmapRasterizer` (inyectado) para convertir imágenes
* Para nodos de texto, delega a `EscPosCommands` existentes
* Para imágenes (`LayoutInfo.DeviceMetadata["is_bitmap"]` con `bitmap_source`): llama a `_rasterizer.Rasterize(source, width)`

### TK-65 — PdfRenderer

* Archivo: `samples/MotorDsl.MultaApp/Renderers/PdfRenderer.cs`
* Implementa `IRenderer` con `Target = "pdf"`
* Usa PdfSharpCore (A4, word-wrap por medición real, page-break automático; `MotorDslFontResolver : IFontResolver`) para mapear `LayoutedDocument` a PDF, recorriendo `NodeLayoutInfo`:
  * Texto (`WrappedText`) → dibujado con estilos leídos de `DeviceMetadata` (`bold`) y `LayoutInfo.Alignment`
  * Imagen (`DeviceMetadata["is_bitmap"]` / `bitmap_source`) → imagen desde bytes base64
  * Tabla → columnas calculadas (la metadata de tabla también va en `NodeLayoutInfo`)
* Retorna `RenderResult` con `Output = byte[]` (PDF binario)

### TK-66 — MauiProgram.cs de MultaApp

Hay **dos rutas de composición** según cómo referencie la app a MotorDsl:

**(a) ProjectReference (apps `MotorDsl.MultaApp` / `Integrated.MultaApp`)** — renderers locales + `.AddRenderer<T>()` y rasterizer manual:

```csharp
builder.Services.AddMotorDslEngine()
    .AddRenderer<BitmapEscPosRenderer>()
    .AddRenderer<PdfRenderer>()
    .AddTemplates(t => t.Add("acta-infraccion", MultaDsl.Template))
    .AddProfiles(p =>
    {
        p.Add(new DeviceProfile("thermal_58mm", 32, "escpos-bitmap"));
        p.Add(new DeviceProfile("a4-pdf", 80, "pdf"));
    });

builder.Services.AddSingleton<IBitmapRasterizer, SkiaSharpRasterizer>();
builder.Services.AddSingleton<IThermalPrinterService, ThermalPrinterService>();
```

**(b) NuGet (apps `MotorDsl.Nuget.MultaApp` / `Nuget.Integrated.MultaApp`)** — `.AddMotorDslMaui()` (en `MotorDsl.Maui`, extiende `MotorDslBuilder`) aporta el rasterizer y los renderers (`PdfRenderer`, `BitmapEscPosRenderer`, `RasterPreviewRenderer`), y el `IThermalPrinterService` vía `AddMotorDslPrinting`:

```csharp
builder.Services.AddMotorDslEngine()
    .AddTemplates(t => t.Add("acta-infraccion", MultaDsl.Template))
    .AddProfiles(p => { /* ... */ })
    .AddMotorDslMaui();   // registra IBitmapRasterizer→SkiaSharpRasterizer, QrCodeRasterizer,
                          // PdfRenderer/BitmapEscPosRenderer/RasterPreviewRenderer y AddMotorDslPrinting
builder.Services.AddBluetoothPrinterTransport();
```

### TK-67 — MainPage.xaml con pestañas

* UI con `TabbedPage` o `TabBar`:

| Pestaña          | Contenido                                           |
|------------------|-----------------------------------------------------|
| **Preview**      | `MauiDocumentPreview` con `RenderLayout()`          |
| **ESC/POS**      | Hex dump formateado de los bytes ESC/POS generados  |
| **PDF**          | WebView o botón para abrir PDF generado             |
| **Exportar API** | Botón que envía ToBase64() a endpoint (mock/stub)   |

* Cada pestaña llama a `_engine.Render()` con el profile correspondiente
* La pestaña API hace POST con payload JSON (stub — sin servidor real)

### TK-68 — Tests unitarios

* `Sprint08Tests.cs` (~8 tests):

| Test | Descripción |
|------|-------------|
| 1 | `IBitmapRasterizer` mock: rasteriza y retorna bytes ESC/POS |
| 2 | `LayoutEngine` marca la imagen en `NodeLayoutInfo` (`LayoutInfo.DeviceMetadata`: is_bitmap/bitmap_source) con metadata correcta |
| 3 | `TextRenderer` genera placeholder `[BITMAP: ...]` para ImageNode |
| 4 | `.AddRenderer<T>()` registra renderer en DI correctamente |
| 5 | `.AddRenderer<T>()` es chainable con `.AddTemplates()` |
| 6 | `RendererRegistry` resuelve renderer custom por target |
| 7 | Pipeline completo con ImageNode + mock rasterizer → output con imagen |
| 8 | Pipeline con template de multa válido → IsSuccessful |

---

## 🔁 Pipeline completo con este sprint

```text
DSL JSON (con ImageNode)
 ↓
Parser (Sprint 01)
 ↓
DocumentTemplate
 ↓
ITemplateValidator (Sprint 07)
 ↓
IDataValidator (Sprint 06/07)
 ↓
Evaluator (Sprint 02)
 ↓
EvaluatedDocument
 ↓
IProfileValidator (Sprint 07)
 ↓
LayoutEngine (Sprint 03 + Sprint 08)  ←── MEJORADO: marca imagen en NodeLayoutInfo (DeviceMetadata)
 ↓
LayoutedDocument (NodeLayoutInfo con is_bitmap/bitmap_source)
 ↓
┌──────────────────────────────────────┐
│ RendererRegistry (Sprint 03 + 08)     │
│  ├── TextRenderer        → string     │  [BITMAP: source]
│  ├── EscPosRenderer      → byte[]     │  texto + QR + barcode
│  ├── BitmapEscPosRenderer→ byte[]     │  ←── NUEVO: texto + imágenes (SkiaSharp)
│  └── PdfRenderer         → byte[]     │  ←── NUEVO: PDF (PdfSharpCore)
└──────────────────────────────────────┘
 ↓
RenderResult
 ├── .Output (byte[] o string)
 ├── .ToBase64() → API REST
 ├── .ToHexString() → debug
 ├── .Errors
 └── .Warnings
```

---

## 📊 Métricas del Sprint

| Métrica               | Objetivo             |
| --------------------- | -------------------- |
| Tests acumulados      | 173 → ~181           |
| Cobertura funcional   | CU-08, CU-09, CU-10 (con logo), extensibilidad |
| Story points          | 56 pts               |
| Archivos nuevos       | ~12 (contracts, app, renderers, tests) |
| Archivos modificados  | ~4 (LayoutEngine, MotorDslBuilder, RendererRegistry, .sln) |

---

## 📐 Criterios de aceptación del sprint

1. `IBitmapRasterizer` definido como contrato en `MotorDsl.Core` — sin implementación en core
2. `LayoutEngine` marca la imagen en `NodeLayoutInfo` (`LayoutInfo.DeviceMetadata`: bitmap_source, bitmap_width, is_bitmap)
3. `.AddRenderer<T>()` funciona en la fluent API y es chainable
4. `MultaApp` compila y despliega en Android
5. `MultaDsl.cs` contiene template completo del acta con: logo, infractor, vehículo, tabla de infracciones, QR condicional, firma
6. `BitmapEscPosRenderer` rasteriza imágenes con SkiaSharp para impresora térmica
7. `PdfRenderer` genera PDF válido con PdfSharpCore
8. Las 4 pestañas funcionan: Preview MAUI, Hex dump, PDF, Exportar API
9. ~8 tests nuevos pasando (total ~181)
10. Build limpio + todos los tests pasando + committed

---

## 📝 Dependencias entre tareas

```text
TK-59 (ImageNode layout) ──┐
TK-60 (IBitmapRasterizer) ─┤
TK-61 (AddRenderer<T>) ────┤
                            ├──► TK-63 (MultaApp base + MultaDsl.cs)
TK-62 (docs — ya hecho) ───┘        │
                                     ├──► TK-64 (BitmapEscPosRenderer)
                                     ├──► TK-65 (PdfRenderer)
                                     ├──► TK-66 (MauiProgram.cs)
                                     └──► TK-67 (MainPage.xaml)
                                              │
                                              └──► TK-68 (Tests)
```

* TK-59, TK-60, TK-61 son paralelos — extienden la librería core
* TK-62 ya está completado en este sprint (documentación)
* TK-63 depende de TK-59/60/61 — crea el proyecto con los contratos disponibles
* TK-64/65/66/67 dependen de TK-63 — implementan la app
* TK-68 es el último — verifica todo

---

## 📦 Fuera de alcance (deferred)

* Conexión real a API REST (se usa mock/stub en la pestaña Exportar)
* Firma digital criptográfica del acta
* Servidor backend para recepción de multas
* Caching de templates compilados
* Logging estructurado del pipeline
* **Soporte iOS para apps de ejemplo con impresión Bluetooth.** iOS no soporta Bluetooth clásico (SPP) requerido por impresoras térmicas ESC/POS. La librería core es compatible con iOS pero las apps de ejemplo son Android-only. Un desarrollador que necesite iOS debería considerar impresoras con soporte WiFi, AirPrint o hardware certificado MFi.

---

## 🗓️ Historial de versiones

| Versión | Fecha      | Autor          | Cambios              |
| ------- | ---------- | -------------- | -------------------- |
| 1.0     | 2026-07-08 | Equipo Técnico | Plan Sprint 08 inicial |
