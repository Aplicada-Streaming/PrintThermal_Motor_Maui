# Matriz de Cobertura de Pruebas — Motor DSL de Renderizado  
**Archivo:** matriz-cobertura-pruebas_v1.0.md  
**Versión:** 1.0  
**Fecha:** 2026-03-28  
**Autor:** Equipo Técnico  
**Estado:** Activo  

---

## 1. Propósito

Establecer la trazabilidad entre:

- Componentes del motor  
- Reglas de negocio  
- Casos de prueba  
- Tipos de pruebas  

El objetivo es asegurar que cada parte crítica del sistema esté validada mediante pruebas adecuadas y evitar áreas sin cobertura.

---

## 2. Alcance

Aplica a todos los módulos del motor DSL:

- Parser  
- AST / Document Model  
- Evaluador de expresiones  
- Data Resolver  
- Layout Engine  
- Renderers  
- Motor de ejecución  
- Extensibilidad  

---

## 3. Estructura de la matriz

La matriz relaciona:

- **Componente**
- **Responsabilidad**
- **Tipo de prueba**
- **Casos de prueba referenciales**
- **Cobertura esperada**
- **Estado**

---

# 4. Matriz de cobertura

| Componente | Responsabilidad | Tipo de prueba | Casos relacionados | Cobertura esperada | Estado |
|-----------|---------------|---------------|-------------------|------------------|--------|
| Parser DSL | Interpretar DSL y generar AST | Unit / Integration | CP-001, CP-002, CP-012 | Alta | Pendiente |
| AST (Document Model) | Representación estructurada del documento | Unit | CP-006 | Alta | Pendiente |
| Expresiones | Evaluación de condiciones y lógica | Unit | CP-003, CP-010 | Alta | Pendiente |
| Data Resolver | Resolución de datos dinámicos | Unit / Integration | CP-002, CP-011 | Alta | Pendiente |
| Layout Engine | Adaptación del documento al dispositivo | Unit / Snapshot | CP-007 | Alta | Pendiente |
| Renderer texto / PDF / raster-preview | Renderizado a texto plano, PDF y previsualización pixelada | Integration / Snapshot | CP-013 | Media | Pendiente |
| Renderer ESC/POS | Renderizado para impresión | Integration / Snapshot | CP-008, CP-013 | Alta | Pendiente |
| Motor de ejecución | Orquestación del pipeline completo | Integration / Snapshot | CP-009 | Alta | Pendiente |
| Condicionales | Evaluación de nodos If | Unit | CP-003 | Alta | Pendiente |
| Iteraciones | Evaluación de loops | Unit / Integration | CP-005 | Alta | Pendiente |
| Jerarquía de nodos | Estructura del documento | Unit | CP-006 | Alta | Pendiente |
| Manejo de errores | Validación y reporting | Unit / Integration | CP-012 | Alta | Pendiente |
| Device Profiles | Adaptación de salida | Integration | CP-007, CP-013 | Alta | Pendiente |
| Encoding de texto | Manejo de caracteres especiales | Unit | CP-015 | Media | Pendiente |
| Documento vacío | Casos borde | Unit | CP-014 | Media | Pendiente |

> **Nota sobre renderers.** Los `IRenderer` reales y sus targets son: `text` (TextRenderer), `escpos` (EscPosRenderer) y, en MAUI, `pdf`, `escpos-bitmap` y `raster-preview`. No existe un renderer con target `"UI"`: la previsualización en pantalla se arma desde el `LayoutedDocument` con controles MAUI (`MauiDocumentPreview` / `MauiRasterPreview`), no es un target de `IRenderer`.

---

## 5. Cobertura por tipo de prueba

### Unit Tests

Cubren:

- Parser DSL  
- AST  
- Expresiones  
- Data Resolver  
- Nodos individuales  

**Objetivo:** lógica aislada y rápida ejecución.

---

### Integration Tests

Cubren:

- Parser + AST  
- AST + Layout  
- Layout + Renderer  
- Motor completo  

**Objetivo:** validar interacción entre componentes.

---

### Snapshot (Golden Tests)

Cubren:

- Output final del motor  
- Render por target  
- Comportamiento visual/estructural  

**Objetivo:** detectar regresiones.

---

### Regression Tests

Cubren:

- Casos críticos de negocio  
- Documentos representativos  
- Flujos completos  

**Objetivo:** evitar roturas por cambios.

---

## 6. Cobertura por componente crítico

### 6.1 Parser DSL

Debe cubrir:

- Sintaxis válida  
- Sintaxis inválida  
- Estructuras anidadas  
- Expresiones  

Casos: CP-001, CP-002, CP-012  

---

### 6.2 AST

Debe cubrir:

- Jerarquía  
- Navegación  
- Integridad estructural  

Caso: CP-006  

---

### 6.3 Expresiones

Debe cubrir:

- Operadores lógicos  
- Comparaciones  
- Evaluación en contexto  

Casos: CP-003, CP-010  

---

### 6.4 Layout Engine

Debe cubrir:

- Reflow  
- Restricciones de tamaño  
- Adaptación por DeviceProfile  

Caso: CP-007  

---

### 6.5 Renderers

Debe cubrir:

- Consistencia de output  
- Diferencias por target  
- Independencia del AST  

Casos: CP-008, CP-013  

---

### 6.6 Motor completo

Debe cubrir:

- Pipeline end-to-end  
- Integración de todos los módulos  

Caso: CP-009  

---

## 7. Identificación de gaps de cobertura

Áreas potencialmente críticas a vigilar:

- Nuevos nodos DSL no cubiertos por tests  
- Nuevos renderers sin snapshots  
- Expresiones complejas no testadas  
- Combinaciones de layout + render  
- Casos extremos de datos  

---

## 8. Criterios de cobertura mínima

Se recomienda:

- ≥ 70% en componentes core  
- ≥ 80% en parser y expresiones  
- ≥ 70% en layout engine  
- ≥ 70% en renderers críticos  

---

## 9. Estrategia de mantenimiento

La matriz debe actualizarse cuando:

- Se agregan nuevos nodos DSL  
- Se agregan nuevos renderers  
- Se modifican reglas de negocio  
- Se incorporan nuevos casos de prueba  

---

## 10. Uso práctico

Esta matriz se utiliza para:

- Validar cobertura de tests  
- Identificar áreas sin pruebas  
- Guiar implementación de nuevos tests  
- Auditorías técnicas  
- Control de calidad del motor  

---

## 11. Relación con otros documentos

- casos-prueba-referenciales_v1.0.md  
- estrategia-testing-motor_v1.0.md  
- estrategia-calidad-motor_v1.0.md  
- plan de pruebas  
- backlog técnico  
- arquitectura del motor  

---

## 12. Control de versiones

| Versión | Fecha | Autor | Descripción |
|--------|------|------|------------|
| 1.0 | 2026-03-28 | Equipo Técnico | Matriz inicial de cobertura |

---