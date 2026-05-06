# Render Pixelado y PDF

Guía de los renderers MAUI incluidos en el paquete `MotorDsl.Maui`. Cubre:

- `RasterPreviewRenderer` (target `raster-preview`) — vista previa pixelada en PNG.
- `QrCodeRasterizer` — helper QR reusable por todos los renderers.
- `MauiRasterPreview` — control que muestra el PNG con zoom.
- `PdfRenderer` (target `pdf`) — generación PDF con QR + barcode-text + word-wrap + page-break.

---

## 1. `RasterPreviewRenderer`

Produce un **PNG** que simula lo que la impresora térmica rasterizaría: texto
monoespaciado a 1-bit, bitmaps, QR y barcode (este último como fallback de
texto). Útil para vista previa en pantalla sin enviar al hardware.

**Target:** `"raster-preview"`
**Salida:** `byte[]` con un PNG.

### 1.1 Cómo funciona

1. Ordena los `LayoutInfo` del `LayoutedDocument` por `LineNumber, ColumnNumber`.
2. **Pase 1 (medición):** pre-rasteriza QRs y bitmaps para conocer su altura
   real y suma `lineHeight` por cada línea de texto.
3. **Pase 2 (dibujo):** crea un `SKBitmap` del ancho del canvas y la altura
   total calculada, dibuja en orden con `SkiaSharp` (font monoespaciada,
   antialias **off** para preservar el look pixelado).
4. Encodea el bitmap a PNG y lo devuelve en `result.Output`.

### 1.2 Capabilities del profile

| Capability | Tipo | Default | Efecto |
|---|---|---|---|
| `bitmap_max_width_px` | `int` | `384` | Ancho del canvas en píxeles. |
| `Width` (propiedad del DeviceProfile) | `int` | `32` | Caracteres por línea. Determina el `fontSize`: `fontSize = (canvasWidth / chars) / 0.6`. |

```csharp
var profile = new DeviceProfile("preview", 32, "raster-preview");
profile.SetCapability("bitmap_max_width_px", 384);
```

### 1.3 Tipos de nodo soportados

| Node metadata | Comportamiento |
|---|---|
| `is_qr = true`, `qr_data = "..."` | Genera QR con `QrCodeRasterizer.Rasterize(data, moduleSize: 4)`. |
| `is_bitmap = true`, `bitmap_source = "..."` | Llama a `IBitmapRasterizer.Rasterize(source, canvasWidth)` y lo dibuja a 1-bit. |
| `is_barcode = true`, `barcode_data = "..."` | Fallback: dibuja `[Código: <data>]` (no genera código de barras real aún). |
| `bold = true` | Carga la variante Bold del typeface. |
| `align = center / right / left` | Calcula `x = (canvas - content) / 2` o `canvas - content - 8` o `8`. |

### 1.4 Salida de errores

- Si la rasterización de un bitmap falla, agrega `result.Warnings` con el
  número de línea y continúa.
- Si todo el render falla, agrega `result.Errors` y devuelve `Output = []`.

---

## 2. `QrCodeRasterizer`

Helper standalone que rasteriza datos como QR a un `SKBitmap`. Reutilizable
por `PdfRenderer` y `RasterPreviewRenderer`.

### 2.1 API

```csharp
public class QrCodeRasterizer
{
    public SKBitmap Rasterize(
        string data,
        int moduleSize = 4,
        QRCoder.QRCodeGenerator.ECCLevel ecc = QRCoder.QRCodeGenerator.ECCLevel.M);

    public SKSizeI Measure(
        string data,
        int moduleSize = 4,
        QRCoder.QRCodeGenerator.ECCLevel ecc = QRCoder.QRCodeGenerator.ECCLevel.M);
}
```

### 2.2 Parámetros

| Parámetro | Default | Descripción |
|---|---|---|
| `data` | (required) | Texto a codificar. Debe ser no-vacío. |
| `moduleSize` | `4` | Píxeles por módulo del QR. Más grande = QR más nítido pero más grande. |
| `ecc` | `ECCLevel.M` | Nivel de corrección de errores (`L`, `M`, `Q`, `H`). |

### 2.3 Implementación

Usa `QRCoder.QRCodeGenerator` para generar la matriz, exporta a PNG con
`PngByteQRCode.GetGraphic(moduleSize)` y luego decodifica con
`SKBitmap.Decode` para entregar un `SKBitmap` listo para el canvas.

### 2.4 Uso desde DI

`AddMotorDslMaui()` lo registra como singleton. En un renderer custom:

```csharp
public MyRenderer(QrCodeRasterizer qr) { _qr = qr; }
```

---

## 3. `MauiRasterPreview` — control para la vista previa

`ContentView` que muestra el PNG producido por `RasterPreviewRenderer` con
escalado configurable.

### 3.1 XAML

```xml
<muic:MauiRasterPreview x:Name="RasterPreview" ZoomFactor="2" />
```

### 3.2 Code-behind

