# 📄 Plan de Iteración — Sprint 05
**Archivo:** plan-iteracion_sprint-05_v1.0.md  
**Versión:** 1.0  
**Fecha:** 2026-05-27

## 📋 Información general

* Proyecto: Motor DSL para generación y renderizado de documentos (ESC/POS / PDF / Preview)
* Sprint: 05
* Duración: 2 semanas
* Fecha inicio: 27/05/2026
* Fecha fin: 09/06/2026
* Objetivo del sprint: Proveedores configurables + TableNode + QR ESC/POS + RenderResult helpers + Validaciones core

---

## 🎯 Objetivo del Sprint

Evolucionar la arquitectura del motor hacia un modelo de **proveedores inyectables**, permitiendo:

* Registrar y obtener plantillas DSL vía `ITemplateProvider`
* Registrar y obtener perfiles de impresora vía `IDeviceProfileProvider`
* Registrar y obtener datos vía `IDataProvider`
* Configurar el motor con una fluent API: `AddMotorDslEngine().AddTemplates().AddProfiles()`
* Renderizar `TableNode` correctamente en TextRenderer y EscPosRenderer
* Generar códigos QR en EscPosRenderer (`GS ( k`)
* Exponer `ToHexString()` y `ToBase64()` en `RenderResult`

👉 Este sprint habilita **CU-21 v2.0** (Proveer perfiles), **CU-24 v2.0** (Proveer plantillas), **CU-25 v2.0** (Proveer datos), mejora **CU-06 v2.0** (RenderResult helpers + QR), y **CU-07** (TableNode en texto).

---

## 🧩 Historias / tareas comprometidas

