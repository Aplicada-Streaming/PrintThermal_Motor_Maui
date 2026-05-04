# Ejemplo 03 — MotorDsl.Integrated.MultaApp

> Acta de infracción de tránsito usando el **formato integrado** del DSL. Mismo caso real que el Ejemplo 02, pero con el JSON ya pre-resuelto.

---

## 1. Propósito y Audiencia

Aplicación avanzada que demuestra cómo usar MotorDsl con la **modalidad integrada** del DSL: el JSON ya viene con todos los valores resueltos, sin `{{placeholders}}`, sin `loop` y sin `conditional`. Dirigida al desarrollador que:

- Construye el documento completo en su propio backend, server-side o job batch.
- Necesita guardar snapshots inmutables del documento (auditoría, reimpresión idéntica).
- Quiere evitar mantener el diccionario de datos en sincronía con el template.
- Recibe documentos ya armados desde un servicio REST/gRPC y solo debe rasterizarlos.

**Nivel:** Avanzado
**Ubicación:** `samples/MotorDsl.Integrated.MultaApp/`

---

## 2. Funcionalidades

| Feature                    | Descripción                                              |
|----------------------------|----------------------------------------------------------|
| Preview MAUI               | Vista previa nativa del acta en pantalla                 |
| Hex dump ESC/POS           | Visualización de los comandos ESC/POS generados          |
| PDF                        | Generación y vista previa de PDF (renderer custom)       |
| Impresión térmica con logo | Imprimir en impresora BT con imagen rasterizada          |
| Código QR                  | QR de pago con URL ya resuelta (sin placeholders)        |
| Validación formal          | Template y profile validados antes del render            |
| **Modo integrado**         | `engine.Render(json, profile)` — sin diccionario de datos|

---

## 3. Diferencia con Ejemplo 02 (MultaApp clásico)

| Aspecto                | Ejemplo 02 — MultaApp                | Ejemplo 03 — Integrated.MultaApp              |
|------------------------|--------------------------------------|-----------------------------------------------|
| Modalidad DSL          | Template + Data separados            | JSON integrado (`"format": "integrated"`)     |
| Templates registrados  | 3 (`MultaDsl`, `TicketSimple`, `Comprobante`) | 1 (`MultaIntegratedDsl.Document`)     |
| Llamada al motor       | `engine.Render(template, data, profile)` | `engine.Render(integratedJson, profile)`  |
| Pipeline interno       | Parse → Validate → **Evaluate** → Layout → Render | Parse → Validate → Layout → Render      |
| Diccionario de datos   | sí, requerido                        | no aplica                                     |
| ApplicationId          | `com.motordsl.multaapp`              | `com.motordsl.integrated.multaapp`            |
| Namespace              | `MotorDsl.MultaApp.*`                | `MotorDsl.Integrated.MultaApp.*`              |

---

## 4. JSON integrado de la multa

El template está en [`Templates/MultaIntegratedDsl.cs`](../../samples/MotorDsl.Integrated.MultaApp/Templates/MultaIntegratedDsl.cs) como un único string `Document`. Resumen de cómo se construyó:

1. **Discriminador** `"format": "integrated"` en la raíz.
2. **TextNodes** usan `value` (no `text`) con el contenido ya resuelto: `"value": "ACTA N° 2026-00123"` en lugar de `"text": "ACTA N° {{nroActa}}"`.
3. **Loops expandidos** manualmente: el `loop` original sobre `infracciones` se transformó en N `container` concretos, uno por infracción del sample data.
4. **Imágenes con source completo**: el QR contiene `"https://multas.ejemplo.gob.ar/pago/2026-00123"` (no `"https://.../{{nroActa}}"`); las imágenes bitmap traen su base64 embebido.

Fragmento ilustrativo:

