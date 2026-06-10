# 📄 Plan de Iteración — Sprint 03
**Archivo:** plan-iteracion_sprint-03_v1.0.md
**Versión:** 1.0  
**Fecha:** 2026-03-28

## 📋 Información general

* Proyecto: Motor DSL para generación y renderizado de documentos (ESC/POS / PDF / Preview)
* Sprint: 03
* Duración: 2 semanas
* Fecha inicio: 29/04/2026
* Fecha fin: 12/05/2026
* Objetivo del sprint: Implementar el Motor de Layout, primer Renderizador (texto plano) y la Orquestación completa del motor, cerrando el pipeline end-to-end.

---

## 🎯 Objetivo del Sprint

Implementar el **pipeline completo del motor**, habilitando:

* Cálculo de layout según restricciones del dispositivo
* Generación de representación abstracta independiente del renderer
* Implementación de renderizador de texto plano (debug)
* Orquestación completa: DSL → Parser → Evaluator → Layout → Renderer
* Pipeline funcional de punta a punta

👉 Este sprint cierra el **MVP funcional del motor**, permitiendo procesar un DSL completo hasta generar una salida renderizada.

---

## 🧩 Historias / tareas comprometidas

| ID    | Tipo     | Descripción                                           | Prioridad | Estimación |
| ----- | -------- | ----------------------------------------------------- | --------- | ---------- |
| US-09 | Historia | Calcular layout del documento según DeviceProfile     | Alta      | 13 pts     |
| US-10 | Historia | Generar representación abstracta del documento        | Alta      | 8 pts      |
| US-11 | Historia | Renderizar documento a texto plano (debug/preview)    | Alta      | 8 pts      |
| US-12 | Historia | Orquestar pipeline completo (DSL → Render)           | Alta      | 8 pts      |
| TK-13 | Técnica  | Implementar interfaz ILayoutEngine                    | Alta      | 3 pts      |
| TK-14 | Técnica  | Implementar layout básico (vertical/horizontal)       | Alta      | 8 pts      |
| TK-15 | Técnica  | Implementar adaptación a ancho de dispositivo         | Alta      | 5 pts      |
| TK-16 | Técnica  | Implementar interfaz IRenderer base                   | Alta      | 3 pts      |
| TK-17 | Técnica  | Implementar TextRenderer (salida plana)               | Alta      | 8 pts      |
| TK-18 | Técnica  | Implementar IDocumentEngine (orquestación)            | Alta      | 8 pts      |
| TK-19 | Técnica  | Crear tests unitarios para layout y renderer          | Media     | 5 pts      |

---

## 🏗️ Alcance técnico del sprint

### Motor Core + Layout + Rendering

* Implementación de:

  * ILayoutEngine (interfaz)
  * LayoutEngine : ILayoutEngine (algoritmos de distribución, namespace `MotorDsl.Core.Layout`; método `ApplyLayout(EvaluatedDocument, DeviceProfile)`)
  * IRenderer base (contrato para todos los renderers)
  * TextRenderer (renderer de texto plano)
  * IDocumentEngine (orquestador principal)
* Cálculo de layout:

  * Distribución vertical/horizontal
  * Ajuste a ancho de dispositivo
  * Gestión de restricciones del perfil
* Representación abstracta:

  * Estructura intermedia independiente del dispositivo
  * Compatible con múltiples renderers
* Renderización a texto plano:

  * Debug legible
  * Reflects estructura del documento
  * Independiente de dispositivo específico

---

## 🔁 Flujo completo implementado hasta este sprint

```
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
TextRenderer (Sprint 03)
 ↓
RenderResult (texto plano)
```

---

## 🔧 Capacidades que se habilitan

**CU habilitados por este sprint:**

* CU-04: Ejecutar motor de layout
* CU-05: Generar representación abstracta
* CU-07: Renderizar a texto plano (debug)

**Flujo completo:**

* Entrada: DSL + datos + DeviceProfile
* Salida: Documento renderizado en texto plano
* Uso: Validación, debug, preview

