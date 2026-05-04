# Contratos del Motor — Librería DSL de Renderizado  
**Archivo:** contratos-del-motor_v1.0.md  
**Versión:** 1.0  
**Fecha:** 2026-03-28  
**Autor:** Equipo Técnico  
**Estado:** Borrador  

---

## 1. Propósito

Este documento define los contratos públicos del Motor DSL como librería. Establece las interfaces, modelos de entrada/salida y puntos de extensión que permiten a aplicaciones consumidoras integrar el motor de renderizado de documentos.

Los contratos aquí definidos constituyen la fuente de verdad para:

- Uso del motor desde aplicaciones externas (.NET MAUI, servicios, etc.)
- Implementación de renderizadores personalizados
- Integración con perfiles de dispositivo
- Testing e interoperabilidad

---

## 2. Convenciones generales

**Namespace base sugerido**

```text
MotorDsl
````

**Principios**

* Interfaces orientadas a contratos (DIP)
* Modelos independientes de infraestructura
* Sin dependencia de UI o frameworks específicos
* Extensible mediante inyección de dependencias
* Tipado fuerte en todos los contratos

**Reglas**

* No acoplar contratos a implementaciones concretas
* No exponer detalles internos del pipeline
* Mantener modelos serializables cuando aplique
* Evitar dependencias circulares

---

## 3. Contrato principal — Motor de documentos

### IDocumentEngine

```csharp id="idocumentengine-001"
public interface IDocumentEngine
{
    // Modo clásico — DSL crudo + datos
    RenderResult Render(
        string templateDsl,
        object data,
        DeviceProfile profile);

    // Modo clásico — plantilla pre-parseada + datos
    RenderResult Render(
        DocumentTemplate template,
        object data,
        DeviceProfile profile);

    // Modo integrado — JSON ya resuelto, sin diccionario de datos
    RenderResult Render(
        string integratedJson,
        DeviceProfile profile);

    // Pipeline parcial Parse → Evaluate → Layout (sin Render) — útil para preview UI
    LayoutedDocument RenderLayout(
        string templateDsl,
        object data,
        DeviceProfile profile);
}
```

**Responsabilidad**

* Orquestar todo el pipeline de renderizado
* Aceptar DSL crudo, plantilla pre-parseada o JSON integrado
* Devolver el resultado final renderizado

**Cuándo usar cada overload**

| Overload | Caso de uso |
|---|---|
| `Render(string, object, DeviceProfile)` | Caso general: el consumidor tiene un template DSL con `{{placeholders}}` y un diccionario de datos. |
| `Render(DocumentTemplate, object, DeviceProfile)` | Optimización: la plantilla ya fue parseada en una llamada previa y se reutiliza con datos distintos. |
| `Render(string, DeviceProfile)` | El JSON ya viene con todos los valores resueltos (`"format": "integrated"`, sin loops ni conditionals). El motor saltea la etapa de Evaluate. |
| `RenderLayout(...)` | Preview de UI: necesita el `LayoutedDocument` intermedio sin ejecutar el renderer final. |

> **Defensa en profundidad:** el overload integrado verifica que el JSON declare `"format": "integrated"`. Si llega un template clásico por esta vía, devuelve un `RenderResult` con error en lugar de procesarlo silenciosamente.

---

## 4. Modelo de entrada

### DocumentTemplate

```csharp id="documenttemplate-001"
public class DocumentTemplate
{
    public const string FormatTemplate   = "template";
    public const string FormatIntegrated = "integrated";