```csharp
private void OnPreviewClicked(object? sender, EventArgs e)
{
    var profile = new DeviceProfile("preview", 32, "raster-preview");
    var result  = _engine.Render(jsonDsl, profile);
    if (result.IsSuccessful && result.Output is byte[] png)
        RasterPreview.ImageBytes = png;
}
```

### 3.3 Comportamiento

- Decodifica el tamaño nativo del PNG con `SkiaSharp` y aplica
  `WidthRequest = nativeWidth * ZoomFactor`. Esto evita interpolación visual.
- Está envuelto en un `ScrollView` bidireccional para permitir desplazarse en
  imágenes grandes.
- Si `ImageBytes` es `null`/vacío, muestra placeholder *"(sin vista previa)"*.

> Detalle ampliado en [`componentes-ux-maui.md`](componentes-ux-maui.md).

---

## 4. `PdfRenderer` — fixes respecto a v1.0

`PdfRenderer` (target `"pdf"`, salida `byte[]` con un documento PDF) usa
`PdfSharpCore` para producir un PDF imprimible o exportable. La versión
incluida en `MotorDsl.Maui` agrega varios soportes que en v1.0 fallaban o
estaban ausentes.

### 4.1 QR vía `QrCodeRasterizer`

Antes los nodos QR no se renderizaban en el PDF. Ahora `PdfRenderer` resuelve
el `qr_data` y delega en `QrCodeRasterizer` para generar el bitmap, que se
embebe como `XImage` en el PDF a la altura calculada por el layout.

### 4.2 Fallback barcode → texto

Para nodos `is_barcode = true` el render dibuja `[Código: <data>]` como
placeholder textual centrado. Esto evita que el PDF aparezca vacío en lugar
del código de barras hasta que se implemente un encoder EAN-13 / Code128 nativo.

### 4.3 Word-wrap automático con `gfx.MeasureString`

Cada línea de texto se mide contra el ancho disponible
(`page.Width - margins`) usando `gfx.MeasureString(text, font)`. Si excede,
se parte por palabras y se dibujan tantas líneas como sea necesario.

```csharp
var width = gfx.MeasureString(text, font).Width;
if (width > availableWidth)
{
    var words = text.Split(' ');
    // greedy word wrap...
}
```

### 4.4 Page-break automático

Antes el renderer escribía todo en la página 1 y desbordaba. Ahora controla
el `currentY` y, si la siguiente línea excede `page.Height - bottomMargin`,
crea una nueva `PdfPage` y resetea `currentY` al margen superior.

```csharp
if (currentY + lineHeight > page.Height - bottomMargin)
{
    page = pdfDoc.AddPage();
    gfx  = XGraphics.FromPdfPage(page);
    currentY = topMargin;
}
```

### 4.5 Comparativa

| Característica | v1.0 | v1.1 |
|---|---|---|
| QR en PDF | ❌ ausente | ✅ vía `QrCodeRasterizer` |
| Barcode | ❌ ausente | ✅ fallback texto `[Código: ...]` |
| Word-wrap | ❌ texto largo se cortaba | ✅ wrap por palabras con `MeasureString` |
| Page-break | ❌ overflow visual | ✅ nueva página automática |
| Font | DroidSans embebida en `Resources/Fonts` | Igual |
| Logo bitmap | ✅ | ✅ (sin cambios) |

---

## 5. Cómo elegir el renderer correcto

| Caso | Profile target | Output |
|---|---|---|
| Imprimir en térmica con bitmap (logo, QR) | `escpos-bitmap` | `byte[]` GS v 0 |
| Imprimir en térmica sólo texto | `escpos` | `byte[]` ESC/POS clásico |
| Vista previa pixelada en pantalla | `raster-preview` | `byte[]` PNG |
| PDF para email / archivo / AirPrint | `pdf` | `byte[]` PDF |
| Texto plano (debug) | `text` | `string` |

---

## 6. Orden de aplicación

```text
DSL JSON → Parser → Evaluator → LayoutEngine → IRenderer (elige por profile.RenderTarget)
                                                  │
                       ┌──────────────────────────┼────────────────┐
                       ▼                          ▼                ▼
             RasterPreviewRenderer         PdfRenderer    BitmapEscPosRenderer
                       │                          │                │
                       ▼                          ▼                ▼
                    PNG                         PDF            ESC/POS bytes
                       │                          │                │
                       ▼                          ▼                ▼
              MauiRasterPreview              Launcher /       IThermalPrinterService
                                            File.WriteAll     .SendBytesAsync(...)
```

---

## 7. Referencias

- `src/MotorDsl.Maui/Renderers/RasterPreviewRenderer.cs`
- `src/MotorDsl.Maui/Renderers/QrCodeRasterizer.cs`
- `src/MotorDsl.Maui/Renderers/PdfRenderer.cs`
- `src/MotorDsl.Maui/Renderers/BitmapEscPosRenderer.cs`
- [Componentes UX MAUI](componentes-ux-maui.md)
- [Guía de Integración MAUI](guia-integracion-maui.md)
- [Arquitectura de la Solución (v1.1)](../05_arquitectura_tecnica/arquitectura-solucion_v1.1.md)
