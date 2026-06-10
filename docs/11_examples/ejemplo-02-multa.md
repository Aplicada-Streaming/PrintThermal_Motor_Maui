# Ejemplo 02 — MotorDsl.MultaApp

> Acta de infracción de tránsito. Caso real con todas las funcionalidades de la librería.

**Estado:** Implementado.

---

## 1. Propósito y Audiencia

Aplicación avanzada que demuestra cómo usar MotorDsl en un escenario real de gobierno/fiscalización. Dirigida al desarrollador que ya conoce los fundamentos (ver [ejemplo-01-simple.md](ejemplo-01-simple.md)) y necesita:

- Templates complejos con todos los tipos de nodo
- Imágenes bitmap (logo) rasterizadas para impresora térmica
- Renderers custom implementados por la app (`IRenderer`)
- Generación de PDF sin dependencias en la librería core
- Integración con API REST para persistencia

**Nivel:** Avanzado  
**Ubicación:** `samples/MotorDsl.MultaApp/`

---

## 2. Funcionalidades

| Feature                    | Descripción                                              |
|----------------------------|----------------------------------------------------------|
| Preview MAUI               | Vista previa nativa del acta con `RenderLayout()`        |
| Hex dump ESC/POS           | Visualización de los comandos ESC/POS generados          |
| PDF                        | Generación y vista previa de PDF via PdfSharpCore        |
| Exportar a API REST        | Enviar el ticket en Base64 a un endpoint                 |
| Impresión térmica con logo | Imprimir en impresora BT con imagen rasterizada          |
| Código QR                  | QR de pago condicional (si permite pago online)          |
| Validación formal          | Template, datos y profile validados antes del render     |

---

## 3. Arquitectura

### Árbol de archivos

```
samples/MotorDsl.MultaApp/
├── MauiProgram.cs                  ← Registro DI + renderers custom
├── Templates/
│   ├── MultaDsl.cs                 ← Template DSL + datos hardcodeados
│   ├── TicketSimpleDsl.cs          ← Template DSL ticket simple
│   └── ComprobanteDsl.cs           ← Template DSL comprobante de pago
├── Pages/
│   ├── MainPage.xaml               ← UI con pestañas
│   └── MainPage.xaml.cs            ← Lógica: preview, hex, pdf, api
├── Renderers/
│   ├── BitmapEscPosRenderer.cs     ← IRenderer: ESC/POS con imágenes
│   ├── PdfRenderer.cs              ← IRenderer: PDF con PdfSharpCore
│   └── SkiaSharpRasterizer.cs      ← IBitmapRasterizer: base64 → 1-bit
├── Services/
│   └── ThermalPrinterService.cs    ← Conexión BT (reutilizado)
└── MotorDsl.MultaApp.csproj
```

### Dependencias NuGet

| Paquete                           | Versión  | Uso                                              |
|-----------------------------------|----------|--------------------------------------------------|
| SkiaSharp.Views.Maui.Controls     | 3.119.2  | Rasterizar imágenes base64 → ESC/POS bitmap      |
| PdfSharpCore                      | 1.3.67   | Generar PDF desde `LayoutedDocument`             |

> **Decisión de arquitectura:** La librería core (`MotorDsl.Core`) NO incluye estas dependencias. Es responsabilidad de la app cliente implementar los renderers que las necesiten, registrándolos como `IRenderer`.

### Integración con la librería

```
MotorDsl.MultaApp
    │
    ├── Usa MotorDsl.Core (pipeline, modelos, validación)
    ├── Usa MotorDsl.Extensions (DI, fluent API)
    ├── Usa MotorDsl.Rendering (TextRenderer,EscPosRenderer base)
    │
    └── Implementa sus propios renderers:
        ├── BitmapEscPosRenderer : IRenderer  (Target = "escpos-bitmap")
        └── PdfRenderer : IRenderer           (Target = "pdf")
```

---

## 4. Template DSL de la Multa

