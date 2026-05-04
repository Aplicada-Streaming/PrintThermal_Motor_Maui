# Modelo de Datos Lógico — Motor DSL de Renderizado  
**Archivo:** modelo-datos-logico_v1.0.md  
**Versión:** 1.0  
**Fecha:** 2026-03-28  
**Autor:** Equipo Técnico  
**Estado:** Borrador  

---

## 1. Propósito

Este documento define el modelo de datos lógico del Motor DSL de renderizado. Describe las estructuras internas utilizadas para representar documentos, nodos del DSL, perfiles de dispositivo y resultados de renderizado.

A diferencia de un sistema transaccional tradicional, este modelo no está centrado en tablas relacionales estrictas, sino en estructuras orientadas a objetos que representan el árbol abstracto (AST) del documento.

El objetivo es proporcionar una representación consistente que soporte:

- Parsing de DSL
- Evaluación de expresiones
- Layout de contenido
- Renderizado en múltiples targets
- Extensibilidad del motor

---

## 2. Alcance

El modelo cubre:

- Representación abstracta de documentos (AST)
- Nodos del DSL
- Perfiles de dispositivo
- Resultado de renderizado
- Elementos estructurales del layout

No incluye:

- Persistencia en base de datos
- Índices físicos
- Optimización de almacenamiento
- Infraestructura específica

---

## 3. Entidades principales

El sistema se compone de los siguientes conceptos lógicos:

- DocumentTemplate  
- DocumentNode (y derivados)  
- DeviceProfile  
- RenderResult  
- Expresiones DSL  
- Contexto de ejecución  

---

## 4. Documento: DocumentTemplate

Representa una plantilla completa procesada desde DSL.

### Estructura lógica

| Campo | Tipo | Nulo | Descripción |
|------|------|------|------------|
| Id | string | No | Identificador de plantilla |
| Version | string | No | Versión del documento |
| Root | DocumentNode | No | Nodo raíz del árbol |
| Metadata | Dictionary | Sí | Información adicional |
| Format | string | No | Modalidad del documento. Default: `"template"`. Alternativa: `"integrated"`. |

### Reglas

- Root siempre debe existir
- Metadata es opcional y extensible
- Version permite control de compatibilidad
- Format discrimina la modalidad de procesamiento:
  - `"template"`: pipeline completo (Parse → Validate → Evaluate → Layout → Render). El AST contiene `{{placeholders}}`, `loop` y/o `conditional` que se resuelven en la etapa de Evaluate.
  - `"integrated"`: pipeline simplificado (Parse → Validate → Layout → Render). El AST ya está resuelto; la etapa de Evaluate se omite. No se permiten nodos `loop` ni `conditional`, y los `value`/`source` no pueden contener `{{...}}`.

---

## 5. Nodo base: DocumentNode

Entidad abstracta que representa cualquier elemento del documento.

### Estructura lógica

| Campo | Tipo | Nulo | Descripción |
|------|------|------|------------|
| Type | string | No | Tipo de nodo |
| Children | List<DocumentNode> | Sí | Hijos del nodo |
| Properties | Dictionary | Sí | Propiedades dinámicas |
| Style | StyleDefinition | Sí | Estilos aplicables |

---

### Tipos de nodos derivados

- TextNode  
- ContainerNode  
- TableNode  
- ConditionalNode  
- LoopNode  
- ImageNode  

---

## 6. Nodo: TextNode

Representa contenido textual.

### Estructura

| Campo | Tipo | Nulo | Descripción |
|------|------|------|------------|
| Text | string | No | Texto a renderizar |
| BindPath | string | Sí | Path a datos dinámicos |

### Reglas

- Puede contener interpolación de datos  
- Puede ser estático o dinámico  

---

## 7. Nodo: ContainerNode

Contenedor estructural.

### Estructura

| Campo | Tipo | Nulo | Descripción |
|------|------|------|------------|
| Layout | string | Sí | Tipo de layout (vertical, horizontal) |
| Children | List<DocumentNode> | No | Elementos contenidos |

### Reglas

- Organiza el flujo visual  
- No tiene contenido directo  

---

## 8. Nodo: ConditionalNode

Permite lógica condicional.

### Estructura

| Campo | Tipo | Nulo | Descripción |
|------|------|------|------------|
| Expression | string | No | Expresión DSL |
| TrueBranch | DocumentNode | No | Nodo si verdadero |
| FalseBranch | DocumentNode | Sí | Nodo alternativo |

