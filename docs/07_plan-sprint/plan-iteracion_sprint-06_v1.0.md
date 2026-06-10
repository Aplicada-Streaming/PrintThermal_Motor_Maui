# 📄 Plan de Iteración — Sprint 06
**Archivo:** plan-iteracion_sprint-06_v1.0.md  
**Versión:** 1.0  
**Fecha:** 2026-06-10

## 📋 Información general

* Proyecto: Motor DSL para generación y renderizado de documentos (ESC/POS / PDF / Preview)
* Sprint: 06
* Duración: 2 semanas
* Fecha inicio: 10/06/2026
* Fecha fin: 23/06/2026
* Objetivo del sprint: Validación de datos, error handling BT robusto, barcode EAN-13 y vista previa MAUI nativa.

---

## 🎯 Objetivo del Sprint

Consolidar la **robustez del motor** y ampliar las **capacidades de renderizado**, permitiendo:

* Validar la estructura de datos contra la plantilla DSL antes de evaluar
* Clasificar y manejar errores de impresión Bluetooth con reintentos configurables
* Generar códigos de barra EAN-13 en EscPosRenderer (`GS k`)
* Visualizar documentos en vista previa MAUI nativa sin depender de impresora física

👉 Este sprint habilita **CU-19** (Validar estructura de datos), **CU-32** (Manejar errores de impresión), mejora **CU-06** (Barcode EAN-13) y concreta **CU-08** (Vista previa UI nativa en SampleApp).

---

## 🧩 Historias / tareas comprometidas

| ID    | Tipo     | Descripción                                                                          | Prioridad | Estimación |
| ----- | -------- | ------------------------------------------------------------------------------------ | --------- | ---------- |
| US-21 | Historia | Como desarrollador, quiero validar los datos contra la plantilla para detectar errores antes de evaluar | Alta      | 8 pts      |
| US-22 | Historia | Como usuario, quiero que los errores de impresión BT se manejen con reintentos automáticos | Alta      | 8 pts      |
| US-23 | Historia | Como desarrollador, quiero generar barcodes EAN-13 en EscPosRenderer                | Alta      | 5 pts      |
| US-24 | Historia | Como usuario, quiero ver una vista previa del documento en la app MAUI antes de imprimir | Media     | 8 pts      |
| TK-36 | Técnica  | Definir `IDataValidator` + implementación: extraer referencias del AST y validar existencia en datos | Alta      | 5 pts      |
| TK-37 | Técnica  | Validar tipos: loops requieren IEnumerable, condicionales requieren boolean-compatible | Alta      | 5 pts      |
| TK-38 | Técnica  | `ValidationResult` con lista de errores tipados (MissingField, TypeMismatch, InvalidStructure) | Alta      | 3 pts      |
| TK-39 | Técnica  | Integrar validación en pipeline: opción de validar antes de evaluar                  | Media     | 3 pts      |
| TK-40 | Técnica  | Definir `IPrintErrorHandler` + modelo `PrintError` con clasificación (Connection, Timeout, Hardware, Protocol) | Alta      | 5 pts      |
| TK-41 | Técnica  | Implementar retry policy configurable con backoff exponencial en `ThermalPrinterService` | Alta      | 8 pts      |
| TK-42 | Técnica  | BT reconnection logic: detectar desconexión, reintentar conexión antes de reenviar   | Alta      | 5 pts      |
| TK-43 | Técnica  | Barcode EAN-13: constantes `GS k` en EscPosCommands + LayoutEngine branch para barcode | Alta      | 5 pts      |
| TK-44 | Técnica  | Barcode EAN-13: emisión en EscPosRenderer + placeholder en TextRenderer              | Alta      | 5 pts      |
| TK-45 | Técnica  | SampleApp: crear `MauiDocumentPreview` — genera `View` (StackLayout + Labels) desde LayoutedDocument | Media     | 8 pts      |
| TK-46 | Técnica  | SampleApp: preview con estilos (bold, alignment, font size) + TableNode como Grid    | Media     | 5 pts      |
| TK-47 | Técnica  | Tests unitarios de validación, barcode y error handling                               | Alta      | 8 pts      |

---

## 🏗️ Alcance técnico del sprint