```json
{
  "id": "acta-infraccion-001",
  "version": "1.0",
  "root": {
    "type": "container",
    "layout": "vertical",
    "children": [
      {
        "type": "image",
        "source": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAABU0lEQVR4nO2Vu0oDQRSGv9lsNokxEUUbwUawsLG0sfMBfAFfwNrCwsLKB7CxsrBQC+8XvKBGE41ZL7uzOzOSYpfdb2b+c/7/nJkFh//OoKkBtgJLwApQBy6AG8WAJYf/qJOlrOuBC+AcWJLEfOATCp7mgHlJzHfjGSNwCMwD0wlcxAD3fhGbqpxc+lXgKi/CRtYJCEgCJ2RB3NdiYINYAc4kvhXgEzhPz4FT4EoSc1K7L0H8NLTCANMuUMnMt/g56b2fUMfbovmZnzBcQJseGpfACeAxsS/B7wLQkkSb+MCvgPMSWI+GRv4FbwHrCYxnw4GJvHW3p0+2BeeA2Yi3qVkejwqF2GrKRenKD/RG2e4ybyHewbVnmz25QnwVpx4nLiB1iLeuJN4WPbXv6Ot3xKPwKHkpjP+0YDg0pv/4KfScx3A4HAr4FbSew3xA0+U4n5PxD/ACdkdx8asnkuAAAAAElFTkSuQmCC",
        "style": { "align": "center", "width": 32, "height": 32 },
        "_comment": "Logo del organismo 32x32 PNG"
      },
      {
        "type": "text",
        "text": "",
        "_comment": "Línea en blanco después del logo"
      },
      {
        "type": "text",
        "text": "ACTA DE INFRACCIÓN",
        "style": { "align": "center", "bold": true }
      },
      {
        "type": "text",
        "text": "Municipalidad de {{municipio}}",
        "style": { "align": "center" }
      },
      {
        "type": "text",
        "text": "Acta N° {{actaNumero}}    Fecha: {{fecha}}",
        "style": { "bold": true }
      },
      {
        "type": "text",
        "text": "================================"
      },
      {
        "type": "text",
        "text": "DATOS DEL INFRACTOR",
        "style": { "bold": true }
      },
      {
        "type": "text",
        "text": "Nombre: {{infractor.nombre}}"
      },
      {
        "type": "text",
        "text": "DNI:    {{infractor.dni}}"
      },
      {
        "type": "text",
        "text": "Domicilio: {{infractor.domicilio}}"
      },
      {
        "type": "text",
        "text": "================================"
      },
      {
        "type": "text",
        "text": "DATOS DEL VEHÍCULO",
        "style": { "bold": true }
      },
      {
        "type": "text",
        "text": "Patente: {{vehiculo.patente}}"
      },
      {
        "type": "text",
        "text": "Marca:   {{vehiculo.marca}}"
      },
      {
        "type": "text",
        "text": "Modelo:  {{vehiculo.modelo}}"
      },
      {
        "type": "text",
        "text": "================================"
      },
      {
        "type": "text",
        "text": "INFRACCIONES",
        "style": { "bold": true }
      },
      {
        "type": "table",
        "headers": ["Art.", "Descripción", "Pts", "Monto"],
        "rows": [
          ["42.1", "Exceso velocidad", "4", "15000.00"],
          ["38.3", "Giro prohibido", "2", "8500.00"]
        ],
        "_comment": "Tabla de infracciones: headers + rows (TableNode)"
      },
      {
        "type": "text",
        "text": "================================"
      },
      {
        "type": "text",
        "text": "TOTAL A PAGAR: ${{totalMonto}}",
        "style": { "align": "right", "bold": true }
      },
      {
        "type": "text",
        "text": "Puntos totales: {{totalPuntos}}",
        "style": { "align": "right" }
      },
      {
        "type": "text",
        "text": ""
      },
      {
        "type": "conditional",
        "expression": "{{permitePagoOnline}}",
        "trueBranch": {
          "type": "container",
          "layout": "vertical",
          "children": [
            {
              "type": "text",
              "text": "Pague online escaneando el QR:",
              "style": { "align": "center" }
            },
            {
              "type": "image",
              "source": "{{qrPagoUrl}}",
              "imageType": "qrcode",
              "style": { "align": "center" }
            }
          ]
        },
        "falseBranch": {
          "type": "text",
          "text": "Pague en oficinas de Tránsito Municipal",
          "style": { "align": "center" }
        },
        "_comment": "Si permite pago online muestra QR, si no indica oficina"
      },
      {
        "type": "text",
        "text": ""
      },
      {
        "type": "text",
        "text": "================================"
      },
      {
        "type": "text",
        "text": "Firma del agente:"
      },
      {
        "type": "image",
        "source": "{{firmaAgente}}",
        "style": { "align": "center", "width": 24, "height": 12 },
        "_comment": "Firma digitalizada del agente como base64"
      },
      {
        "type": "text",
        "text": "Ag. {{agente.nombre}} - Leg. {{agente.legajo}}",
        "style": { "align": "center" }
      }
    ]
  }
}
```

