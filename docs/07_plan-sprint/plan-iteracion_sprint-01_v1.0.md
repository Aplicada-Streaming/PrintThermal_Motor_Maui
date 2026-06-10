--
# 📄 Plan de Iteración — Sprint 01
**Archivo:** plan-iteracion_sprint-01_v1.0.md
**Versión:** 1.0  
**Fecha:** 2026-03-28

## 📋 Información general

* Proyecto: Motor DSL para generación y renderizado de documentos (ESC/POS / PDF / Preview)
* Sprint: 01
* Duración: 2 semanas
* Fecha inicio: 01/04/2026
* Fecha fin: 14/04/2026
* Objetivo del sprint: Inicializar el núcleo del motor e implementar el pipeline base hasta la construcción del AST desde un DSL simple.

---

## 🎯 Objetivo del Sprint

Implementar el **MVP técnico del núcleo del motor**, permitiendo:

* Crear la estructura base de la solución
* Definir el modelo lógico (AST)
* Implementar el parser DSL básico
* Generar un AST desde una plantilla DSL simple
* Validar la interpretación estructural del documento

👉 Este sprint se enfoca exclusivamente en el **núcleo del motor (Core + Parser)**, sin rendering final.

---

## 🧩 Historias / tareas comprometidas

| ID    | Tipo     | Descripción                                                  | Prioridad | Estimación |
| ----- | -------- | ------------------------------------------------------------ | --------- | ---------- |
| US-01 | Historia | Inicializar estructura base del motor (solution + proyectos) | Alta      | 5 pts      |
| US-02 | Historia | Definir modelo AST base (Node y derivados)                   | Alta      | 8 pts      |
| US-03 | Historia | Implementar parser DSL básico                                | Alta      | 13 pts     |
| US-04 | Historia | Procesar plantilla DSL simple y generar AST                  | Alta      | 8 pts      |
| TK-01 | Técnica  | Crear solución .sln y proyectos (Core, Parser, etc.)         | Alta      | 3 pts      |
| TK-02 | Técnica  | Implementar clase abstracta Node                             | Alta      | 3 pts      |
| TK-03 | Técnica  | Implementar nodos básicos (DocumentNode, TextNode)           | Alta      | 5 pts      |
| TK-04 | Técnica  | Definir interfaces IDslParser                                | Alta      | 3 pts      |
| TK-05 | Técnica  | Implementar parser inicial (DSL simple)                      | Alta      | 8 pts      |
| TK-06 | Técnica  | Crear estructura de tests unitarios base                     | Media     | 3 pts      |

---

## 🏗️ Alcance técnico del sprint

### Backend / Motor

* Creación de solution y proyectos:

  * MotorDsl.Core
  * MotorDsl.Parser
  * MotorDsl.Tests
* Definición del AST (modelo lógico)
* Definición de interfaces base:

  * IDslParser
* Implementación inicial del parser DSL
* Soporte para:

  * Texto plano
  * Estructura básica de documento
* Generación de AST desde string DSL

---

## 🚫 Fuera de alcance (por ahora)

* Evaluator (condiciones, loops)
* Rendering (ESC/POS, PDF, UI)
* Integración con dispositivos
* Persistencia de datos externos
* Perfil de impresoras
* Plugins/extensibilidad avanzada

👉 Todo esto queda para sprints posteriores.

---

## ✅ Criterios de terminado (Definition of Done)

Una tarea se considera terminada cuando:

* El código compila correctamente
* Está integrado en la solution
* Tiene pruebas unitarias básicas (cuando aplica)
* El AST se genera correctamente desde un DSL simple
* Se respeta la separación de responsabilidades
* No existen dependencias cruzadas indebidas entre módulos
* Se documenta cualquier decisión relevante

---

## 🧪 Plan de pruebas del sprint

### Pruebas unitarias

* Creación de nodos del AST
* Estructura jerárquica de nodos
* Parser con DSL simple (texto plano)

### Pruebas de integración

* Input DSL → Output AST
* Validación de estructura del árbol generado

Ejemplo de validación:

* Un DSL de entrada debe generar:

  * DocumentNode raíz
  * Uno o más TextNode hijos

---

## 🚀 Entregables del sprint

* Solution .NET inicial creada
* Proyectos base estructurados
* Modelo AST implementado
* Parser DSL básico funcional
* Tests unitarios iniciales
* Código compilable y organizado según arquitectura

---

## ⚠️ Riesgos identificados

* Ambigüedad en la definición inicial del DSL
* Falta de casos de ejemplo DSL suficientemente claros
* Complejidad en el diseño del AST (posibles cambios en iteraciones futuras)
* Subestimación del alcance del parser

---

## 🔄 Métricas de seguimiento

* Velocidad del equipo
* Historias completadas vs comprometidas
* Cobertura de tests del núcleo
* AST generado correctamente desde ejemplos DSL
* Defectos detectados en parsing

---