### CU-19 — Validación de datos contra plantilla (TK-36, TK-37, TK-38, TK-39)

* Nueva interfaz en `MotorDsl.Core/Contracts`:

```csharp
public interface IDataValidator
{
    ValidationResult Validate(DocumentNode ast, IDictionary<string, object> data);
}
```

* `ValidationResult` en `MotorDsl.Core.Models` (forma preliminar; la forma final del Sprint 07 incluye `NodeType`, `Severity`, `Location` y `IsValid` calculado):

```csharp
public class ValidationResult
{
    // IsValid es calculada: true si no hay errores con Severity == Error
    public bool IsValid => !Errors.Any(e => e.Severity == ValidationSeverity.Error);
    public List<ValidationError> Errors { get; } = new();
    public void AddError(string field, ValidationErrorType type, string message, string nodeType);
}

public class ValidationError
{
    public string Field { get; }
    public ValidationErrorType Type { get; }   // MissingField, TypeMismatch, InvalidStructure, ...
    public string Message { get; }
    public string NodeType { get; }
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
    public string? Location { get; init; } = null;
    // ctor de 4 parámetros:
    public ValidationError(string field, ValidationErrorType type, string message, string nodeType);
}
```

* Implementación `DataValidator` recorre el AST:
  * `TextNode` con expresión `{{campo}}` → valida que `data["campo"]` exista
  * `ConditionalNode.Condition` → valida que el campo referenciado sea boolean-compatible
  * `LoopNode.Collection` → valida que el campo referenciado sea `IEnumerable`
  * `TableNode.RowSource` → valida que el campo referenciado sea `IEnumerable`
* Integración opcional en pipeline: `IDocumentEngine.ValidateBeforeRender(template, data)` retorna `ValidationResult`

### CU-32 — Error handling BT robusto (TK-40, TK-41, TK-42)

* Modelo de error en `MotorDsl.Core/Models`:

```csharp
public enum PrintErrorType { Connection, Timeout, Hardware, Protocol, Unknown }

// PrintError es inmutable (props get-only + ctor de 5 parámetros + factory FromException)
public class PrintError
{
    public PrintErrorType Type { get; }
    public string Message { get; }
    public Exception? InnerException { get; }
    public int Attempt { get; }
    public int MaxAttempts { get; }
    public PrintError(PrintErrorType type, string message, Exception? innerException, int attempt, int maxAttempts);
    public static PrintError FromException(Exception ex, int attempt, int maxAttempts);
}
```

* Interfaz `IPrintErrorHandler` en `MotorDsl.Core.Contracts`:

```csharp
public interface IPrintErrorHandler
{
    Task<bool> HandleErrorAsync(PrintError error);  // true = retry, false = abort
    void OnRetryAttempt(PrintError error);
    void OnPrintSuccess(int totalAttempts);
}
```

* Retry policy en `ThermalPrinterService`. La implementación de referencia con backoff exponencial vive en `MotorDsl.Printing.Abstractions` (`ThermalPrinterService : IThermalPrinterService`, delay `InitialDelayMs * (1 << (attempt-1))`); los samples con `ProjectReference` usan un `ThermalPrinterService` local más simple.
  * Configurable: `MaxRetries` (default 3), `InitialDelayMs` (default 500)
  * Backoff exponencial: 500ms → 1000ms → 2000ms
  * Antes de cada reintento: verificar conexión BT, reconectar si necesario
  * Clasificar excepciones en `PrintErrorType`:
    * `IOException` / `SocketException` → Connection
    * `TimeoutException` → Timeout
    * Otros → Unknown
* Eventos: `OnPrintError`, `OnRetryAttempt`, `OnPrintSuccess` para que la UI reaccione

### Barcode EAN-13 (TK-43, TK-44)

* Constantes ESC/POS en `EscPosCommands.cs`:

```csharp
// GS k — Barcode EAN-13
public static readonly byte[] BarcodeEan13 = { 0x1D, 0x6B, 0x02 };  // GS k m (EAN13=2)
public static readonly byte[] BarcodeHriBelow = { 0x1D, 0x48, 0x02 };  // HRI below barcode
public static readonly byte[] BarcodeHeight72 = { 0x1D, 0x68, 0x48 };  // Height = 72 dots
public static readonly byte[] BarcodeWidth2 = { 0x1D, 0x77, 0x02 };    // Width multiplier = 2
```