### Tipos de nodo utilizados

| Tipo          | Cantidad | Función                                        |
|---------------|----------|------------------------------------------------|
| `container`   | 2        | Raíz y grupo del QR condicional                |
| `text`        | 19       | Encabezados, datos, separadores, pie           |
| `image`       | 3        | Logo del organismo + QR de pago + firma        |
| `table`       | 1        | Tabla de infracciones (headers + rows)         |
| `conditional` | 1        | QR de pago online vs. texto de oficina         |

---

## 5. Datos de Ejemplo

```csharp
public static Dictionary<string, object> GetSampleData() => new()
{
    // Encabezado
    ["municipio"]   = "San Miguel de Tucumán",
    ["actaNumero"]  = "SMT-2026-004571",
    ["fecha"]       = "30/03/2026 14:35",

    // Infractor
    ["infractor"] = new Dictionary<string, object>
    {
        ["nombre"]    = "Juan Carlos Pérez",
        ["dni"]       = "28.456.789",
        ["domicilio"] = "Av. Mate de Luna 2100"
    },

    // Vehículo
    ["vehiculo"] = new Dictionary<string, object>
    {
        ["patente"] = "AB 123 CD",
        ["marca"]   = "Volkswagen",
        ["modelo"]  = "Gol Trend 2019"
    },

    // Infracciones (tabla)
    ["infracciones"] = new List<Dictionary<string, object>>
    {
        new()
        {
            ["articulo"]    = "42.1",
            ["descripcion"] = "Exceso velocidad",
            ["puntos"]      = "4",
            ["monto"]       = "15000.00"
        },
        new()
        {
            ["articulo"]    = "38.3",
            ["descripcion"] = "Giro prohibido",
            ["puntos"]      = "2",
            ["monto"]       = "8500.00"
        }
    },

    // Totales
    ["totalMonto"]  = "23500.00",
    ["totalPuntos"] = "6",

    // Pago online
    ["permitePagoOnline"] = true,
    ["qrPagoUrl"]         = "https://multas.tucuman.gob.ar/pago/SMT-2026-004571",

    // Agente
    ["agente"] = new Dictionary<string, object>
    {
        ["nombre"] = "María López",
        ["legajo"] = "T-1247"
    },

    // Firma del agente (base64 de imagen 48x24 placeholder)
    ["firmaAgente"] = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAADAAAA" +
                      "AYCAYAAACk/IOkAAAAMklEQVR4nO3OMQEAAAgDoGl" +
                      "j/0tWwR5cQDZ5sCmpqampqampqampqampqampqampq" +
                      "an5twBf8AAFiHcj8AAAAASUVORK5CYII="
};
```

---

## 6. Renderers Custom

### A) BitmapEscPosRenderer

Extiende los comandos ESC/POS con soporte para imágenes rasterizadas con SkiaSharp.

