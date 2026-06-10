# Conceptos Fundamentales

## Qué es MotorDsl

**MotorDsl** es una librería .NET que transforma plantillas DSL (JSON) + datos dinámicos en documentos renderizados para impresoras térmicas, vista previa en texto plano u otros formatos.

La librería recibe tres entradas — una plantilla DSL, un diccionario de datos y un perfil de dispositivo — y produce un `RenderResult` con la salida lista para enviar a la impresora o mostrar en pantalla. Todo el proceso ocurre en memoria, sin dependencias de red, UI o hardware.

El diseño sigue el principio de **librería pura**: MotorDsl define contratos (`ITemplateProvider`, `IDataProvider`, `IDeviceProfileProvider`, `IRenderer`) y el sistema consumidor inyecta las implementaciones que necesite. Esto permite integrar el motor en cualquier tipo de aplicación .NET sin acoplamiento.

---

## El Pipeline

```
┌──────────────────────────────────────────────────────────────────────┐
│                         ENTRADAS                                     │
│  DSL JSON (string)  +  Data (dictionary)  +  DeviceProfile           │
└──────────┬───────────────────┬─────────────────────┬─────────────────┘
           │                   │                     │
           ▼                   │                     │
   ┌───────────────┐           │                     │
   │    Parser      │           │                     │
   │  (IDslParser)  │           │                     │
   └───────┬───────┘           │                     │
           │ AST               │                     │
           ▼                   ▼                     │
   ┌───────────────────────────────┐                 │
   │         Evaluator             │                 │
   │  (IEvaluator)                 │                 │
   │  Resuelve bindings, loops,    │                 │
   │  condicionales                │                 │
   └───────────┬───────────────────┘                 │
               │ EvaluatedDocument                   │
               ▼                                     ▼
   ┌─────────────────────────────────────────────────────┐
   │                 LayoutEngine                         │
   │  (ILayoutEngine)                                     │
   │  Ajusta anchos, alineación según DeviceProfile       │
   └───────────────────────┬─────────────────────────────┘
                           │ LayoutedDocument
                           ▼
   ┌─────────────────────────────────────────────────────┐
   │              RendererRegistry                        │
   │  (IRendererRegistry)                                 │
   │  Selecciona renderer según profile.RenderTarget      │
   └───────────────────────┬─────────────────────────────┘
                           │
              ┌────────────┴────────────┐
              ▼                         ▼
   ┌──────────────────┐     ┌──────────────────┐
   │  TextRenderer     │     │  EscPosRenderer   │
   │  Target: "text"   │     │  Target: "escpos"  │
   │  Output: string   │     │  Output: byte[]    │
   └────────┬─────────┘     └────────┬──────────┘
            │                        │
            └───────────┬────────────┘
                        ▼
              ┌──────────────────┐
              │   RenderResult    │
              │  .Output          │
              │  .IsSuccessful    │
              │  .ToHexString()   │
              │  .ToBase64()      │
              └──────────────────┘
```

---

## Etapas del pipeline

| Etapa | Componente | Qué hace |
|-------|-----------|----------|
| **1. Parse** | `IDslParser` | Convierte el JSON DSL en un árbol de nodos (AST) con `DocumentTemplate`. |
| **2. Evaluate** | `IEvaluator` | Resuelve bindings (`{{variable}}`), ejecuta loops y evalúa condicionales contra los datos. |
| **3. Layout** | `ILayoutEngine` | Ajusta anchos de línea, alineación y estructura según el `DeviceProfile` (ej. 32 chars para 58mm). |
| **4. Render** | `IRenderer` | Transforma el documento layouteado en la salida final: `string` (texto) o `byte[]` (ESC/POS). |

El motor orquesta todas las etapas a través de `IDocumentEngine.Render()` — el consumidor solo necesita llamar un método.

---

## Conceptos clave

