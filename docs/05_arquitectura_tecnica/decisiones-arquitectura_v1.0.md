# Decisiones de Arquitectura (ADR) — Sistema de Gestión de Reclamos  
**Proyecto:** Motor DSL para generación y renderizado de documentos
**Archivo:** decisiones-arquitectura_v1.0.md  
**Versión:** 1.0  
**Fecha:** 2026-03-02  
**Autor:** Equipo Técnico  
**Estado:** Activo  
**Tipo:** Documento de decisiones arquitectónicas (ADR simplificado)
**Objetivo:** Registrar decisiones clave de diseño, justificar alternativas y establecer lineamientos técnicos del sistema

---

## 🎯 Propósito

Documentar las decisiones arquitectónicas fundamentales que guían el diseño e implementación del motor DSL, asegurando:

- Consistencia en el desarrollo
- Trazabilidad de decisiones
- Facilidad de mantenimiento
- Alineación entre componentes
- Guía para herramientas de codificación asistida (Copilot)

---

## 🧠 Principios arquitectónicos adoptados

### 1. Separación de responsabilidades

Cada componente del sistema tiene una única responsabilidad:

- Parser → interpreta DSL
- Evaluator → aplica lógica
- Renderer → genera salida

---

### 2. Pipeline en etapas

El sistema se diseña como un pipeline secuencial:

```text
DSL → AST → EvaluatedDocument → RenderOutput
````

Cada etapa produce una representación intermedia clara.

---

### 3. AST como núcleo del sistema

El Abstract Syntax Tree (AST) es la estructura central que representa el documento.

Decisión:

* Todo el procesamiento gira en torno al AST
* El AST es independiente de la salida final

---

### 4. Uso de interfaces

Se adoptan interfaces para desacoplar implementaciones:

* IDslParser
* IEvaluator
* IRenderer
* IDataResolver
* ILayoutEngine

Beneficio:

* Permite extensibilidad
* Facilita testing
* Permite múltiples implementaciones

---

### 5. Independencia entre etapas

Cada etapa del pipeline no conoce detalles internos de las demás:

* Parser no conoce evaluación
* Evaluator no conoce rendering
* Renderer no conoce parsing

---

### 6. Modelo inmutable en evaluación

El EvaluatedDocument se considera una representación derivada:

* No modifica el AST original
* Se genera como resultado de evaluación
* Evita efectos colaterales

---

## 🏗️ Decisiones técnicas clave

### 1. Lenguaje y plataforma

* Tecnología: .NET (C#)
* Tipo de proyecto: Class Libraries
* Arquitectura: modular por proyectos

---

### 2. Estructura de proyectos

Se define separación por capas (7 paquetes):

* MotorDsl.Core → modelos, AST, interfaces, evaluador (namespace `MotorDsl.Core.Evaluators`), layout y validadores
* MotorDsl.Parser → parsing DSL
* MotorDsl.Rendering → generación de salida (TextRenderer, EscPosRenderer)
* MotorDsl.Extensions → extensibilidad / DI
* MotorDsl.Printing.Abstractions → contratos de impresión y orquestador
* MotorDsl.Bluetooth → transport Bluetooth (Android)
* MotorDsl.Maui → controles y renderers MAUI (PDF, ESC/POS bitmap, raster)

> No existe un proyecto `MotorDsl.Evaluator`: el Evaluator vive dentro de
> `MotorDsl.Core` (namespace `MotorDsl.Core.Evaluators`).

---

### 3. Representación del AST

Decisión:

* Se utiliza una jerarquía de clases

Ejemplo conceptual:

* DocumentNode (abstracto, base)

  * TextNode
  * ContainerNode
  * ConditionalNode
  * LoopNode
  * TableNode
  * ImageNode

Motivo:

* Facilita extensibilidad
* Permite composición jerárquica
* Compatible con recorridos recursivos

---

### 4. DSL (lenguaje de plantillas)

Decisión:

* DSL textual interpretado (no compilado)
* Parsing manual o basado en reglas

Motivo:

* Flexibilidad
* Control total del parsing
* Independencia de herramientas externas

---

### 5. Evaluación basada en contexto

Se introduce DataContext:

* Fuente de datos dinámica
* Utilizada por el evaluator
* Permite resolución de variables

Motivo:

* Desacoplar lógica del origen de datos
* Permitir múltiples fuentes (API, DB, mocks)

---

### 6. Estrategia de rendering

Decisión:

* Renderer intercambiable por tipo de salida

Tipos previstos:

* ESC/POS
* PDF
* Texto plano (debug)
* UI preview

Motivo:

* Permite múltiples outputs sin afectar lógica central

---

### 7. Manejo de condiciones

Decisión:

* Evaluación de condiciones en el evaluator
* No en el parser

Motivo:

* Separación de parsing vs lógica
* Mayor flexibilidad en expresiones

---

### 8. Iteraciones (loops)

Decisión:

* Expansión de loops durante la evaluación
* Generación de múltiples nodos en EvaluatedDocument

Motivo:

* Evita lógica compleja en rendering
* Mantiene coherencia en el modelo evaluado

---

## 🚫 Decisiones descartadas

### ❌ Compilación del DSL

No se compila a código intermedio.

Motivo:

* Complejidad innecesaria en esta etapa
* Se prioriza interpretación dinámica

---

### ❌ Renderizado acoplado al evaluator

Se evita que el evaluator genere salida directamente.

Motivo:

* Rompe separación de responsabilidades
* Reduce extensibilidad

---

### ❌ Uso directo de templates rígidos sin AST

No se trabaja con templates sin estructura intermedia.

Motivo:

* Dificulta evaluación de lógica compleja
* Reduce capacidad de extensión

---

## 🔄 Evolución de decisiones

Este documento puede evolucionar en versiones futuras:

* Nuevos tipos de nodos
* Nuevos renderers
* Nuevas estrategias de evaluación
* Optimización del pipeline
* Introducción de caching o compilación intermedia

Cada cambio significativo deberá registrarse como una nueva decisión o actualización.

---

## 🧩 Relación con otros documentos

* modelo-ejecucion-logico_v1.0.md → define cómo se ejecuta el sistema
* modelo-datos-logico_v1.0.md → define estructuras de datos
* flujo-ejecucion-motor_v1.0.md → describe el flujo conceptual
* contratos-del-motor_v1.0.md → define interfaces públicas
* extensibilidad-motor_v1.0.md → define mecanismos de extensión

---

## 🎯 Impacto en desarrollo asistido por IA

Este documento es clave para:

* Guiar a Copilot en decisiones consistentes
* Evitar divergencias en implementaciones
* Establecer reglas claras de diseño
* Mantener coherencia entre módulos

Copilot debe respetar estas decisiones al generar código.

---

````

---

# 🧠 Cómo usar este documento con Copilot

En la práctica:

- Copilot no “lee” documentos automáticamente
- Pero vos lo guiás con comentarios como:

```csharp
// Basado en decisiones-arquitectura_v1.0.md:
// - Separación de responsabilidades
// - AST como núcleo
// - Evaluación desacoplada del rendering
````

👉 Eso condiciona mucho mejor las sugerencias.