```csharp
public class BitmapEscPosRenderer : IRenderer
{
    public string Target => "escpos-bitmap";

    // CP437 para texto plano (consistente con EscPosRenderer)
    private static readonly Encoding Cp437 =
        CodePagesEncodingProvider.Instance.GetEncoding(437) ?? Encoding.ASCII;

    public RenderResult Render(LayoutedDocument document, DeviceProfile profile)
    {
        var ms = new MemoryStream();
        ms.Write(EscPosCommands.Init);

        // El LayoutEngine ya resolvió cada nodo en una entrada de NodeLayoutInfo.
        // Iteramos ordenando por LineNumber / ColumnNumber.
        var ordered = document.NodeLayoutInfo.Values
            .OrderBy(i => i.LineNumber).ThenBy(i => i.ColumnNumber);

        foreach (var info in ordered)
        {
            // Imagen bitmap → ESC/POS GS v 0
            if (info.DeviceMetadata.TryGetValue("is_bitmap", out var b) && b is true)
            {
                var source = info.DeviceMetadata["bitmap_source"]?.ToString() ?? "";
                var bitmap = DecodeBase64ToBitmap(source);
                ms.Write(RasterizeToBitImage(bitmap, profile.Width));
                continue;
            }

            if (string.IsNullOrEmpty(info.WrappedText)) continue;

            // Alineación según LayoutInfo.Alignment
            ms.Write(info.Alignment switch
            {
                "center" => EscPosCommands.AlignCenter,
                "right"  => EscPosCommands.AlignRight,
                _        => EscPosCommands.AlignLeft
            });

            // Texto plano codificado en CP437 (no hay EscPosCommands.Text)
            ms.Write(Cp437.GetBytes(info.WrappedText));
            ms.Write(EscPosCommands.LineFeed);
        }

        ms.Write(EscPosCommands.CutFull);
        return new RenderResult(Target, ms.ToArray());
    }

    private SKBitmap DecodeBase64ToBitmap(string base64Source)
    {
        // Extraer bytes del data URI
        var base64 = base64Source.Split(",")[1];
        var bytes = Convert.FromBase64String(base64);
        return SKBitmap.Decode(bytes);
    }

    private byte[] RasterizeToBitImage(SKBitmap bitmap, int widthChars)
    {
        // Convertir a 1-bit y generar comando GS v 0
        // ... implementación SkiaSharp
    }
}
```

> **Decisión:** La librería core provee `EscPosCommands` con las secuencias ESC/POS (alineación, estilos, corte, QR y código de barras). El texto plano se codifica en **CP437** (`Encoding 437`), igual que hace el `EscPosRenderer` base. La rasterización de imágenes queda del lado del cliente porque requiere SkiaSharp.

### B) PdfRenderer

Genera PDF usando PdfSharpCore (`PdfDocument` + `XGraphics`), mapeando cada
entrada de `LayoutedDocument.NodeLayoutInfo` a líneas dibujadas en la página.

```csharp
public class PdfRenderer : IRenderer
{
    public string Target => "pdf";

    public RenderResult Render(LayoutedDocument document, DeviceProfile profile)
    {
        using var pdf = new PdfDocument();
        var page = pdf.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;

        using var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("DroidSans", 10, XFontStyle.Regular);
        double y = 40;

        // El LayoutEngine ya resolvió cada nodo en NodeLayoutInfo.
        var ordered = document.NodeLayoutInfo.Values
            .OrderBy(i => i.LineNumber).ThenBy(i => i.ColumnNumber);

        foreach (var info in ordered)
        {
            // Imagen bitmap embebida (base64)
            if (info.DeviceMetadata.TryGetValue("is_bitmap", out var b) && b is true)
            {
                var source = info.DeviceMetadata["bitmap_source"]?.ToString() ?? "";
                var imgBytes = Convert.FromBase64String(source.Split(",")[1]);
                using var ms = new MemoryStream(imgBytes);
                var img = XImage.FromStream(() => ms);
                gfx.DrawImage(img, 40, y, img.PixelWidth, img.PixelHeight);
                y += img.PixelHeight + 4;
                continue;
            }

            if (string.IsNullOrEmpty(info.WrappedText)) continue;

            // Bold según DeviceMetadata["bold"]
            var lineFont = info.DeviceMetadata.TryGetValue("bold", out var bold) && bold is true
                ? new XFont("DroidSans", 10, XFontStyle.Bold)
                : font;

            // Alineación según LayoutInfo.Alignment
            var fmt = info.Alignment switch
            {
                "center" => XStringFormats.TopCenter,
                "right"  => XStringFormats.TopRight,
                _        => XStringFormats.TopLeft
            };

            gfx.DrawString(info.WrappedText, lineFont, XBrushes.Black,
                new XRect(40, y, page.Width - 80, 14), fmt);
            y += 14;
        }

        using var outMs = new MemoryStream();
        pdf.Save(outMs, false);
        return new RenderResult(Target, outMs.ToArray());
    }
}
```