* LayoutEngine — `ImageNode` con `imageType == "barcode"` o `"ean13"`:
  * `WrappedText = "[BARCODE: {source}]"`
  * `DeviceMetadata["is_barcode"] = true`
  * `DeviceMetadata["barcode_data"] = source`
  * `DeviceMetadata["barcode_type"] = "ean13"`

* EscPosRenderer — En el loop de entries:
  * Detectar `DeviceMetadata["is_barcode"]`
  * Emitir secuencia: HRI position → height → width → `GS k 2` + 13 bytes datos + NUL terminator
  * Validar que los datos sean 12-13 dígitos numéricos

* TextRenderer — Usa `WrappedText` directamente: `[BARCODE: 7790001234567]`

### CU-08 — Vista previa MAUI nativa en SampleApp (TK-45, TK-46)

**Decisión arquitectónica clave:** CU-08 NO se implementa como `IRenderer` en la librería `MotorDsl.Rendering`. Hacerlo crearía una dependencia directa de `Microsoft.Maui` en la librería core, violando el principio de desacople.

* Se implementa en `samples/MotorDsl.SampleApp/` como clase independiente:

```csharp
// samples/MotorDsl.SampleApp/Controls/MauiDocumentPreview.cs
public class MauiDocumentPreview
{
    public View BuildPreview(LayoutedDocument document)
    {
        // Recorre NodeLayoutInfo y genera View MAUI
    }
}
```

* **No implementa `IRenderer`** — recibe `LayoutedDocument` directamente del pipeline
* Mapeo de nodos a controles MAUI:
  * `WrappedText` → `Label` con `FontAttributes`, `HorizontalTextAlignment`
  * Bold (via DeviceMetadata) → `FontAttributes.Bold`
  * Alignment → `HorizontalTextAlignment.Start/Center/End`
  * Separadores → `BoxView` con altura 1
  * TableNode rows → `Grid` con columnas proporcionales
  * QR/Barcode → `Label` con placeholder `[QR: ...]` / `[BARCODE: ...]`
* Contenedor principal: `ScrollView` → `VerticalStackLayout`
* Estilo: fondo blanco, fuente monospace para simular ticket térmico

---

## 🔁 Pipeline completo con este sprint

```text
DSL JSON
 ↓
Parser (Sprint 01)
 ↓
AST (DocumentNode)
 ↓
╔══════════════════════════╗
║ IDataValidator (Sprint 06)║  ←── NUEVO: validación opcional
║ ValidationResult          ║      antes de evaluar
╚══════════════════════════╝
 ↓ (si válido)
Evaluator (Sprint 02)
 ↓
EvaluatedDocument
 ↓
LayoutEngine (Sprint 03)
 ↓
LayoutedDocument
 ↓
┌──────────────────────────────────────────────────────┐
│            RendererRegistry (Sprint 04)               │
├───────────────┬───────────────┬───────────────────────┤
│ TextRenderer  │ EscPosRenderer│ (Extensible por       │
│ + TableNode   │ + TableNode   │  cliente)             │
│ + Barcode plh │ + QR Code     │                       │
│ (Sprint 03-06)│ + Barcode EAN │                       │
│ → string      │ (Sprint 04-06)│                       │
│               │ → byte[]      │                       │
└───────────────┴───────┬───────┴───────────────────────┘
                        ↓
                   RenderResult
                   .Output
                   .ToHexString()
                   .ToBase64()
                        ↓
        ╔═══════════════════════════════════╗
        ║ ThermalPrinterService (Sprint 06) ║  ←── MEJORADO:
        ║ + Retry policy                    ║      reintentos,
        ║ + Error classification            ║      reconexión BT,
        ║ + BT reconnection                 ║      error handling
        ╚═══════════════════════════════════╝

LayoutedDocument ──→ MauiDocumentPreview (Sprint 06)  ←── NUEVO
                     → View (StackLayout + Labels)         en SampleApp
                     (NO es IRenderer)
```

---

## 🔧 Decisiones arquitectónicas del sprint

