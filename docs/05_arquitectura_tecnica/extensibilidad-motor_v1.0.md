# Extensibilidad del Motor — Librería DSL de Renderizado  
**Archivo:** extensibilidad-motor_v1.0.md  
**Versión:** 1.0  
**Fecha:** 2026-03-28  
**Autor:** Equipo Técnico  
**Estado:** Borrador  

---

## 1. Propósito

Este documento describe los mecanismos de extensibilidad del Motor DSL de renderizado. Define cómo terceros pueden ampliar el comportamiento del motor sin modificar su núcleo, manteniendo desacoplamiento, cohesión y compatibilidad entre versiones.

La extensibilidad es un principio central del diseño del motor, permitiendo:

- Incorporar nuevos renderizadores
- Agregar nuevos tipos de nodos DSL
- Extender capacidades de layout
- Integrar nuevos perfiles de dispositivo
- Introducir lógica personalizada mediante inyección de dependencias

---

## 2. Principios de extensibilidad

El motor sigue los siguientes principios:

- **Open/Closed Principle**: abierto a extensión, cerrado a modificación  
- **Inversión de dependencias**: el core depende de abstracciones  
- **Plug-in architecture**: componentes reemplazables  
- **Desacoplamiento**: cada módulo es independiente  
- **Registro explícito**: extensiones se registran vía DI o fábrica  

---

## 3. Puntos de extensión principales

El motor expone los siguientes puntos de extensión:

1. Renderizadores (`IRenderer`)  
2. Nodos DSL personalizados (`DocumentNode`)  
3. Evaluadores de expresiones  
4. Layout engines (`ILayoutEngine`)  
5. Resolvedores de datos (`IDataResolver`)  
6. Proveedores de perfiles (`IDeviceProfileProvider`)  

---

## 4. Extensión: Renderizadores

### Objetivo

Permitir generar salidas en distintos formatos (UI, ESC/POS, PDF, texto, etc.).

### Contrato base

```csharp id="irenderer-ext-001"
public interface IRenderer
{
    string Target { get; }
    RenderResult Render(DocumentNode document, DeviceProfile profile);
}
````

### Implementación personalizada

```csharp id="custom-renderer-ext-001"
public class HtmlRenderer : IRenderer
{
    public string Target => "html";

