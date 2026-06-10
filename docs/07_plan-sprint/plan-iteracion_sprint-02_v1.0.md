--
# 📄 Plan de Iteración — Sprint 02
**Archivo:** plan-iteracion_sprint-02_v1.0.md
**Versión:** 1.0  
**Fecha:** 2026-03-28

## 📋 Información general

* Proyecto: Motor DSL para generación y renderizado de documentos (ESC/POS / PDF / Preview)
* Sprint: 02
* Duración: 2 semanas
* Fecha inicio: 15/04/2026
* Fecha fin: 28/04/2026
* Objetivo del sprint: Implementar la etapa de evaluación del motor, permitiendo procesar condiciones, variables e iteraciones sobre el AST generado en el Sprint 01.

---

## 🎯 Objetivo del Sprint

Implementar el **Evaluator del motor**, habilitando:

* Evaluación de nodos condicionales
* Evaluación de loops (iteraciones)
* Resolución de variables desde un contexto de datos
* Transformación del AST en un documento evaluado (EvaluatedDocument)

👉 Este sprint introduce la lógica dinámica del motor.

---

## 🧩 Historias / tareas comprometidas

| ID    | Tipo     | Descripción                                       | Prioridad | Estimación |
| ----- | -------- | ------------------------------------------------- | --------- | ---------- |
| US-05 | Historia | Evaluar variables en el documento DSL             | Alta      | 8 pts      |
| US-06 | Historia | Evaluar estructuras condicionales (if/else)       | Alta      | 13 pts     |
| US-07 | Historia | Evaluar estructuras de iteración (loops)          | Alta      | 13 pts     |
| US-08 | Historia | Integrar evaluator con AST generado por el parser | Alta      | 8 pts      |
| TK-07 | Técnica  | Implementar interfaz IEvaluator                   | Alta      | 3 pts      |
| TK-08 | Técnica  | Implementar EvaluatedDocument                     | Alta      | 5 pts      |
| TK-09 | Técnica  | Implementar resolución de variables (IDataResolver / DataResolver) | Alta      | 5 pts      |
| TK-10 | Técnica  | Implementar evaluación de nodos condicionales     | Alta      | 8 pts      |
| TK-11 | Técnica  | Implementar evaluación de loops (LoopNode)        | Alta      | 8 pts      |
| TK-12 | Técnica  | Crear tests unitarios para evaluator              | Media     | 5 pts      |

---

## 🏗️ Alcance técnico del sprint

### Motor Core + Evaluator

* Implementación de:

  * IEvaluator
  * EvaluatedDocument
  * IDataResolver / DataResolver (resuelve el contexto de datos; inyectado en `Evaluator(IDataResolver? dataResolver = null)`)
* Evaluación de nodos del AST:

  * TextNode → resolución de variables
  * ConditionalNode → evaluación booleana
  * LoopNode → expansión iterativa
* Recorre AST y produce una versión evaluada del documento

---

## 🔁 Flujo implementado en este sprint

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
```

---

## 🔧 Capacidades que se habilitan

* Reemplazo de variables tipo:

  * {{Nombre}}
  * {{Cliente.Direccion}}
* Condicionales tipo:

  * IF condición THEN contenido ELSE contenido
* Iteraciones tipo:

  * LOOP sobre listas de datos
* Contexto de datos dinámico (IDataResolver / DataResolver)

---

## 🚫 Fuera de alcance

* Rendering final (ESC/POS / PDF / UI)
* Perfil de dispositivos
* Optimización de performance avanzada
* Cache de evaluación
* Extensibilidad avanzada (plugins)

---

## ✅ Criterios de terminado (Definition of Done)

Una tarea se considera terminada cuando:

* El evaluator procesa correctamente un AST generado por el parser
* Variables son correctamente resueltas desde el contexto
* Condiciones booleanas funcionan correctamente
* Los loops generan múltiples nodos evaluados
* Se genera un EvaluatedDocument válido
* Tests unitarios cubren casos principales:

  * Variables simples
  * Variables anidadas
  * Condiciones true/false
  * Iteraciones sobre listas
* No hay acoplamiento con el renderer

---

## 🧪 Plan de pruebas del sprint

### Pruebas unitarias

* Resolución de variables simples
* Resolución de variables anidadas
* Evaluación de condiciones
* Evaluación de loops
* Transformación AST → EvaluatedDocument

### Pruebas de integración

* DSL + Parser + Evaluator funcionando en conjunto
* Documento completo evaluado correctamente

Ejemplo:

* Input DSL con variables + loop
* Output: documento con datos concretos ya resueltos

---

## 🚀 Entregables del sprint

* Evaluator implementado (IEvaluator)
* EvaluatedDocument definido
* IDataResolver / DataResolver funcional
* Evaluación de nodos condicionales
* Evaluación de loops
* Integración con AST generado en Sprint 01
* Suite de tests unitarios del evaluator
* Pipeline parcial funcional: DSL → AST → EvaluatedDocument

---

## ⚠️ Riesgos identificados

* Ambigüedad en el formato de expresiones DSL
* Complejidad en resolución de variables anidadas
* Manejo de listas en loops
* Evaluación de condiciones complejas
* Posibles inconsistencias entre parser y evaluator

---

## 🔄 Métricas de seguimiento

* % de AST correctamente evaluado
* Cobertura de tests del evaluator
* Casos de variables/condiciones/loops soportados
* Bugs encontrados en evaluación
* Integración correcta con parser

---

## 🧠 Resultado esperado al finalizar el Sprint 02

El motor será capaz de:

* Interpretar un DSL
* Convertirlo en AST
* Evaluarlo con datos reales
* Producir un documento estructurado listo para renderizado