---

## 🚫 Fuera de alcance

* Renderer ESC/POS (impresoras térmicas) → Sprint 04
* Renderer UI MAUI (preview visual) → Sprint 04+
* Renderer PDF → Sprint 05+
* Reflow avanzado / optimizaciones → Sprint 04+
* Suporte de caracteres especiales avanzados → Future
* Compresión de output → Future

---

## ✅ Criterios de terminado (Definition of Done)

Una tarea se considera terminada cuando:

* LayoutEngine calcula correctamente la distribución según DeviceProfile
* La representación abstracta es generada e independiente del renderer
* TextRenderer produce salida legible y estructurada
* El pipeline completo DSL → Render funciona end-to-end
* Tests unitarios cubren casos principales:

  * Cálculo de layout simple
  * Adaptación a ancho del dispositivo
  * Representación abstracta de documento complejo
  * Renderizado de texto plano
  * Integración de todas las etapas
* No hay acoplamiento innecesario entre LayoutEngine y renderizadores
* El código mantiene separación de responsabilidades
* IDocumentEngine proporciona interfaz unificada para consumidores

---

## 🧪 Plan de pruebas del sprint

### Pruebas unitarias

* Cálculo de layout:

  * Distribución vertical de elementos
  * Distribución horizontal
  * Ajuste a ancho máximo
* Representación abstracta:

  * Independencia de formato
  * Preservación de estructura
* Renderizador de texto:

  * Conversión correcta de nodos
  * Formato legible
  * Preservación de jerarquía

### Pruebas de integración

* DSL + Parser + Evaluator + Layout + TextRenderer completo
* Diferentes tipos de documentos (simple, condicional, loop)
* DeviceProfile aplicado correctamente

Ejemplo:

* Input: DSL con loop + condicional + datos
* Output: Documento evaluado, maquetado y renderizado a texto

---

## 🚀 Entregables del sprint

* LayoutEngine implementado (ILayoutEngine)
* Algoritos de layout básicos (vertical/horizontal)
* Adaptación a restricciones de dispositivo
* IRenderer base definida
* TextRenderer funcional (renders a string)
* IDocumentEngine completo y funcional
* Pipeline end-to-end operacional
* Suite de tests unitarios y de integración
* Documentación de interfaces públicas
* Ejemplo de uso completo del motor

---

## ⚠️ Riesgos identificados

* Complejidad en el cálculo de layout para estructuras anidadas
* Inconsistencias entre DSL → Evaluator → Layout → Renderer
* Manejo de errores incompleto en alguna etapa del pipeline
* Performance en documentos grandes (no es mi prioridad, pero podría ser issue)
* Criterios de aceptación ambiguos en la representación abstracta
* Dependencias circulares entre módulos
* Subestimación del esfuerzo en integración end-to-end

---

## 🔄 Métricas de seguimiento

* % del pipeline funcional (cada etapa)
* Cobertura de tests de layout y renderer
* Casos de documentos correctamente procesados
* Defectos encontrados en integración
* Performance de renderizado de texto
* Velocidad del equipo vs historias comprometidas

---

## 🧠 Resultado esperado al finalizar el Sprint 03

El motor será capaz de:

* Interpretar un DSL completo
* Convertirlo en AST
* Evaluarlo con datos reales
* Calcular su layout según el dispositivo
* Renderizarlo a texto plano legible

**Ejemplo de MVP funcional:**

```
Input DSL:
{
  "id": "ejemplo",
  "root": {
    "type": "container",
    "layout": "vertical",
    "children": [
      { "type": "text", "text": "Título: {{titulo}}" },
      { "type": "loop", "source": "items", "body": { "type": "text", "text": "- {{item}}" } }
    ]
  }
}

Input Data:
{ "titulo": "Mi Documento", "items": ["A", "B", "C"] }

Output (Texto Plano):
─────────────────────
Título: Mi Documento
- A
- B
- C
─────────────────────
```

Este será el **primer MVP completamente funcional** del motor DSL.