    public RenderResult Render(DocumentNode document, DeviceProfile profile)
    {
        // Lógica de render HTML
        return new RenderResult
        {
            Target = Target,
            Output = "<html>...</html>"
        };
    }
}
```

### Registro

```csharp id="renderer-registration-001"
services.AddSingleton<IRenderer, HtmlRenderer>();
```

---

## 5. Extensión: Nuevos nodos DSL

### Objetivo

Permitir introducir nuevos elementos estructurales en el DSL sin modificar el core.

### Estrategia

* Heredar de `DocumentNode`
* Implementar propiedades específicas
* Extender el parser DSL para reconocer el nodo

### Ejemplo

```csharp id="custom-node-001"
public class BarcodeNode : DocumentNode
{
    public string Value { get; set; }
    public string Format { get; set; }
}
```

### Integración en parser

El parser (`IDslParser`) debe mapear la sintaxis DSL a este nuevo nodo.

---

## 6. Extensión: Layout Engine

### Objetivo

Modificar o ampliar cómo se organiza visualmente el documento según el dispositivo.

### Contrato

```csharp id="ilayoutengine-ext-001"
public interface ILayoutEngine
{
    DocumentNode ApplyLayout(DocumentNode document, DeviceProfile profile);
}
```

### Caso de uso

* Ajuste de márgenes dinámicos
* Reflow de contenido
* Adaptación a ancho de dispositivo
* Reorganización de nodos

### Ejemplo de implementación

```csharp id="layout-engine-ext-001"
public class MobileLayoutEngine : ILayoutEngine
{
    public DocumentNode ApplyLayout(DocumentNode document, DeviceProfile profile)
    {
        // Ajustes específicos para dispositivos pequeños
        return document;
    }
}
```

---

## 7. Extensión: Evaluación de expresiones

### Objetivo

Permitir lógica dinámica en el DSL mediante evaluadores personalizados.

### Responsabilidad

* Interpretar expresiones DSL
* Resolver variables del contexto
* Ejecutar operaciones lógicas

### Consideraciones

* Seguridad (sandboxing)
* Performance
* Soporte de funciones personalizadas

---

## 8. Extensión: Resolución de datos

### Contrato

```csharp id="idataresolver-ext-001"
public interface IDataResolver
{
    object Resolve(object data, string path);
}
```

### Objetivo

Permitir diferentes estrategias de acceso a datos:

* Reflection-based
* Expression-based
* JSON path
* Custom mapping

### Ejemplo

```csharp id="dataresolver-ext-001"
public class JsonPathDataResolver : IDataResolver
{
    public object Resolve(object data, string path)
    {
        // Resolución basada en JSON path
        return null;
    }
}
```

---

## 9. Extensión: Proveedores de perfiles

### Contrato

```csharp id="ideviceprofileprovider-ext-001"
public interface IDeviceProfileProvider
{
    DeviceProfile GetProfile(string name);
}
```

### Objetivo

Permitir registrar perfiles dinámicamente desde:

* Configuración
* Base de datos
* Código
* Servicios externos

---

## 10. Registro de extensiones (Dependency Injection)

El motor está diseñado para integrarse con contenedores DI estándar de .NET.

### Ejemplo general

```csharp id="di-ext-001"
services.AddSingleton<IDocumentEngine, DocumentEngine>();
services.AddSingleton<IRenderer, EscPosRenderer>();
services.AddSingleton<IRenderer, UiRenderer>();
services.AddSingleton<ILayoutEngine, DefaultLayoutEngine>();
```

---

## 11. Descubrimiento de extensiones

El motor puede soportar:

* Registro explícito manual
* Auto-discovery por reflection (opcional)
* Carga de plugins externos (assembly scanning)

---

## 12. Arquitectura de plugins (opcional)

Para escenarios avanzados:

* Carga dinámica de assemblies
* Registro de componentes en runtime
* Aislamiento de plugins

Consideraciones:

* Versionado de contratos
* Compatibilidad hacia atrás
* Seguridad de ejecución

---

## 13. Versionado de extensiones

Recomendaciones:

* Mantener compatibilidad de interfaces
* Evitar breaking changes en contratos públicos
* Introducir nuevas interfaces en lugar de modificar existentes
* Versionar plugins independientemente del core

---

## 14. Buenas prácticas

* Implementar extensiones como componentes pequeños y cohesivos
* Evitar lógica de negocio compleja dentro de renderizadores
* Mantener nodos DSL simples y composables
* Documentar cada extensión públicamente
* Registrar extensiones de forma explícita
* Evitar acoplamiento entre extensiones

---

## 15. Casos de uso de extensibilidad

* Renderizar a nuevos formatos (HTML, PDF, imagen)
* Soportar nuevos dispositivos de impresión
* Introducir nuevos nodos DSL (QR, barcode, gráficos)
* Integrar motores de expresión personalizados
* Adaptar layout para nuevos dispositivos
* Conectar con distintas fuentes de datos

---

## 16. Riesgos conocidos

* Exceso de extensiones puede fragmentar el ecosistema
* Plugins mal diseñados pueden afectar performance
* Conflictos entre implementaciones múltiples de una misma interfaz
* Complejidad en debugging distribuido
* Incompatibilidades entre versiones de contratos

---

## 17. Evolución prevista

Posibles mejoras futuras:

* Sistema formal de plugins con metadata
* Marketplace interno de extensiones
* Validación automática de compatibilidad
* Sandbox para ejecución de extensiones
* Pipeline configurables por usuario
* Hot-reload de extensiones en runtime

---

## 18. Compatibilidad con el formato integrado

El formato integrado (`DocumentTemplate.Format == "integrated"`) se beneficia transparentemente de los puntos de extensión existentes:

* **Renderizadores custom**: cualquier `IRenderer` registrado vía `MotorDslBuilder.AddRenderer<T>()` funciona idénticamente para ambos formatos. El renderer recibe un `LayoutedDocument` y no inspecciona el `Format` del template original.
* **Layout engines custom**: una implementación alternativa de `ILayoutEngine` se aplica de la misma forma a documentos clásicos e integrados — la única diferencia es que en el modo integrado no precede una etapa de Evaluate.
* **Profile providers / validators**: el `IProfileValidator` se invoca antes del Layout en ambas modalidades, así que las reglas custom de capabilities siguen aplicando.
* **Renderers de capability degradation** (CU-23): el `LayoutEngine` produce las mismas warnings (`QR degradado`, `Image degradado`, `Barcode degradado`) sin importar el formato de origen.

El único punto de extensión que **no aplica** al modo integrado es el `IDataValidator`, ya que no hay diccionario de datos que validar. Si un consumidor necesita validar la estructura del AST integrado más allá de las reglas del `TemplateValidator`, puede registrar un validador adicional o componer reglas dentro del propio productor del JSON.

---

## 19. Relación con otros documentos

Este documento se alinea con:

* arquitectura-solucion_v1.0.md
* contratos-del-motor_v1.0.md
* modelo-datos-logico_v1.0.md
* flujo-ejecucion-motor_v1.0.md
* guia-uso-libreria_v1.0.md
* RC-04, RC-05, RC-06

---

## 20. Historial de versiones

| Versión | Fecha      | Autor          | Cambios                                                       |
| ------- | ---------- | -------------- | ------------------------------------------------------------- |
| 1.0     | 2026-03-28 | Equipo Técnico | Definición inicial                                            |
| 1.1     | 2026-05-04 | Equipo Técnico | Compatibilidad de los puntos de extensión con formato integrado |

---