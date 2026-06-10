# Ejemplo 01 — MotorDsl.SampleApp

> Ticket de venta simple. Ideal para aprender la librería paso a paso.

---

## Propósito y Audiencia

Esta app está diseñada para el desarrollador que integra MotorDsl por primera vez. Demuestra el flujo mínimo completo: registrar el motor, definir un template DSL, proveer datos y renderizar a texto plano, ESC/POS y vista previa MAUI.

**Nivel:** Básico  
**Ubicación:** `samples/MotorDsl.SampleApp/`

---

## Arquitectura de la App

```
MotorDsl.SampleApp
├── MauiProgram.cs         ← Registro DI del motor
├── Templates/
│   └── TicketDsl.cs       ← Template DSL + datos hardcodeados
├── Pages/
│   ├── MainPage.xaml      ← UI con botones y área de salida
│   └── MainPage.xaml.cs   ← Lógica: preview, hex dump, print
└── Services/
    └── ThermalPrinterService.cs  ← Conexión BT con impresora
```

### Clases de la librería utilizadas

| Clase / Interface        | Paquete               | Uso en la app                          |
|--------------------------|-----------------------|----------------------------------------|
| `IDocumentEngine`        | MotorDsl.Core         | Orquestar el pipeline completo         |
| `DeviceProfile`          | MotorDsl.Core.Models  | Definir impresora 58mm / 32 columnas   |
| `RenderResult`           | MotorDsl.Core.Models  | Obtener output, errores, warnings      |
| `LayoutedDocument`       | MotorDsl.Core.Layout  | Vista previa MAUI con `RenderLayout()` |
| `AddMotorDslEngine()`    | MotorDsl.Extensions   | Registrar todos los servicios en DI    |
| `IPrintErrorHandler`     | MotorDsl.Core         | Manejo de errores de impresión         |

---

## Template DSL del Ticket

Archivo: `Templates/TicketDsl.cs`

```json
{
  "id": "ticket-venta-001",           // Identificador único del template
  "version": "1.0",                    // Versión del template
  "root": {
    "type": "container",               // Nodo raíz: contenedor vertical
    "layout": "vertical",
    "children": [
      {
        "type": "text",
        "text": "{{storeName}}",       // ← Binding: nombre del negocio
        "style": { "align": "center", "bold": true }
      },
      { "type": "text", "text": "" },  // Línea en blanco
      {
        "type": "text",
        "text": "Fecha: {{fecha}}"     // ← Binding: fecha actual
      },
      {
        "type": "text",
        "text": "--------------------------------"  // Separador
      },
      {
        "type": "loop",                // ← Loop: itera sobre items[]
        "source": "items",             //    Fuente de datos
        "itemAlias": "item",           //    Alias para cada elemento
        "body": {
          "type": "container",
          "layout": "vertical",
          "children": [
            { "type": "text", "text": "{{item.nombre}}" },
            { "type": "text", "text": "  {{item.cantidad}} x ${{item.precio}}          ${{item.total}}" }
          ]
        }
      },
      { "type": "text", "text": "--------------------------------" },
      {
        "type": "text",
        "text": "Subtotal: ${{subtotal}}",
        "style": { "align": "right" }
      },
      {
        "type": "text",
        "text": "TOTAL: ${{total}}",   // ← Binding: total con estilo bold
        "style": { "align": "right", "bold": true }
      },
      { "type": "text", "text": "" },
      {
        "type": "text",
        "text": "{{footer}}",          // ← Binding: mensaje de cierre
        "style": { "align": "center" }
      }
    ]
  }
}
```

### Tipos de nodo usados

| Tipo        | Cantidad | Función                          |
|-------------|----------|----------------------------------|
| `container` | 2        | Agrupar hijos en layout vertical |
| `text`      | 9        | Texto estático y bindings        |
| `loop`      | 1        | Iterar sobre la lista de items   |

---

## Datos de Ejemplo

```csharp
public static Dictionary<string, object> GetSampleData() => new()
{
    ["storeName"] = "MI NEGOCIO",                    // → {{storeName}}
    ["fecha"]     = DateTime.Now.ToString("..."),    // → {{fecha}}
    ["items"]     = new List<Dictionary<string, object>>  // → loop source
    {
        new() { ["nombre"] = "Producto 1", ["cantidad"] = "2",
                ["precio"] = "15.50", ["total"] = "31.00" },
        new() { ["nombre"] = "Producto 2", ["cantidad"] = "1",
                ["precio"] = "25.00", ["total"] = "25.00" },
        new() { ["nombre"] = "Producto 3", ["cantidad"] = "3",
                ["precio"] = "8.75",  ["total"] = "26.25" },
    },
    ["subtotal"] = "82.25",                          // → {{subtotal}}
    ["impuesto"] = "13.16",                          // → {{impuesto}}
    ["total"]    = "95.41",                          // → {{total}}
    ["footer"]   = "Gracias por su compra!",         // → {{footer}}
};
```

> **Nota:** Los datos son `Dictionary<string, object>`, el formato más simple para el motor. También se pueden usar objetos anónimos o clases tipadas.

---

## Flujo Completo Paso a Paso

### 1. Registro del motor (arranque)