```json
{
  "id": "acta-infraccion-001",
  "version": "1.0",
  "format": "integrated",
  "root": {
    "type": "container",
    "layout": "vertical",
    "children": [
      {
        "type": "image",
        "source": "data:image/bmp;base64,Qk0+KQAAA...",
        "imageType": "bitmap",
        "width": 200,
        "style": { "align": "center" }
      },
      {
        "type": "text",
        "value": "MUNICIPALIDAD DE EJEMPLO",
        "style": { "align": "center", "bold": true }
      },
      {
        "type": "text",
        "value": "ACTA DE INFRACCIÓN N°: 2026-00123",
        "style": { "bold": true }
      },
      { "type": "text", "value": "Fecha: 31/03/2026  Hora: 14:35" },
      {
        "type": "container",
        "layout": "vertical",
        "children": [
          { "type": "text", "value": "Art. 77 inc. 2 - Exceso de velocidad" },
          { "type": "text", "value": "Puntos: 3  Monto: $15000" }
        ]
      },
      {
        "type": "container",
        "layout": "vertical",
        "children": [
          { "type": "text", "value": "Art. 48 - Uso de teléfono celular" },
          { "type": "text", "value": "Puntos: 2  Monto: $8000" }
        ]
      },
      {
        "type": "image",
        "source": "https://multas.ejemplo.gob.ar/pago/2026-00123",
        "imageType": "qrcode"
      }
    ]
  }
}
```

---

## 5. Llamada al motor

```csharp
public partial class MainPage : ContentPage
{
    private readonly IDocumentEngine _engine;

    public MainPage(IDocumentEngine engine, IThermalPrinterService printer)
    {
        InitializeComponent();
        _engine = engine;
    }

    private void OnVistaPreviewClicked(object? sender, EventArgs e)
    {
        // El JSON ya está pre-resuelto — no hace falta diccionario de datos.
        var profile = new DeviceProfile("thermal_58mm", 32, "text");
        var result = _engine.Render(MultaIntegratedDsl.Document, profile);

        if (result.IsSuccessful)
            PreviewLabel.Text = result.Output?.ToString() ?? "(vacío)";
    }

    private async void OnImprimirClicked(object? sender, EventArgs e)
    {
        var profile = new DeviceProfile("58HB6", 32, "escpos-bitmap");
        profile.SetCapability("supports_bitmap", true);
        profile.SetCapability("bitmap_max_width_px", 320);

        var result = _engine.Render(MultaIntegratedDsl.Document, profile);

        if (result.IsSuccessful && result.Output is byte[] bytes)
            await _printer.SendBytesAsync(bytes);
    }
}
```

> Compará con el Ejemplo 02: la única diferencia visible es que la llamada `Render(template, data, profile)` se reemplaza por `Render(json, profile)`. Todo el resto de la app (impresión BT, PDF, preview, capability degradation) funciona exactamente igual.

---

## 6. Cuándo conviene este formato

| Escenario | Formato recomendado |
|---|---|
| Documento parametrizable con datos del usuario | Clásico (Ejemplo 02) |
| Documento ya armado por un backend o un job batch | **Integrado (este ejemplo)** |
| Templates reutilizables con bindings dinámicos | Clásico |
| Snapshots inmutables (auditoría, reimpresión idéntica) | **Integrado** |
| Lógica condicional o iteraciones declarativas | Clásico |
| Producer-consumer con AST serializado entre servicios | **Integrado** |

---

## 7. Validaciones específicas

`TemplateValidator` aplica reglas extra cuando detecta `"format": "integrated"`:

| Regla | Tipo de error |
|---|---|
| Aparece un nodo `loop` o `conditional` | `UnsupportedInIntegratedFormat` |
| El `value` de un `text` contiene `{{...}}` | `UnresolvedPlaceholder` |
| El `source` de un `image` contiene `{{...}}` | `UnresolvedPlaceholder` |

Estos errores se devuelven en `RenderResult.Errors` con detalle del path donde se detectaron.

---

## 8. Cómo ejecutar

```bash
cd samples/MotorDsl.Integrated.MultaApp
dotnet build -f net10.0-android
dotnet build -f net10.0-ios     # macOS only
```

Requiere las mismas dependencias que [Ejemplo 02](ejemplo-02-multa.md): `.NET 10`, MAUI workload, Android Bluetooth para impresión.

---

## 9. Relación con la documentación

| Documento                                                  | Relación                                  |
|------------------------------------------------------------|-------------------------------------------|
| `docs/10_developer_guide/formato-dsl-templates.md`         | Sección 9: especificación del formato integrado |
| `docs/05_arquitectura_tecnica/contratos-del-motor_v1.0.md` | Overload `IDocumentEngine.Render(string, DeviceProfile)` |
| `docs/05_arquitectura_tecnica/flujo-ejecucion-motor_v1.0.md` | Sección 21: flujo alternativo simplificado |
| `docs/11_examples/ejemplo-02-multa.md`                     | Versión clásica del mismo caso de uso     |
