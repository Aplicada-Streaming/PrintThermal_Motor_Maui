```markdown
# modelo-ejecucion-logico_v1.0.md

## 1. Propósito

Definir el modelo lógico de ejecución del motor de renderizado de plantillas DSL, estableciendo:

- Las abstracciones principales del sistema
- Las estructuras de datos en memoria (AST)
- Las interfaces entre componentes
- El flujo de ejecución desde el DSL hasta la salida renderizada
- Las responsabilidades de cada módulo del motor

Este documento actúa como puente entre la arquitectura conceptual y la implementación en código.

---

## 2. Alcance

Aplica a:

- Interpretación de plantillas DSL
- Evaluación de estructuras dinámicas (condicionales, iteraciones)
- Resolución de datos
- Generación de representaciones intermedias
- Renderizado final (ESC/POS, PDF, UI preview)

No cubre:

- Implementación específica de librerías externas
- Detalles de UI
- Protocolos de comunicación externos

---

## 3. Visión general del modelo

El motor sigue un pipeline en etapas. Existen dos rutas según el campo `format` del documento:

**Ruta clásica** (`format: "template"` — default):

1. Entrada DSL (string) + datos
2. Parsing → AST con placeholders/loops/conditionals
3. Evaluación → Modelo resuelto
4. Adaptación por perfil de dispositivo
5. Renderizado → formato de salida

**Ruta integrada** (`format: "integrated"`):

1. Entrada DSL (string) — sin datos, AST ya resuelto
2. Parsing → AST resuelto (sin placeholders/loops/conditionals)
3. *(Evaluación se omite)*
4. Adaptación por perfil de dispositivo
5. Renderizado → formato de salida

### Flujo lógico:

```

DSL (string)
↓
Parser
↓
AST (árbol de nodos)
↓
[Evaluator]   ← solo si format == "template"
↓
Modelo Resuelto (datos + estructura)
↓
Renderer
↓
Output (ESC/POS / PDF / UI / Texto)

````

> En la ruta integrada, el Parser produce un AST que ya cumple los invariantes del Modelo Resuelto: sin tokens `{{...}}` por interpretar, sin nodos `loop` por expandir, sin nodos `conditional` por evaluar. Por eso el Evaluator es innecesario y se omite.

---

## 4. Componentes principales

### 4.1 Parser

Responsabilidad:
- Interpretar el DSL
- Construir el AST

Entrada:
- String DSL

Salida:
- DocumentNode (raíz del AST)

No debe:
- Evaluar lógica
- Resolver datos
- Renderizar

---

### 4.2 AST (Abstract Syntax Tree)

Estructura jerárquica que representa el documento.

#### Nodo base:

- DocumentNode (abstracto)

Propiedades comunes:
- Type (Tipo)
- Children (Hijos)
- Properties
- Style

> El `DocumentNode` no expone una propiedad pública `Id`. Los identificadores de layout `node_{n}` los genera el `LayoutEngine` (en `LayoutInfo`), no son propiedad del nodo.

#### Tipos de nodos derivados:

- TextNode
- ContainerNode
- ConditionalNode
- LoopNode
- TableNode
- ImageNode

Relaciones:
- Un nodo puede contener múltiples nodos hijos
- La estructura es recursiva

---

### 4.3 Evaluator

Responsabilidad:
- Evaluar condiciones
- Resolver expresiones
- Iterar estructuras
- Reemplazar variables con datos reales

Entrada:
- AST
- Datos del documento

Salida:
- Modelo evaluado (AST enriquecido o estructura intermedia)

---

### 4.4 Data Resolver

Responsabilidad:
- Mapear datos externos al modelo del documento
- Resolver referencias
- Validar existencia de datos

Entrada:
- Modelo de datos externo
- Referencias del DSL

Salida:
- Dataset estructurado utilizable por el evaluator

---

### 4.5 Renderer

Responsabilidad:
- Convertir el modelo evaluado en una salida concreta

Tipos de render:

- ESC/POS
- PDF
- Texto plano (debug)
- Vista previa UI

Entrada:
- Modelo evaluado

Salida:
- Representación final

---

### 4.6 Layout Engine (adaptación por perfil)

Responsabilidad:
- Adaptar el documento según capacidades del dispositivo (`ILayoutEngine`)

Ejemplos:
- Tamaño de papel
- Resolución
- Encoding
- Limitaciones de impresión

Entrada:
- `EvaluatedDocument`
- Perfil de impresora (`DeviceProfile`)

Salida:
- `LayoutedDocument` (documento adaptado al perfil)

---

## 5. Interfaces principales

### 5.1 Parser

```text
IDslParser
- Parse(string dsl) -> DocumentTemplate
````

---

### 5.2 Evaluator

```text
IEvaluator
- Evaluate(DocumentNode ast, object? data) -> EvaluatedDocument
```

---

### 5.3 Renderer

```text
IRenderer
- Target { get; }
- Render(LayoutedDocument document, DeviceProfile profile) -> RenderResult
```

---

### 5.4 Data Resolver

```text
IDataResolver
- Resolve(object? data, string path) -> object?
- ResolveCollection(object? data, string path) -> IEnumerable<object>
```

---

### 5.5 Layout Engine (adaptación por perfil)

```text
ILayoutEngine
- ApplyLayout(EvaluatedDocument document, DeviceProfile profile) -> LayoutedDocument
```

> No existe un `IProfileAdapter`: la adaptación del documento al perfil del
> dispositivo la realiza `ILayoutEngine.ApplyLayout`, que produce un
> `LayoutedDocument`.

---

## 6. Modelo de datos lógico

### Document

Representa el documento completo.

Contiene:

* RootNode (AST)
* Metadata
* Configuración

---

### EvaluatedDocument

Documento ya procesado con:

* Valores resueltos
* Condiciones evaluadas
* Iteraciones expandidas

---

### RenderOutput

Resultado final del renderizado.

Puede variar según tipo:

* Bytes (ESC/POS)
* PDF
* String (debug)
* UI model

---

## 7. Principios de diseño

* Separación de responsabilidades
* Extensibilidad mediante interfaces
* Independencia entre parsing, evaluación y renderizado
* AST como núcleo del sistema
* Pipeline desacoplado por etapas
* Plug-in friendly para renderizadores

---

## 8. Extensibilidad

El sistema permite extender:

* Nuevos tipos de nodos
* Nuevos renderers
* Nuevos evaluadores
* Nuevas fuentes de datos
* Nuevos perfiles de dispositivo

Mediante:

* Interfaces
* Inyección de dependencias
* Registro de componentes

---

## 9. Consideraciones de ejecución

* El parser no debe depender de datos externos
* El evaluator no debe conocer detalles de renderizado
* El renderer no debe modificar lógica del modelo
* Cada etapa produce una representación intermedia clara

---

## 10. Relación con otros documentos

* flujo-ejecucion-motor_v1.0.md → describe el flujo conceptual
* modelo-datos-logico_v1.0.md → define estructuras de datos
* contratos-del-motor_v1.0.md → define interfaces públicas
* extensibilidad-motor_v1.0.md → define mecanismos de extensión

Este documento traduce esos conceptos a estructuras implementables en código.

---

## 11. Resultado esperado en implementación

A partir de este modelo, el código debería reflejar:

* Un AST fuertemente tipado
* Componentes desacoplados por interfaces
* Pipeline de procesamiento en etapas
* Renderers intercambiables
* Evaluación independiente del renderizado

---