| ID    | Tipo     | Descripción                                                                    | Prioridad | Estimación |
| ----- | -------- | ------------------------------------------------------------------------------ | --------- | ---------- |
| US-17 | Historia | Como cliente, quiero registrar mis plantillas DSL en la librería para usarlas por ID | Alta      | 8 pts      |
| US-18 | Historia | Como cliente, quiero registrar mis perfiles de impresora para seleccionarlos por nombre | Alta      | 8 pts      |
| US-19 | Historia | Como cliente, quiero que el RenderResult tenga ToHexString() y ToBase64() para transmitir el ticket por email o API | Alta      | 5 pts      |
| US-20 | Historia | Como desarrollador, quiero que TableNode se renderice correctamente en texto plano y ESC/POS | Alta      | 13 pts     |
| TK-27 | Técnica  | Definir ITemplateProvider + InMemoryTemplateProvider                            | Alta      | 5 pts      |
| TK-28 | Técnica  | Definir IDataProvider + InMemoryDataProvider                                   | Alta      | 5 pts      |
| TK-29 | Técnica  | Definir IDeviceProfileProvider + InMemoryDeviceProfileProvider                 | Alta      | 5 pts      |
| TK-30 | Técnica  | Actualizar AddMotorDslEngine() con fluent API: .AddTemplates() / .AddProfiles() | Alta      | 5 pts      |
| TK-31 | Técnica  | Agregar ToHexString() y ToBase64() a RenderResult                              | Alta      | 3 pts      |
| TK-32 | Técnica  | Implementar TableNode en TextRenderer                                          | Alta      | 8 pts      |
| TK-33 | Técnica  | Implementar TableNode en EscPosRenderer                                        | Alta      | 8 pts      |
| TK-34 | Técnica  | Implementar QR básico en EscPosRenderer (GS ( k)                               | Media     | 8 pts      |
| TK-35 | Técnica  | Tests unitarios de todo lo anterior                                            | Alta      | 8 pts      |

---

## 🏗️ Alcance técnico del sprint

### Proveedores configurables (TK-27, TK-28, TK-29)

* Tres nuevas interfaces en `MotorDsl.Core.Contracts` (los contratos NO incluyen `Add`; ese método es API de las clases In-Memory):

```csharp
public interface ITemplateProvider
{
    string? GetTemplate(string templateId);
    IEnumerable<string> GetAvailableTemplateIds();
}

public interface IDataProvider
{
    IDictionary<string, object>? GetData(string dataKey);
}

public interface IDeviceProfileProvider
{
    DeviceProfile? GetProfile(string name);
    IEnumerable<DeviceProfile> GetAll();
}
```

* Tres implementaciones default en `MotorDsl.Core.Providers` (exponen además el método `Add(...)` para alimentar el diccionario interno):
  * `InMemoryTemplateProvider` — diccionario `<string, string>` + `void Add(string templateId, string dslContent)`
  * `InMemoryDataProvider` — diccionario `<string, IDictionary<string, object>>` + `void Add(string dataKey, IDictionary<string, object> data)`
  * `InMemoryDeviceProfileProvider` — diccionario `<string, DeviceProfile>` + `void Add(DeviceProfile profile)`

* Principio: la librería define contratos, el cliente provee la implementación.
* Si el cliente no registra un proveedor personalizado, se usa el default In-Memory.

### Fluent API (TK-30)

* Actualizar `AddMotorDslEngine()` para retornar un builder configurable:

```csharp
services.AddMotorDslEngine()
    .AddTemplates(templates =>
    {
        templates.Add("ticket-venta", dslJson);
        templates.Add("recibo", otroDslJson);
    })
    .AddProfiles(profiles =>
    {
        profiles.Add(new DeviceProfile("58mm", 32, "escpos"));
        profiles.Add(new DeviceProfile("80mm", 48, "escpos"));
    });
```

* `AddMotorDslEngine()` registra todos los servicios core + proveedores default.
* `.AddTemplates()` y `.AddProfiles()` configuran los proveedores In-Memory con datos iniciales.
* El cliente puede reemplazar cualquier proveedor registrando su propia implementación después.

### RenderResult Helpers (TK-31)

* Agregar a `RenderResult`:

```csharp
public string? ToHexString()
{
    if (Output is byte[] bytes)
        return BitConverter.ToString(bytes).Replace("-", " ");
    return null;
}

public string? ToBase64()
{
    if (Output is byte[] bytes)
        return Convert.ToBase64String(bytes);
    return null;
}
```

* `ToHexString()` → `"1B 40 48 6F 6C 61 0A 1D 56 00"` — para diagnóstico y UI
* `ToBase64()` → `"G0BIb2xhCh1WAA=="` — para transmisión por email/API/almacenamiento
* Retornan `null` si `Output` no es `byte[]`

### TableNode en TextRenderer (TK-32)

* Leer `TableNode.Headers` y `TableNode.Rows` del AST evaluado
* Calcular ancho de columnas proporcional al `DeviceProfile.Width`
* Generar formato columnar con separadores:

```text
Producto        Cant    Precio
--------------------------------
Café              2     $150.00
Medialunas        6      $90.00
--------------------------------
```

* Soportar alineación por columna (left/right/center)
* Truncar contenido si excede el ancho de columna

### TableNode en EscPosRenderer (TK-33)

* Misma lógica de cálculo de columnas que TextRenderer
* Generar bytes ESC/POS: texto tabulado + alineación + separadores
* Cada fila se envía como una línea de texto ESC/POS con padding calculado
* Los encabezados pueden aplicar bold (`ESC !`)

### QR básico en EscPosRenderer (TK-34)

* Implementar generación de QR code usando comandos ESC/POS nativos (`GS ( k`). Tabla **esquemática** — los subcomandos realmente emitidos por `EscPosRenderer` son:

| Subcomando (`EscPosCommands`) | Hex | Descripción |
|---------|-----|-------------|
| QrSetSize3 | `1D 28 6B 03 00 31 43 03` | Tamaño de módulo (size 3) |
| QrSetErrorM | `1D 28 6B 03 00 31 45 33` | Nivel de corrección de errores (M) |
| (store, manual) | `1D 28 6B ll lh 31 50 30` | Almacenar datos QR (`31 50 30`) |
| QrPrint | `1D 28 6B 03 00 31 51 30` | Imprimir QR almacenado (`31 51 30`) |

* Secuencia real:
  1. `GS ( k` — tamaño de módulo (size 3, `QrSetSize3`)
  2. `GS ( k` — nivel de corrección de errores (M, `QrSetErrorM`)
  3. `GS ( k` — almacenar datos: `1D 28 6B ll lh 31 50 30` + datos (construido manualmente)
  4. `GS ( k` — imprimir QR (`31 51 30`, `QrPrint`)

* Datos QR desde el contenido del nodo `image` con `imageType="qrcode"` (metadata `is_qr`/`qr_data` del LayoutEngine)

---

## 🔁 Pipeline completo con este sprint

```text
DSL JSON
 ↓
Parser (Sprint 01)
 ↓
AST (con TableNode)
 ↓
Evaluator (Sprint 02)
 ↓
EvaluatedDocument
 ↓
LayoutEngine (Sprint 03)
 ↓
LayoutedDocument
 ↓
RendererRegistry.GetRenderer(target)  (Sprint 04)
 ↓
┌─────────────────┐     ┌──────────────────┐
│ TextRenderer     │     │ EscPosRenderer   │
│ + TableNode      │     │ + TableNode      │
│ (Sprint 03+05)   │     │ + QR Code        │
│ → string         │     │ (Sprint 04+05)   │
└─────────────────┘     │ → byte[]         │
         ↓              └──────────────────┘
    RenderResult                  ↓
    .Output (string)        RenderResult
                            .Output (byte[])
                            .ToHexString()
                            .ToBase64()
```

**Proveedores (nuevo en Sprint 05):**

```text
ITemplateProvider ──→ InMemoryTemplateProvider (default)
                  └─→ ClienteApiProvider (implementación del cliente)

IDataProvider ──────→ InMemoryDataProvider (default)
                  └─→ ClienteBdProvider (implementación del cliente)

IDeviceProfileProvider → InMemoryDeviceProfileProvider (default)
                     └─→ ClienteFileProvider (implementación del cliente)
```

---

## 🔧 Decisiones arquitectónicas del sprint

| Decisión | Fundamento |
| -------- | ---------- |
| **Patrón Provider** | La librería define contratos; el cliente implementa la fuente. Mantiene el motor libre de dependencias de infraestructura. |
| **InMemory como default** | Permite uso inmediato sin configurar fuentes externas. El cliente puede reemplazar en cualquier momento. |
| **Fluent API** | `AddMotorDslEngine().AddTemplates().AddProfiles()` es idiomático en .NET y facilita la configuración. |
| **ToHexString / ToBase64 en RenderResult** | Métodos de conveniencia que no agregan dependencias. Habilitan transmisión alternativa del ticket. |
| **QR con comandos nativos ESC/POS** | Sin dependencia de librería QR externa. El QR se genera en la impresora vía `GS ( k`. |
| **TableNode rendering** | Tablas son core en tickets (líneas de ítems). Se implementa en ambos renderers para consistencia. |
| **Sin HTTP client en la librería** | Si el cliente necesita HTTP para plantillas/datos/perfiles, implementa el proveedor correspondiente. |

---

## 🔧 Capacidades habilitadas

**CU habilitados/mejorados por este sprint:**

* **CU-21 v2.0:** Proveer Perfil de Impresora — `IDeviceProfileProvider` + `InMemoryDeviceProfileProvider`
* **CU-24 v2.0:** Proveer Plantilla al Motor — `ITemplateProvider` + `InMemoryTemplateProvider`
* **CU-25 v2.0:** Proveer Datos al Motor — `IDataProvider` + `InMemoryDataProvider`
* **CU-06 v2.0:** Renderizar ESC/POS — `ToHexString()`, `ToBase64()`, QR code, TableNode
* **CU-07:** Renderizar Texto Plano — TableNode columnar

**CU marcado como absorbido:**

* **CU-26:** Fusionado con CU-21 v2.0

**CU marcado como fuera de alcance:**

* **CU-09:** Renderizar PDF — fuera de la librería core, documentado como extensión del cliente

---

## 🚫 Fuera de alcance

* **PDF** — El cliente implementa `IRenderer` si lo necesita (ver CU-09 v2.0)
* **HTTP clients** — El cliente implementa `ITemplateProvider` / `IDataProvider` / `IDeviceProfileProvider` con su propia lógica HTTP
* **UI renderer nativo MAUI** — Queda para sprint futuro
* **Imágenes bitmap ESC/POS** — Queda para sprint futuro
* **Validación de esquema DSL** (CU-14) — Queda para sprint futuro
* **Validación de datos contra plantilla** (CU-19) — Queda para sprint futuro
* **Caching / optimización de performance** — Queda para sprint futuro

---

## ✅ Criterios de terminado (Definition of Done)

Una tarea se considera terminada cuando:

* `ITemplateProvider`, `IDataProvider`, `IDeviceProfileProvider` están definidos con implementaciones In-Memory
* `AddMotorDslEngine()` registra los proveedores default automáticamente
* `.AddTemplates()` y `.AddProfiles()` configuran datos iniciales en los proveedores
* `RenderResult.ToHexString()` genera hex correcto desde `byte[]`
* `RenderResult.ToBase64()` genera Base64 decodificable al `byte[]` original
* `TableNode` se renderiza correctamente en TextRenderer y EscPosRenderer
* QR code se genera con comandos `GS ( k` en EscPosRenderer
* Tests unitarios validan todas las funcionalidades nuevas
* No se altera funcionalidad existente (84 tests previos siguen pasando)
* La librería mantiene cero dependencias de infraestructura externa
* El código compila, pasa todos los tests y está integrado en el pipeline

---

## 🧪 Plan de pruebas del sprint

### Pruebas unitarias — Proveedores

* `InMemoryTemplateProvider`: Add, GetTemplate, GetAvailableTemplateIds, template inexistente retorna null
* `InMemoryDataProvider`: Add, GetData, datos inexistentes retorna null
* `InMemoryDeviceProfileProvider`: Add, GetProfile, GetAll, perfil inexistente retorna null
* Registro múltiple de items en cada proveedor
* Sobrescritura de items con mismo ID/nombre

### Pruebas unitarias — Fluent API

* `AddMotorDslEngine()` registra todos los servicios necesarios
* `.AddTemplates()` configura plantillas en `ITemplateProvider`
* `.AddProfiles()` configura perfiles en `IDeviceProfileProvider`
* Service provider resuelve `IDocumentEngine`, `ITemplateProvider`, `IDeviceProfileProvider`

### Pruebas unitarias — RenderResult Helpers

* `ToHexString()` con `byte[]` → retorna hex separado por espacios
* `ToBase64()` con `byte[]` → retorna Base64 decodificable
* `ToHexString()` con Output = `string` → retorna `null`
* `ToBase64()` con Output = `null` → retorna `null`
* Round-trip: `byte[]` → `ToBase64()` → `Convert.FromBase64String()` → bytes iguales

### Pruebas unitarias — TableNode TextRenderer

* Tabla simple con headers y rows → formato columnar correcto
* Alineación por columna (left, right, center)
* Contenido truncado si excede ancho de columna
* Tabla sin rows → solo headers y separadores
* Tabla vacía → sin output adicional

### Pruebas unitarias — TableNode EscPosRenderer

* Tabla simple → bytes ESC/POS con texto tabulado
* Headers con bold → `ESC !` aplicado
* Separadores generados correctamente
* Ancho de columnas respeta DeviceProfile.Width

### Pruebas unitarias — QR EscPosRenderer

* QR con texto simple → secuencia `GS ( k` correcta
* QR con diferentes tamaños de módulo
* QR con datos vacíos → manejo controlado
* Bytes generados comienzan con model selection y terminan con print command

### Pruebas de integración

* Pipeline completo con TableNode → TextRenderer → string con tabla
* Pipeline completo con TableNode → EscPosRenderer → byte[] con tabla
* Pipeline completo con QR → EscPosRenderer → byte[] con QR
* `RenderResult` de EscPosRenderer → `ToHexString()` y `ToBase64()` funcionales

---

## 🚀 Entregables del sprint

* `ITemplateProvider` + `InMemoryTemplateProvider` implementados y testeados
* `IDataProvider` + `InMemoryDataProvider` implementados y testeados
* `IDeviceProfileProvider` + `InMemoryDeviceProfileProvider` implementados y testeados
* `AddMotorDslEngine()` actualizado con fluent API
* `RenderResult.ToHexString()` y `ToBase64()` implementados y testeados
* `TableNode` renderizado en TextRenderer y EscPosRenderer
* QR code generado en EscPosRenderer con `GS ( k`
* Suite de tests unitarios cubriendo todo lo nuevo
* 84 tests previos siguen pasando (regresión cero)
* CUs actualizados: CU-06 v2.0, CU-09 v2.0, CU-21 v2.0, CU-24 v2.0, CU-25 v2.0, CU-26 fusionado

---

## ⚠️ Riesgos identificados

* Complejidad del cálculo de columnas en TableNode (proporcional al ancho)
* Compatibilidad de comandos QR `GS ( k` con distintas marcas de impresoras
* Tamaño del módulo QR puede no caber en impresora de 58mm
* La fluent API debe ser backward-compatible con código existente que usa `AddMotorDslEngine()` sin builder
* El registro de proveedores default no debe interferir con proveedores personalizados del cliente

---

## 🔄 Métricas de seguimiento

* Tests nuevos del sprint pasando
* Tests previos (84) sin regresión
* Pipeline end-to-end funcional con proveedores
* TableNode renderizado correctamente en ambos renderers
* QR code generado y verificable en hex dump
* `ToHexString()` / `ToBase64()` funcionales
* Fluent API resuelve todos los servicios correctamente

---

## 🧠 Resultado esperado al finalizar el Sprint 05

El motor quedará en un estado donde:

* Es **configurable**: plantillas, datos y perfiles se registran vía proveedores inyectables
* Es **extensible**: el cliente puede reemplazar cualquier proveedor con su implementación
* **Soporta tablas**: TableNode se renderiza correctamente en texto plano y ESC/POS
* **Soporta QR**: códigos QR generados nativamente sin dependencias externas
* **RenderResult es transmisible**: `ToHexString()` y `ToBase64()` habilitan canales alternativos
* **Mantiene cero dependencias externas** en la librería core
* Está preparado para que un sprint futuro agregue validaciones (CU-14, CU-19), UI renderer (CU-08) y optimizaciones de performance

---


