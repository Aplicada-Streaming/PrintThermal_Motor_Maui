# 📄 Plan de Iteración — Sprint 04
**Archivo:** plan-iteracion_sprint-04_v1.0.md  
**Versión:** 1.0  
**Fecha:** 2026-05-13

## 📋 Información general

* Proyecto: Motor DSL para generación y renderizado de documentos (ESC/POS / PDF / Preview)
* Sprint: 04
* Duración: 2 semanas
* Fecha inicio: 13/05/2026
* Fecha fin: 26/05/2026
* Objetivo del sprint: Implementar EscPosRenderer (generación de bytes ESC/POS), selección dinámica de renderers, extensibilidad por DI y app MAUI de ejemplo.

---

## 🎯 Objetivo del Sprint

Incorporar el **primer renderer productivo** del motor y habilitar la **extensibilidad real**, permitiendo:

* Generar secuencias de bytes ESC/POS desde un `LayoutedDocument`
* Seleccionar renderer dinámicamente según `DeviceProfile.Target`
* Registrar renderers y servicios del motor vía `services.AddMotorDslEngine()`
* Demostrar el pipeline completo en una app MAUI con preview + envío simulado

👉 Este sprint habilita **CU-06** (Renderizar ESC/POS), **CU-11** (Seleccionar perfil de impresora), **CU-12** (Adaptar documento al perfil), y prepara indirectamente **CU-10** (Enviar a impresora Bluetooth).

---

## 🧩 Historias / tareas comprometidas

| ID    | Tipo     | Descripción                                                       | Prioridad | Estimación |
| ----- | -------- | ----------------------------------------------------------------- | --------- | ---------- |
| US-13 | Historia | Renderizar documento a ESC/POS (byte[])                          | Alta      | 13 pts     |
| US-14 | Historia | Selección dinámica de renderer según perfil de dispositivo        | Alta      | 8 pts      |
| US-15 | Historia | Registro de renderers y servicios del motor vía DI                | Alta      | 5 pts      |
| US-16 | Historia | App MAUI de ejemplo con preview y envío simulado                  | Media     | 8 pts      |
| TK-20 | Técnica  | Implementar EscPosRenderer (genera byte[] manualmente)            | Alta      | 13 pts     |
| TK-21 | Técnica  | Definir comandos ESC/POS soportados (ESC @, ESC a, ESC !, GS V)  | Alta      | 5 pts      |
| TK-22 | Técnica  | Implementar IRendererRegistry + RendererRegistry                  | Alta      | 5 pts      |
| TK-23 | Técnica  | Implementar extensión `services.AddMotorDslEngine()`              | Alta      | 5 pts      |
| TK-24 | Técnica  | Actualizar DocumentEngine para usar IRendererRegistry             | Alta      | 3 pts      |
| TK-25 | Técnica  | Crear app MAUI de demostración (sin Bluetooth real)               | Media     | 8 pts      |
| TK-26 | Técnica  | Tests unitarios de EscPosRenderer y RendererRegistry              | Alta      | 8 pts      |

---

## 🏗️ Alcance técnico del sprint

### EscPosRenderer (TK-20, TK-21)

* Clase dentro de `MotorDsl.Rendering` (`EscPosRenderer`, junto a `TextRenderer` y `EscPosCommands`)
* Implementa `IRenderer` con `Target => "escpos"`
* Recibe `LayoutedDocument` + `DeviceProfile` → devuelve `RenderResult` con `byte[]` en `Output`
* Genera bytes ESC/POS **manualmente** (sin dependencia de ESCPOS_NET)
* Comandos soportados en esta iteración:

| Comando      | Hex            | Descripción                          |
| ------------ | -------------- | ------------------------------------ |
| ESC @        | `0x1B 0x40`    | Inicializar impresora                |
| ESC a n      | `0x1B 0x61 n`  | Alineación (0=izq, 1=centro, 2=der) |
| ESC ! n      | `0x1B 0x21 n`  | Selección de estilo (bold, etc.)     |
| GS V m       | `0x1D 0x56 m`  | Corte de papel (0=total, 1=parcial)  |
| ESC d n      | `0x1B 0x64 n`  | Avance de n líneas                   |
| LF           | `0x0A`         | Salto de línea                       |

* Encoding de texto: codepage 437/850 configurable vía `DeviceProfile`
* No soporta en este sprint: imágenes, QR, códigos de barras

### Registro de renderers (TK-22, TK-24)

* Nueva interfaz `IRendererRegistry`:

```csharp
public interface IRendererRegistry
{
    void Register(IRenderer renderer);
    IRenderer? GetRenderer(string target);
    IEnumerable<string> GetAvailableTargets();
}
```

* `RendererRegistry` implementa la interfaz con diccionario interno
* `DocumentEngine` recibe `IRendererRegistry` (reemplaza lista de `IRenderer`)
* Selección automática por `DeviceProfile.Target` → fallback a `"text"`

### Extensión DI (TK-23)

* Extension method en el proyecto `MotorDsl.Extensions` (clase `MotorDslServiceCollectionExtensions`, namespace `MotorDsl.Extensions`). Devuelve un `MotorDslBuilder` encadenable.
* El `IRendererRegistry` se construye por factory: registra de fábrica `new TextRenderer()` y `new EscPosRenderer()` (de `MotorDsl.Rendering`) más todos los `IRenderer` resueltos del contenedor (`sp.GetServices<IRenderer>()`).
* Snippet **esquemático** (la firma real devuelve `MotorDslBuilder` y el RendererRegistry usa factory):

```csharp
// Proyecto real: MotorDsl.Extensions
public static class MotorDslServiceCollectionExtensions
{
    public static MotorDslBuilder AddMotorDslEngine(this IServiceCollection services)
    {
        services.AddSingleton<IDslParser, DslParser>();
        services.AddSingleton<IEvaluator, Evaluator>();
        services.AddSingleton<ILayoutEngine, LayoutEngine>();
        // RendererRegistry por factory: TextRenderer + EscPosRenderer de fábrica
        // + los IRenderer del contenedor (sp.GetServices<IRenderer>())
        services.AddSingleton<IRendererRegistry>(sp => /* factory: new TextRenderer(), new EscPosRenderer(), ... */);
        services.AddSingleton<IDocumentEngine, DocumentEngine>();
        return new MotorDslBuilder(services);
    }
}
```

### App MAUI de ejemplo (TK-25)

* Proyecto `SampleApp.Maui` o similar
* Pantalla con:
  * TextBox para DSL
  * Botón "Preview" → ejecuta pipeline con `TextRenderer` y muestra resultado
  * Botón "Generar ESC/POS" → ejecuta pipeline con `EscPosRenderer` y muestra hex dump
* Define `IThermalPrinterService` **en la app MAUI** (no en la librería del motor). Bosquejo conceptual del sprint; la superficie real del service local de los samples es:

```csharp
public interface IThermalPrinterService
{
    bool IsConnected { get; }
    Task<List<BluetoothDevice>> ScanDevicesAsync();
    Task<bool> ConnectAsync(string deviceAddress);
    Task DisconnectAsync();
    Task SendBytesAsync(byte[] data, PrinterProfile? profile = null, PrintRetryOptions? retryOptions = null);
}
```

* Implementación mock/stub (sin conexión Bluetooth real)
* Demuestra que el motor es consumible vía DI en un host real

---

## 🔁 Pipeline completo con este sprint

```text
DSL
 ↓
Parser (Sprint 01)
 ↓
AST
 ↓
Evaluator (Sprint 02)
 ↓
EvaluatedDocument
 ↓
LayoutEngine (Sprint 03)
 ↓
LayoutedDocument
 ↓
RendererRegistry.GetRenderer(target)  ← Sprint 04
 ↓
┌─────────────────┐     ┌──────────────────┐
│ TextRenderer     │     │ EscPosRenderer   │
│ (Sprint 03)      │     │ (Sprint 04)      │
│ → string         │     │ → byte[]         │
└─────────────────┘     └──────────────────┘
         ↓                        ↓
    RenderResult             RenderResult
```

---

## 🔧 Decisiones arquitectónicas del sprint

| Decisión | Fundamento |
| -------- | ---------- |
| **Sin ESCPOS_NET** | Generamos bytes manualmente para evitar dependencia externa y mantener control total del output |
| **IThermalPrinterService en MAUI app** | El servicio de impresión Bluetooth es responsabilidad de la app host, no de la librería del motor |
| **IRendererRegistry explícito** | Registro manual de renderers vía DI, sin assembly scanning (evita magia/complejidad) |
| **RenderResult.Output como object** | Permite `string` (TextRenderer) y `byte[]` (EscPosRenderer) en el mismo contrato |
| **Encoding configurable** | Codepage 437/850 seleccionable en DeviceProfile para distintas impresoras térmicas |

---

## 🔧 Capacidades habilitadas

**CU habilitados por este sprint:**