### Reglas

- Expression debe evaluarse en runtime  
- Soporta variables del contexto de datos  

---

## 9. Nodo: LoopNode

Permite iteración sobre colecciones.

### Estructura

| Campo | Tipo | Nulo | Descripción |
|------|------|------|------------|
| Source | string | No | Path a colección |
| ItemAlias | string | No | Alias del elemento |
| Body | DocumentNode | No | Contenido iterado |

### Reglas

- Source debe resolver a una colección  
- ItemAlias se usa en bindings internos  

---

## 10. Perfil de dispositivo: DeviceProfile

Define capacidades del dispositivo de renderizado.

### Estructura lógica

| Campo | Tipo | Nulo | Descripción |
|------|------|------|------------|
| Name | string | No | Nombre del perfil |
| Width | int | No | Ancho disponible |
| RenderTarget | string | No | Tipo de salida |
| Capabilities | Dictionary | Sí | Capacidades específicas |

### RenderTargets posibles

- escpos  
- ui  
- pdf  
- text  
- image  

---

## 11. Resultado de renderizado: RenderResult

Representa la salida del motor.

### Estructura lógica

| Campo | Tipo | Nulo | Descripción |
|------|------|------|------------|
| Target | string | No | Tipo de salida |
| Output | object | No | Resultado generado |
| Warnings | List<string> | Sí | Advertencias |
| Errors | List<string> | Sí | Errores |

### Reglas

- Output depende del renderer  
- Puede contener texto, bytes o estructuras UI  

---

## 12. Expresiones DSL

Las expresiones permiten lógica dinámica dentro del documento.

### Tipos de uso

- Condiciones (ConditionalNode)  
- Iteraciones (LoopNode)  
- Binding de datos (TextNode)  

### Características

- Evaluadas en runtime  
- Acceden al contexto de datos  
- Sintaxis definida por el parser DSL  

---

## 13. Contexto de ejecución

Representa el entorno durante el renderizado.

### Contenido

- Datos de entrada (object)  
- Variables temporales  
- Resultado de evaluaciones intermedias  

---

## 14. Relaciones

```text id="relaciones-001"
DocumentTemplate
    └── Root → DocumentNode (árbol jerárquico)

DocumentNode
    ├── Children → DocumentNode*
    ├── Style → StyleDefinition
    └── Properties → Dictionary

ConditionalNode
    ├── TrueBranch → DocumentNode
    └── FalseBranch → DocumentNode

LoopNode
    └── Body → DocumentNode

DeviceProfile
    └── influye en → Layout + Renderer

RenderResult
    └── resultado de → IDocumentEngine
````

---

## 15. Consideraciones de integridad lógica

* El árbol de DocumentNode no debe contener ciclos
* Root es único por DocumentTemplate
* ConditionalNode debe tener al menos una rama válida
* LoopNode debe referenciar una colección existente
* DeviceProfile debe ser consistente con el renderer seleccionado

---

## 16. Consideraciones de extensibilidad

El modelo está diseñado para permitir:

* Nuevos tipos de nodos sin modificar los existentes
* Nuevas propiedades dinámicas mediante Dictionary
* Incorporación de nuevos targets de renderizado
* Extensión del sistema de expresiones DSL

---

## 17. Trazabilidad

Este modelo deriva de:

* arquitectura-solucion_v1.0.md
* contratos-del-motor_v1.0.md
* CU relacionados al renderizado
* RN-02 independencia del render
* RC-04 elementos soportados por el motor
* RC-05 independencia del documento abstracto
* RC-06 perfil de dispositivo

---

## 18. Riesgos conocidos

* Complejidad creciente del AST con DSL avanzado
* Evaluación de expresiones puede impactar performance
* Necesidad de validación estricta del árbol
* Posible sobreextensión del modelo dinámico (Dictionary)

---

## 19. Evolución prevista

Posibles extensiones:

* Nodo de tablas avanzado con estilos complejos
* Nodo de componentes reutilizables (partials)
* Sistema de macros o includes
* Tipado fuerte de expresiones DSL
* Caché de evaluación de nodos
* Optimización del AST (pruning / pre-evaluación)

---

## 20. Historial de versiones

| Versión | Fecha      | Autor          | Cambios               |
| ------- | ---------- | -------------- | --------------------- |
| 1.0     | 2026-03-28 | Equipo Técnico | Modelo lógico inicial |

---