    public string Id { get; set; }
    public string Version { get; set; }
    public DocumentNode? Root { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    /// Default: "template". Set to "integrated" to indicate the AST is fully resolved.
    public string Format { get; set; } = FormatTemplate;
}
```

**Responsabilidad**

Representar una plantilla ya parseada y validada. La propiedad `Format` discrimina entre el modo clásico (con placeholders/loops/conditionals a resolver) y el modo integrado (AST ya resuelto).

---

### DeviceProfile

```csharp id="deviceprofile-001"
public class DeviceProfile
{
    public string Name { get; set; }
    public int Width { get; set; }
    public string RenderTarget { get; set; } // escpos | ui | text | pdf
    public Dictionary<string, object> Capabilities { get; set; }
}
```

**Responsabilidad**

Definir las capacidades y restricciones del dispositivo destino.

---

## 5. Modelo abstracto del documento

### DocumentNode

```csharp id="documentnode-001"
public abstract class DocumentNode
{
    public string Type { get; set; }
    public List<DocumentNode> Children { get; set; }
}
```

---

### Tipos derivados (ejemplos)

```csharp id="textnode-001"
public class TextNode : DocumentNode
{
    public string Text { get; set; }
}
```

```csharp id="tablenode-001"
public class TableNode : DocumentNode
{
    public List<string> Headers { get; set; }
    public List<List<string>> Rows { get; set; }
}
```

```csharp id="conditionalnode-001"
public class ConditionalNode : DocumentNode
{
    public string Expression { get; set; }
    public DocumentNode TrueBranch { get; set; }
    public DocumentNode FalseBranch { get; set; }
}
```

---

## 6. Contrato de renderización

### IRenderer

```csharp id="irenderer-001"
public interface IRenderer
{
    string Target { get; }
    RenderResult Render(DocumentNode document, DeviceProfile profile);
}
```

**Responsabilidad**

* Transformar el modelo abstracto en una salida específica
* Cada renderizador soporta un target distinto

---

### RenderResult

```csharp id="renderresult-001"
public class RenderResult
{
    public string Target { get; set; }
    public object Output { get; set; }
    public List<string> Warnings { get; set; }
    public List<string> Errors { get; set; }
}
```

**Responsabilidad**

Contener el resultado del renderizado junto con posibles advertencias o errores.

---

## 7. Contrato del parser DSL

### IDslParser

```csharp id="idslparser-001"
public interface IDslParser
{
    DocumentTemplate Parse(string dsl);
}
```

**Responsabilidad**

* Convertir DSL en modelo estructurado (`DocumentTemplate`)
* Validar sintaxis básica

---

## 8. Contrato del motor de layout

### ILayoutEngine

```csharp id="ilayoutengine-001"
public interface ILayoutEngine
{
    DocumentNode ApplyLayout(DocumentNode document, DeviceProfile profile);
}
```

**Responsabilidad**

* Aplicar reglas de distribución visual
* Ajustar estructura según perfil del dispositivo
* Preparar el documento para renderizado final

---

## 9. Contrato de resolución de datos

### IDataResolver

```csharp id="idataresolver-001"
public interface IDataResolver
{
    object Resolve(object data, string path);
}
```

**Responsabilidad**

* Resolver referencias de datos dentro del documento
* Mapear datos externos a la plantilla

---

## 10. Contrato de perfiles de dispositivo

### IDeviceProfileProvider

```csharp id="ideviceprofileprovider-001"
public interface IDeviceProfileProvider
{
    DeviceProfile GetProfile(string name);
}
```

**Responsabilidad**

* Proveer perfiles de dispositivos registrados
* Permitir selección dinámica del target

---

## 11. Flujo de uso del motor

```text id="flow-001"
Plantilla DSL + Datos + Perfil
        ↓
IDocumentEngine.Render()
        ↓
Parse DSL → Modelo Abstracto
        ↓
Resolver Datos
        ↓
Evaluar Condiciones / Iteraciones
        ↓
Aplicar Layout
        ↓
Seleccionar Renderer
        ↓
RenderResult
```

---

## 12. Extensibilidad

El motor permite extenderse mediante:

### Nuevos renderizadores

Implementando:

```csharp id="custom-renderer-001"
public class CustomRenderer : IRenderer
{
    public string Target => "custom";

    public RenderResult Render(DocumentNode document, DeviceProfile profile)
    {
        // implementación personalizada
        return new RenderResult();
    }
}
```

---

### Nuevos tipos de nodos

Extendiendo `DocumentNode` para soportar nuevos elementos DSL.

---

### Registro en DI

```csharp id="di-registration-001"
services.AddSingleton<IRenderer, EscPosRenderer>();
services.AddSingleton<IRenderer, UiRenderer>();
```

---

## 13. Errores y manejo

El motor debe manejar:

* Errores de parsing
* Errores de validación DSL
* Datos inconsistentes
* Fallos de renderizado

Los errores deben reflejarse en `RenderResult.Errors`.

---

## 14. Criterios de aceptación

El motor cumple sus contratos si:

* Puede renderizar desde DSL o modelo abstracto
* Soporta múltiples renderizadores intercambiables
* Permite inyección de dependencias
* No depende de UI ni infraestructura externa
* Mantiene independencia entre capas
* Permite extensión sin modificar core

---

## 15. Relación con otros documentos

Este documento se alinea con:

* arquitectura-solucion_v1.0.md
* modelo-datos-logico_v1.0.md
* extensibilidad-motor_v1.0.md
* guia-uso-libreria_v1.0.md
* wireframes-documentos_v1.0.md
* CU-xx (casos de uso del motor)
* RN-02 a RN-06
* RC-01 a RC-06

---

## 16. Historial de versiones

| Versión | Fecha      | Autor          | Cambios                         |
| ------- | ---------- | -------------- | ------------------------------- |
| 1.0     | 2026-03-28 | Equipo Técnico | Definición inicial de contratos |

---