| Decisión | Fundamento |
| -------- | ---------- |
| **IDataValidator como contrato separado** | La validación es opcional y no todos los consumidores la necesitan. Mantenerla como interfaz inyectable permite reemplazarla o desactivarla. |
| **ValidationResult con errores tipados** | Clasificar errores (MissingField, TypeMismatch) permite UI específica y decisiones de flujo por parte del consumidor. |
| **IPrintErrorHandler como contrato** | Desacopla la política de error del servicio de impresión. El cliente puede implementar logging, UI, o reintentos personalizados. |
| **Retry con backoff exponencial** | Estándar de la industria para conexiones BT inestables. Evita saturar el dispositivo con reintentos inmediatos. |
| **Barcode con GS k nativo** | Sin dependencia de librería de barcode externa. Las impresoras térmicas generan el barcode internamente. |
| **MauiDocumentPreview en SampleApp, NO como IRenderer** | Implementar CU-08 como `IRenderer` requeriría referenciar `Microsoft.Maui` en `MotorDsl.Rendering`, creando dependencia de framework UI en la librería core. La librería debe permanecer agnóstica a la plataforma. El SampleApp consume `LayoutedDocument` directamente. |
| **Preview con controles nativos MAUI** | Labels + Grid + BoxView son suficientes para simular un ticket térmico. No se necesitan controles complejos ni renderizado custom. |

---

## 🔧 Capacidades habilitadas

**CU habilitados/mejorados por este sprint:**

* **CU-19:** Validar Estructura de Datos — `IDataValidator` + `DataValidator` + `ValidationResult`
* **CU-32:** Manejar Errores de Impresión — `IPrintErrorHandler` + `PrintError` + retry policy
* **CU-06 v3.0:** Renderizar ESC/POS — Barcode EAN-13 (además de QR del Sprint 05)
* **CU-08:** Renderizar Vista Previa UI — `MauiDocumentPreview` en SampleApp

**CU que quedan para sprints futuros:**

* **CU-14:** Validar Plantilla DSL — validación de esquema de la plantilla
* **CU-22:** Validar Perfil de Dispositivo — validación formal de DeviceProfile
* **CU-30/31:** Manejar Errores de Plantilla/Datos — clasificación formal

---

## 🚫 Fuera de alcance

* **CU-14** — Validar plantilla DSL (esquema de la plantilla, no de los datos)
* **CU-22** — Validación formal de DeviceProfile
* **Imágenes bitmap** ESC/POS — requiere rasterización
* **Preview QR/Barcode como imagen** en CU-08 — se muestra placeholder texto
* **Logging del pipeline** (BT-073) — no es crítico aún
* **Cache de templates** (BT-100) — optimización prematura
* **PDF renderer** (CU-09) — delegado al cliente como extensión

---

## ✅ Criterios de terminado (Definition of Done)

Una tarea se considera terminada cuando:

* `IDataValidator` está definido y `DataValidator` recorre TextNode, ConditionalNode, LoopNode, TableNode
* `ValidationResult` reporta errores tipados con campo, tipo y mensaje
* `IPrintErrorHandler` está definido con modelo `PrintError` clasificado
* `ThermalPrinterService` implementa retry con backoff exponencial (3 reintentos, 500ms base)
* Reconexión BT automática antes de cada reintento
* Barcode EAN-13 genera secuencia `GS k` correcta en EscPosRenderer
* TextRenderer muestra placeholder `[BARCODE: ...]`
* `MauiDocumentPreview` genera `View` MAUI desde `LayoutedDocument` con estilos
* Tests unitarios validan todas las funcionalidades nuevas
* No se altera funcionalidad existente (130 tests previos siguen pasando)
* La librería core NO tiene dependencia de `Microsoft.Maui`
* El código compila, pasa todos los tests y está integrado en el pipeline

---

## 🧪 Plan de pruebas del sprint

### Pruebas unitarias — Validación de datos (CU-19)

* `DataValidator` con datos completos y plantilla válida → `IsValid = true`, sin errores
* `DataValidator` con campo faltante en TextNode → error `MissingField`
* `DataValidator` con campo no-iterable en LoopNode → error `TypeMismatch`
* `DataValidator` con campo no-boolean en ConditionalNode → error `TypeMismatch`
* `DataValidator` con múltiples errores → todos reportados en `ValidationResult.Errors`
* `ValidationResult.IsValid` coherente con lista de errores