```
MauiProgram.cs → AddMotorDslEngine()
                    ├── IDslParser        → DslParser
                    ├── IEvaluator        → Evaluator
                    ├── ILayoutEngine     → LayoutEngine
                    ├── IRendererRegistry → TextRenderer + EscPosRenderer
                    ├── IDataValidator    → DataValidator
                    ├── ITemplateValidator → TemplateValidator
                    ├── IProfileValidator → ProfileValidator
                    └── IDocumentEngine   → DocumentEngine
                 .AddTemplates()  → registra "ticket-venta"
                 .AddProfiles()   → registra "thermal_58mm" (32 cols, escpos)
```

### 2. Preview (botón "Vista Previa")

```
OnPreviewClicked()
    │
    ├── Crea DeviceProfile("thermal_58mm", 32, "text")
    │                                             ↑ target = texto plano
    ├── _engine.Render(template, data, profile)
    │       │
    │       ├── Parse → DocumentTemplate
    │       ├── Validate → OK
    │       ├── Evaluate → resuelve bindings
    │       ├── Layout → aplica columnas
    │       └── Render(TextRenderer) → string
    │
    └── OutputLabel.Text = result.Output
```

### 3. Hex Dump (botón "ESC/POS")

```
OnHexDumpClicked()
    │
    ├── Crea DeviceProfile("thermal_58mm", 32, "escpos")
    │                                             ↑ target = ESC/POS
    ├── _engine.Render(template, data, profile)
    │       └── Render(EscPosRenderer) → byte[]
    │
    └── Muestra hex dump formateado en OutputLabel
        Ejemplo: "1B 40 1B 74 00 ..."  (Init ESC @ + selección CP437 ESC t 0)
```

### 4. Vista Previa MAUI (botón "MAUI Preview")

```
OnMauiPreviewClicked()
    │
    ├── _engine.RenderLayout(template, data, profile)
    │       └── Retorna LayoutedDocument (sin renderizar)
    │
    └── MauiPreview.Document = layouted
        → MauiDocumentPreview renderiza los nodos como Views
```

### 5. Impresión BT (botón "Imprimir")

```
OnPrintClicked()
    │
    ├── _engine.Render(template, data, escposProfile)
    │       └── byte[] con comandos ESC/POS
    │
    ├── _printerService.SendBytesAsync(bytes, PrinterProfile)
    │       └── Envía via Bluetooth
    │
    └── Manejo de errores con reintentos
```

---

## Código Clave Comentado

### MauiProgram.cs — Registro del motor

```csharp
// Una sola línea registra todo el motor con sus dependencias
builder.Services.AddMotorDslEngine()
    .AddTemplates(t =>
    {
        t.Add("ticket-venta", TicketDsl.Template);  // Registra el template
    })
    .AddProfiles(p =>
    {
        p.Add(new DeviceProfile("thermal_58mm", 32, "escpos"));  // 58mm, 32 cols
    });
```

### OnPreviewClicked — Renderizado a texto

```csharp
private void OnPreviewClicked(object sender, EventArgs e)
{
    // Profile con target "text" → usa TextRenderer
    var profile = new DeviceProfile("thermal_58mm", 32, "text");

    // El engine ejecuta el pipeline completo
    var result = _engine.Render(TicketDsl.Template, TicketDsl.GetSampleData(), profile);

    if (result.IsSuccessful)
        OutputLabel.Text = result.Output?.ToString() ?? "(vacío)";
    else
        OutputLabel.Text = "ERRORES:\n" + string.Join("\n", result.Errors);
}
```

### OnPrintClicked — Impresión BT

```csharp
private async void OnPrintClicked(object sender, EventArgs e)
{
    // Profile con target "escpos" → usa EscPosRenderer → byte[]
    var profile = new DeviceProfile("thermal_58mm", 32, "escpos");
    var result = _engine.Render(TicketDsl.Template, TicketDsl.GetSampleData(), profile);

    if (result.IsSuccessful && result.Output is byte[] bytes)
    {
        // Enviar los bytes crudos a la impresora
        await _printerService.SendBytesAsync(bytes, PrinterProfile.Thermal58mm);
        ShowMessage("Ticket impreso correctamente");
    }
}
```

---

## Qué Aprender de Este Ejemplo

| Concepto                       | Dónde verlo                          |
|--------------------------------|--------------------------------------|
| Registro DI con fluent API     | `MauiProgram.cs` líneas 27-35       |
| Template DSL con bindings      | `TicketDsl.cs` — `{{campo}}`        |
| Loop sobre colecciones         | Template: `"type": "loop"`          |
| Cambiar target cambia renderer | `"text"` vs `"escpos"` en Profile   |
| `RenderResult` con errores     | `result.IsSuccessful`, `.Errors`    |
| `RenderLayout()` para MAUI     | `OnMauiPreviewClicked()`            |
| Impresión BT real              | `OnPrintClicked()` → `SendBytesAsync` |

---

## Próximos Pasos

Este ejemplo cubre lo básico. Para funcionalidades avanzadas:

→ **[Ejemplo 02 — MotorDsl.MultaApp](ejemplo-02-multa.md)**
- Imágenes (logo base64, firma)
- Tablas con columnas
- Código QR
- Conditional nodes
- Renderizado PDF
- Exportación vía API REST
- Validación formal de template y profile