### Registro en DI

```csharp
// MauiProgram.cs
builder.Services.AddMotorDslEngine()
    .AddTemplates(t => t.Add("acta-infraccion", MultaDsl.Template))
    .AddProfiles(p =>
    {
        p.Add(new DeviceProfile("thermal_58mm", 32, "escpos-bitmap"));
        p.Add(new DeviceProfile("a4-pdf", 80, "pdf"));
    })
    // Renderers custom: el builder los resuelve por DI.
    // BitmapEscPosRenderer requiere IBitmapRasterizer; PdfRenderer también lo usa.
    .AddRenderer<PdfRenderer>()
    .AddRenderer<BitmapEscPosRenderer>();

// El rasterizer que inyecta DI en BitmapEscPosRenderer(IBitmapRasterizer ...)
builder.Services.AddSingleton<IBitmapRasterizer, SkiaSharpRasterizer>();
```

> `TextRenderer` y `EscPosRenderer` ya quedan registrados de fábrica por
> `AddMotorDslEngine()`. `AddRenderer<T>()` agrega los custom y resuelve sus
> dependencias (`IBitmapRasterizer`) desde el contenedor — por eso
> `BitmapEscPosRenderer` **no** tiene constructor sin argumentos.

---

## 7. Exportar a API REST

### Endpoint esperado

```
POST /api/multas/{actaNumero}/ticket
Content-Type: application/json

{
  "actaNumero":  "SMT-2026-004571",
  "timestamp":   "2026-03-30T14:35:00Z",
  "target":      "escpos-bitmap",
  "contentB64":  "G0AbaQEA... (Base64 del ticket)",
  "pdfB64":      "JVBERi0x... (Base64 del PDF, opcional)",
  "agenteLegajo": "T-1247"
}
```

### Código de exportación

```csharp
private async void OnExportClicked(object sender, EventArgs e)
{
    // 1. Renderizar ESC/POS
    var escposProfile = new DeviceProfile("thermal_58mm", 32, "escpos-bitmap");
    var escposResult = _engine.Render(MultaDsl.Template, MultaDsl.GetSampleData(), escposProfile);

    // 2. Renderizar PDF
    var pdfProfile = new DeviceProfile("a4-pdf", 80, "pdf");
    var pdfResult = _engine.Render(MultaDsl.Template, MultaDsl.GetSampleData(), pdfProfile);

    // 3. Armar payload
    var payload = new
    {
        actaNumero   = "SMT-2026-004571",
        timestamp    = DateTime.UtcNow,
        target       = escposResult.Target,
        contentB64   = escposResult.ToBase64(),
        pdfB64       = pdfResult.ToBase64(),
        agenteLegajo = "T-1247"
    };

    // 4. Enviar
    using var http = new HttpClient();
    var response = await http.PostAsJsonAsync(
        "https://api.ejemplo.com/api/multas/SMT-2026-004571/ticket",
        payload);

    if (response.IsSuccessStatusCode)
        ShowMessage("Exportado correctamente");
}
```

---

## Relación con la Documentación

| Documento                                             | Relación                                     |
|-------------------------------------------------------|----------------------------------------------|
| [ejemplo-01-simple.md](ejemplo-01-simple.md)          | Prerrequisito — fundamentos de la librería   |
| `docs/10_developer_guide/integracion-api-rest.md`     | Patrón REST con ToBase64()                   |
| `docs/05_arquitectura_tecnica/extensibilidad-motor.md`| Cómo agregar renderers custom                |
| `docs/08_calidad_y_pruebas/guia-testing-extensibilidad.md` | Cómo testear renderers custom          |
