# Caso de Uso: Renderizar Documento a PDF

**Código:** CU-09
**Archivo:** CU-09-renderizar-pdf_v2.0.md
**Versión:** 2.0
**Estado:** ⚠️ FUERA DEL ALCANCE DE `MotorDsl.Core` (provisto por `MotorDsl.Maui`)
**Fecha:** 2026-05-27
**Autor:** Equipo Funcional / Arquitectura

---

# 1. Propósito

Este caso de uso describe la generación de documentos PDF a partir de la representación abstracta del motor DSL.

> **Decisión v2.0:** La generación de PDF queda **fuera del alcance de la librería core (`MotorDsl.Core`)** del motor DSL. El motivo principal es que cualquier implementación de PDF requiere dependencias de terceros (iTextSharp, QuestPDF, PdfSharpCore, etc.) que no deben formar parte del núcleo del motor.
>
> **Importante:** Esta exclusión aplica **solo a `MotorDsl.Core`**, no a la librería completa. El paquete **`MotorDsl.Maui` SÍ provee de fábrica un `PdfRenderer` real** (`PdfRenderer : IRenderer` con `Target => "pdf"`, basado en **PdfSharpCore 1.3.67**), que se registra automáticamente vía `builder.AddRenderer<PdfRenderer>()` dentro de `AddMotorDslMaui()`. El cliente que use `MotorDsl.Maui` obtiene PDF sin implementar nada. El ejemplo de implementación propia que sigue es una **alternativa genérica para clientes que NO usan `MotorDsl.Maui`**.

---

# 2. Motivo de exclusión (de `MotorDsl.Core`)

| Aspecto | Detalle |
|---------|---------|
| **Dependencias externas** | Todas las librerías PDF .NET son paquetes de terceros con licencias variadas (AGPL, comercial, MIT). El motor core (`MotorDsl.Core`) debe mantenerse libre de dependencias externas. La dependencia de PDF (PdfSharpCore) vive en `MotorDsl.Maui`, no en el core. |
| **Complejidad** | El renderizado PDF involucra coordenadas absolutas, gestión de páginas, fuentes embebidas y manejo de imágenes que exceden el alcance de un renderer estándar. |
| **Variabilidad** | Cada cliente puede tener preferencias distintas de librería PDF según requisitos de licencia, rendimiento o compatibilidad. Por eso el core no impone una; el cliente puede usar el `PdfRenderer` de `MotorDsl.Maui` o implementar el suyo. |
| **Principio de extensibilidad** | El motor ya provee `IRenderer` + `IRendererRegistry`, lo cual permite que `MotorDsl.Maui` aporte su `PdfRenderer` y que cualquier cliente implemente su propio renderer PDF sin modificar la librería core. |

---

# 3. Opción recomendada: `PdfRenderer` de `MotorDsl.Maui`

El paquete **`MotorDsl.Maui`** incluye `PdfRenderer : IRenderer` (`Target => "pdf"`) basado en **PdfSharpCore**. Se registra automáticamente con `AddMotorDslMaui()`; no requiere implementar nada:

```csharp
// En la configuración del cliente MAUI
builder.Services
    .AddMotorDslEngine()
    .AddMotorDslMaui();   // registra PdfRenderer (Target "pdf"), entre otros

// Uso (perfil con RenderTarget = "pdf")
var profile = new DeviceProfile("PDF-A4", 80, "pdf");
var result = engine.Render(templateDsl, data, profile);

if (result.IsSuccessful)
{
    var pdfBytes = (byte[])result.Output;
    File.WriteAllBytes("ticket.pdf", pdfBytes);
}
```

---

# 3-bis. Alternativa: Implementación por el cliente (sin `MotorDsl.Maui`)

Si el sistema consumidor **no** usa `MotorDsl.Maui`, puede implementar su propio renderer PDF y registrarlo vía `IRendererRegistry`. El motor procesará el pipeline completo y delegará la generación de bytes PDF al renderer del cliente.

## 3.1 Ejemplo de implementación (librería PDF genérica del cliente)

> El siguiente ejemplo es **ilustrativo y genérico** (usa una API PDF hipotética del cliente). El `PdfRenderer` de fábrica de `MotorDsl.Maui` usa **PdfSharpCore**, no QuestPDF. Nótese el uso de la API REAL de `RenderResult` (constructor + `AddError`), de `LayoutedDocument.NodeLayoutInfo` (no existe `Lines`) y del constructor de `DeviceProfile`.