| Concepto | Qué es | Ejemplo |
|----------|--------|---------|
| **Template** | Plantilla JSON DSL que define la estructura del documento. Contiene nodos, bindings y estilos. | `{ "type": "text", "text": "{{storeName}}", "style": { "align": "center" } }` |
| **Data** | Diccionario de datos que alimenta los bindings de la plantilla. | `{ "storeName": "Mi Tienda", "total": 95.41 }` |
| **Profile** | `DeviceProfile` que define las capacidades del dispositivo destino (ancho, target de render). | `new DeviceProfile("58mm", 32, "escpos")` |
| **Renderer** | Implementación de `IRenderer` que genera la salida final en un formato específico. | `TextRenderer` → `string`, `EscPosRenderer` → `byte[]` |
| **Binding** | Expresión `{{ruta}}` dentro del texto de un nodo que se reemplaza por el valor del dato correspondiente. | `"Total: ${{total}}"` → `"Total: $95.41"` |

---

## Cuándo usar cada Renderer

### TextRenderer (`"text"`)

- **Para qué:** depuración, vista previa en consola, logs, tests.
- **Output:** `string` con texto plano formateado según el ancho del perfil.
- **Cuándo usarlo:**
  - Durante el desarrollo, para verificar que el template genera el contenido esperado.
  - En tests automatizados donde se necesita comparar strings.
  - Para generar una vista previa rápida sin hardware.

### EscPosRenderer (`"escpos"`)

- **Para qué:** impresión en impresoras térmicas compatibles con ESC/POS.
- **Output:** `byte[]` con comandos binarios ESC/POS (inicialización, texto, estilos, corte de papel).
- **Cuándo usarlo:**
  - Para enviar directamente a una impresora Bluetooth/USB.
  - Cuando se necesitan estilos nativos (bold, alineación por hardware, QR por `GS ( k`).
  - Para almacenar o transmitir el ticket (via `ToBase64()` o `ToHexString()`).

### Cómo elegir

```
¿Vas a imprimir en una térmica?
  → Sí → EscPosRenderer ("escpos")
  → No → ¿Es para debug/preview/tests?
           → Sí → TextRenderer ("text")
           → No → Implementá tu propio IRenderer
```

---

## Qué NO hace la librería

MotorDsl es una **librería de transformación de documentos**. No maneja infraestructura, hardware ni lógica de negocio.

| No hace | Responsabilidad de |
|---------|--------------------|
| Conectarse por Bluetooth a impresoras | La app consumidora (ej. Android BT API) |
| Descargar plantillas desde APIs REST | El cliente implementa `ITemplateProvider` con su propia lógica HTTP |
| Descargar datos desde APIs o bases de datos | El cliente implementa `IDataProvider` con su propia fuente |
| Generar PDF (en el core) | El core no genera PDF, pero el paquete `MotorDsl.Maui` ya provee `PdfRenderer` (PdfSharpCore, `Target = "pdf"`). El cliente solo implementa su propio `IRenderer` si no usa `MotorDsl.Maui`. |
| Validar datos de negocio | La app consumidora valida antes de llamar a `Render()` |
| Manejar permisos de Android (BT, storage) | La app MAUI con el framework de permisos correspondiente |
| Cachear plantillas o datos | El cliente decide su estrategia de cache |
| Reintentar operaciones fallidas | La app consumidora maneja reintentos |

---

## Extensión: cómo agregar tu propio Renderer

Para soportar un nuevo formato de salida, implementá `IRenderer` y registralo en DI:

```csharp
public class HtmlRenderer : IRenderer
{
    public string Target => "html";

    public RenderResult Render(LayoutedDocument document, DeviceProfile profile)
    {
        var html = "<html><body>";
        // Recorrer document.NodeLayoutInfo (ordenado por LineNumber/ColumnNumber) y generar HTML...
        html += "</body></html>";
        return new RenderResult("html", html);
    }
}
```

Registro:

```csharp
services.AddSingleton<IRenderer, HtmlRenderer>();
```

Uso:

```csharp
var profile = new DeviceProfile("web", 80, "html");
var result = engine.Render(dslJson, data, profile);
// result.Output es el string HTML
```

El `RendererRegistry` descubre automáticamente todos los `IRenderer` registrados en DI y los resuelve por `Target`.