### Pruebas unitarias — Error handling (CU-32)

* `PrintError` se construye con tipo, mensaje y excepción
* Clasificación de excepciones: `IOException` → Connection, `TimeoutException` → Timeout
* Retry policy respeta `MaxRetries` y `InitialDelayMs`
* Reintento exitoso después de fallo transitorio → `OnPrintSuccess` se dispara
* Todos los reintentos fallidos → último error se propaga

### Pruebas unitarias — Barcode EAN-13

* Barcode con 13 dígitos → secuencia `GS k` correcta con datos + NUL
* Bytes generados contienen `GS k 0x02` (command) y los dígitos ASCII
* HRI position, height y width commands presentes
* TextRenderer muestra `[BARCODE: 7790001234567]`
* LayoutEngine asigna metadata `is_barcode`, `barcode_data`, `barcode_type`

### Pruebas unitarias — Preview MAUI (CU-08)

* `MauiDocumentPreview.BuildPreview()` con documento simple → retorna `View` no null
* Label generado contiene texto del WrappedText
* Bold metadata → `FontAttributes.Bold` aplicado
* Alignment center → `HorizontalTextAlignment.Center`
* Contenedor raíz es `ScrollView` con `VerticalStackLayout`

### Pruebas de integración

* Pipeline completo con validación → datos inválidos → no se renderiza, error informado
* Pipeline completo con barcode → EscPosRenderer → byte[] con GS k
* Pipeline completo con barcode → TextRenderer → string con placeholder
* Retry policy: simular fallo BT → reconexión → reenvío exitoso

---

## 🚀 Entregables del sprint

* `IDataValidator` + `DataValidator` en `MotorDsl.Core`
* `ValidationResult` + `ValidationError` en `MotorDsl.Core/Models`
* `IPrintErrorHandler` + `PrintError` en `MotorDsl.Core`
* `ThermalPrinterService` mejorado con retry + reconnection (implementación de referencia en `MotorDsl.Printing.Abstractions`; los samples con ProjectReference usan un service local)
* Constantes Barcode EAN-13 en `EscPosCommands`
* Barcode en LayoutEngine + EscPosRenderer + TextRenderer
* `MauiDocumentPreview` en `samples/MotorDsl.SampleApp/Controls/`
* Tests unitarios para validación, barcode, error handling y preview
* Documentación de decisiones arquitectónicas actualizada

---

## ⚠️ Riesgos identificados

| Riesgo | Probabilidad | Impacto | Mitigación |
| ------ | ------------ | ------- | ---------- |
| BT reconnection inestable en Android | Media | Alto | Testear en dispositivo real, timeout conservador, fallback a error claro |
| Variabilidad de soporte GS k entre impresoras | Baja | Medio | EAN-13 es el barcode más universalmente soportado en impresoras térmicas |
| MauiDocumentPreview: estilos MAUI inconsistentes entre plataformas | Baja | Bajo | Se implementa solo para Android (target actual) |
| Validación de datos incompleta (expresiones complejas) | Media | Medio | Sprint 06 valida referencias directas `{{campo}}`; expresiones complejas van en sprint futuro |

---

## 📊 Estimación por bloque

| Bloque | Story Points | % del Sprint |
|--------|-------------|-------------|
| CU-19: Validación datos vs plantilla (TK-36 a TK-39) | 16 pts | 25% |
| CU-32: Error handling BT + reintentos (TK-40 a TK-42) | 18 pts | 28% |
| Barcode EAN-13 (TK-43, TK-44) | 10 pts | 15% |
| CU-08: Vista previa MAUI en SampleApp (TK-45, TK-46) | 13 pts | 20% |
| Tests unitarios (TK-47) | 8 pts | 12% |
| **Total** | **65 pts** | **100%** |

---

## 🔄 Métricas de seguimiento

* Velocidad del equipo (comparar con Sprint 05: ~81 pts)
* Historias completadas vs comprometidas
* Cobertura de tests (target: 130 existentes + ~30 nuevos = ~160)
* Errores BT manejados correctamente en dispositivo real
* Validación detecta errores antes de runtime
* Barcode EAN-13 impreso correctamente en impresora térmica

---