```csharp
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;
// using <librería PDF del cliente: PdfSharpCore, QuestPDF, iTextSharp, etc.>

public class PdfRenderer : IRenderer
{
    public string Target => "pdf";

    public RenderResult Render(LayoutedDocument document, DeviceProfile profile)
    {
        var result = new RenderResult("pdf");
        try
        {
            // Líneas ordenadas según el layout calculado por el motor.
            var lines = document.NodeLayoutInfo.Values
                .OrderBy(info => info.LineNumber)
                .ThenBy(info => info.ColumnNumber)
                .Select(info => info.WrappedText);

            byte[] pdfBytes = GeneratePdf(lines);   // API PDF del cliente

            result.Output = pdfBytes;               // byte[] del PDF
            return result;                          // IsSuccessful == true (sin errores)
        }
        catch (Exception ex)
        {
            result.AddError(ex.Message);            // IsSuccessful pasa a false
            return result;
        }
    }
}
```

## 3.2 Registro del renderer

```csharp
// En la configuración del cliente
services.AddMotorDslEngine();
services.AddSingleton<IRenderer, PdfRenderer>();

// El renderer se incorpora al IRendererRegistry
```

## 3.3 Uso

```csharp
var profile = new DeviceProfile("PDF-A4", 80, "pdf");

var result = engine.Render(templateDsl, data, profile);

if (result.IsSuccessful)
{
    var pdfBytes = (byte[])result.Output;
    File.WriteAllBytes("ticket.pdf", pdfBytes);
}
```

---

# 4. Pipeline del motor con renderer PDF

```text
DSL JSON → Parser → AST → Evaluator → EvaluatedDocument
  → LayoutEngine → LayoutedDocument
    → RendererRegistry.GetRenderer("pdf")
      → PdfRenderer (de MotorDsl.Maui, o implementación del CLIENTE)
        → RenderResult { Output = byte[] (PDF) }
```

El motor ejecuta todo el pipeline y solo delega el paso final de renderizado al renderer con `Target == "pdf"` (el de `MotorDsl.Maui` o el registrado por el cliente).

---

# 5. Criterios de Aceptación (para la extensibilidad)

## CA-01 Registro de renderer externo

**Dado** un cliente que implementa `IRenderer` con `Target => "pdf"`
**Cuando** lo registra vía DI
**Entonces** `IRendererRegistry` lo resuelve correctamente con `GetRenderer("pdf")`.

---

## CA-02 Pipeline completo con renderer PDF

**Dado** un renderer PDF registrado
**Cuando** se ejecuta `engine.Render()` con `RenderTarget = "pdf"`
**Entonces** el motor ejecuta el pipeline completo y delega al `PdfRenderer` del cliente.

---

## CA-03 Sin dependencia PDF en `MotorDsl.Core`

**Dado** la librería core del motor (`MotorDsl.Core`)
**Cuando** se inspecciona
**Entonces** no contiene dependencias de librerías PDF (la dependencia PdfSharpCore vive en `MotorDsl.Maui`).

---

# 6. Cambios respecto a versión 1.0

* **Estado:** Cambiado de "Futuro" a "Fuera del Alcance de `MotorDsl.Core`" (pero provisto por `MotorDsl.Maui`).
* **Decisión arquitectónica:** Se documenta formalmente que `MotorDsl.Core` no incluye un renderer PDF, mientras que `MotorDsl.Maui` SÍ aporta `PdfRenderer` (Target "pdf", PdfSharpCore) de fábrica vía `AddMotorDslMaui()`.
* **Alternativa:** Se documenta cómo un cliente que no use `MotorDsl.Maui` puede implementar su propio `PdfRenderer` usando cualquier librería PDF.
* **Ejemplo:** Se incluye ejemplo ilustrativo genérico con la API real de `RenderResult`/`LayoutedDocument`/`DeviceProfile`.

---

# 7. Notas

La decisión de excluir PDF del **core (`MotorDsl.Core`)** es consistente con el principio de diseño del motor: el núcleo provee contratos y el pipeline, mientras que las implementaciones específicas de infraestructura o tecnología viven en paquetes adicionales o en el sistema consumidor. El paquete `MotorDsl.Maui` aprovecha justamente el mecanismo `IRenderer` + `IRendererRegistry` para aportar `PdfRenderer` (PdfSharpCore) sin tocar el núcleo, y el mismo mecanismo permite que un cliente registre su propio renderer PDF.

---

**Fin del documento**