* **CU-06:** Renderizar documento a ESC/POS — totalmente implementado
* **CU-11:** Seleccionar perfil de impresora — el `DeviceProfile.Target` determina el renderer
* **CU-12:** Adaptar documento al perfil — el `LayoutEngine` ya adapta y el renderer respeta el perfil

**CU preparado (no completo):**

* **CU-10:** Enviar a impresora Bluetooth — la app MAUI define `IThermalPrinterService` pero sin implementación real de BT

---

## 🚫 Fuera de alcance

* **ESCPOS_NET** ni ninguna librería externa de ESC/POS
* Conexión Bluetooth real (solo interfaz + mock)
* Renderer PDF → Sprint 05
* Renderer UI/Preview MAUI nativo → Sprint 05+
* Soporte de imágenes, QR o códigos de barras en ESC/POS
* Assembly scanning para descubrimiento automático de renderers
* Logging/telemetría avanzada del pipeline

---

## ✅ Criterios de terminado (Definition of Done)

Una tarea se considera terminada cuando:

* `EscPosRenderer` genera secuencias de bytes ESC/POS correctas para documentos con texto, alineación, estilos y estructura
* Los bytes producidos comienzan con `ESC @` (init) y terminan con `GS V` (corte)
* `RendererRegistry` permite registrar y resolver renderers por target
* `DocumentEngine` selecciona renderer automáticamente basado en `DeviceProfile.Target`
* `services.AddMotorDslEngine()` registra todos los servicios necesarios
* La app MAUI de ejemplo ejecuta el pipeline completo y muestra resultados
* Tests unitarios validan byte[] esperados sin necesidad de hardware
* No existe dependencia de ESCPOS_NET ni de servicios de hardware en la librería del motor
* El código mantiene separación de responsabilidades entre motor y app host

---

## 🧪 Plan de pruebas del sprint

### Pruebas unitarias — EscPosRenderer

* Documento simple → verifica bytes `ESC @` + texto + `GS V`
* Alineación centro → verifica `ESC a 1`
* Alineación derecha → verifica `ESC a 2`
* Estilo bold → verifica `ESC ! n` con bit correspondiente
* Target correcto → `"escpos"`
* Documento vacío → solo init + corte
* `RenderResult.IsSuccessful` == true

### Pruebas unitarias — RendererRegistry

* Registro y obtención de renderer por target
* Target inexistente → retorna null
* Múltiples renderers registrados → cada uno accesible por su target
* `GetAvailableTargets()` lista targets registrados

### Pruebas unitarias — DI / AddMotorDslEngine

* `ServiceProvider` resuelve `IDocumentEngine`
* `ServiceProvider` resuelve `IRendererRegistry` con renderers registrados

### Pruebas de integración

* DSL completo → `EscPosRenderer` → byte[] con estructura válida
* DSL completo → `TextRenderer` (vía target) → string
* Selección de renderer por `DeviceProfile.Target`

---

## 🚀 Entregables del sprint

* `EscPosRenderer` implementado y testeado (byte[] sin dependencias externas)
* `IRendererRegistry` + `RendererRegistry` implementado
* `DocumentEngine` actualizado con selección dinámica de renderer
* `AddMotorDslEngine()` extension method funcional
* App MAUI de ejemplo con preview y hex dump
* Suite de tests unitarios cubriendo ESC/POS, registry y DI
* Motor consumible como librería desde cualquier host .NET

---

## ⚠️ Riesgos identificados

* Complejidad en la generación manual de bytes ESC/POS (sin librería auxiliar)
* Encoding de caracteres especiales según codepage de la impresora
* Verificación de bytes correctos sin hardware real (mitigado con tests de byte[])
* Acoplamiento accidental entre EscPosRenderer y lógica de comunicación BT
* Alcance de la app MAUI podría crecer si no se mantiene como demo simple

---

## 🔄 Métricas de seguimiento

* Tests de byte[] ESC/POS pasando sin hardware
* Cantidad de renderers registrables dinámicamente
* Pipeline end-to-end funcional con ambos renderers (text + escpos)
* App MAUI ejecutable con preview y generación ESC/POS
* Cobertura de tests del sprint

---

## 🧠 Resultado esperado al finalizar el Sprint 04

El motor quedará en un estado donde:

* Genera **bytes ESC/POS reales** listos para enviar a una impresora térmica
* Permite **seleccionar renderer dinámicamente** por target del perfil
* Es registrable y consumible **vía DI** con una sola línea
* Tiene una **app MAUI funcional** que demuestra el pipeline completo
* Mantiene **cero dependencias de hardware** en la librería core
* Está preparado para que un sprint futuro conecte Bluetooth real (solo falta implementar `IThermalPrinterService`)

---
