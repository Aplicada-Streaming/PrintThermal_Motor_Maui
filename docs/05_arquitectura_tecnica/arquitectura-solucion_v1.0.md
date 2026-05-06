# Arquitectura de la Solución — Motor de Renderizado DSL  
**Archivo:** arquitectura-solucion_v1.0.md
**Versión:** 1.0  
**Fecha:** 2026-03-28  
**Autor:** Equipo Técnico  
**Estado:** Histórico  

> **⚠️ Documento histórico — versión 1.0.** Para la versión actual con los nuevos paquetes (`MotorDsl.Printing.Abstractions`, `MotorDsl.Bluetooth`, `MotorDsl.Maui`) ver [arquitectura-solucion_v1.1.md](./arquitectura-solucion_v1.1.md).

---

## 1. Propósito

Este documento describe la arquitectura técnica propuesta para el Motor de Renderizado DSL. Define los componentes principales, sus responsabilidades y cómo interactúan para transformar plantillas DSL y datos en documentos renderizados en múltiples formatos (ESC/POS, UI, texto plano y PDF futuro).

El objetivo es establecer una base arquitectónica modular, extensible y desacoplada que permita evolucionar el motor sin afectar los distintos renderizadores ni los consumidores del sistema.

---

## 2. Alcance

La arquitectura cubre:

- Motor de parsing de DSL  
- Construcción de modelo abstracto del documento  
- Motor de layout  
- Renderizadores (ESC/POS, UI, Text, PDF futuro)  
- Gestión de perfiles de dispositivo  
- Integración con fuentes de datos  
- Extensibilidad del sistema  

No incluye detalles de infraestructura de despliegue, configuración de servidores ni aspectos específicos de networking.

---

## 3. Estilo arquitectónico

Se adopta una **arquitectura modular basada en capas y pipeline de procesamiento**, con separación clara entre:

- Entrada (DSL + datos)
- Procesamiento (parsing, validación, resolución)
- Modelo abstracto intermedio
- Renderización (múltiples targets)

Principios clave:

- Alta cohesión por módulo  
- Bajo acoplamiento entre componentes  
- Independencia del renderizador  
- Extensibilidad mediante plugins/renderizadores  
- Pipeline desacoplado  

---

## 4. Visión de alto nivel

El motor admite dos modalidades de entrada que comparten todas las etapas posteriores al Evaluator. La bifurcación ocurre justo después del Parser, según el campo `format` del documento JSON.

```text
[Plantilla DSL + Datos]      [Documento Integrado]
   (format: "template")        (format: "integrated")
            │                          │
            ▼                          ▼
                  [Parser DSL]
                         ▼
                  [Modelo Abstracto (AST)]
            │                          │
            ▼                          ▼
        [Evaluator]              (skip Evaluator —
   (resuelve {{}}, loops,         AST ya resuelto)
   conditionals)
            └──────────┬──────────────┘
                       ▼
                  [Motor de Layout]
                       ▼
            ┌──────────┼──────────┐
            ▼          ▼          ▼
        [ESC/POS]    [UI]       [Text]
                       │
                       ▼
                     [PDF*]
```

**Bifurcación del pipeline**

* **Modo clásico** (`format: "template"` o ausente — default): pipeline completo de 4 etapas. Es la entrada histórica del motor y mantiene compatibilidad total con consumidores existentes.
* **Modo integrado** (`format: "integrated"`): el JSON ya viene con todos los valores resueltos. El motor saltea la etapa de Evaluate; las etapas de Layout y Render no distinguen el origen del AST.

Los renderers, los validators de profile y el modelo intermedio `LayoutedDocument` son agnósticos al modo. Por diseño, cualquier extensión (renderers custom, providers de profiles) se beneficia automáticamente de ambas modalidades.

---

## 5. Componentes principales

### 5.1 Cargador de Plantillas DSL

**Responsabilidad**

* Obtener plantillas desde archivo, base de datos o API
* Versionar y cachear plantillas
* Preparar el contenido DSL para parsing

**Entradas**

* Identificador de plantilla
* Fuente (archivo / API / almacenamiento)

**Salida**

* Documento DSL crudo

---

### 5.2 Validador DSL

**Responsabilidad**

* Validar estructura sintáctica del DSL
* Verificar cumplimiento de esquema
* Detectar errores tempranos

**Reglas**

* Cumplimiento de RN-02
* Estructura jerárquica válida
* Tipos de elementos soportados

---

### 5.3 Parser DSL

**Responsabilidad**

* Transformar DSL en estructura interna intermedia
* Construir árbol de nodos (AST simplificado)

**Salida**

* Representación estructurada del documento

---

### 5.4 Modelo Abstracto del Documento

**Responsabilidad**

* Representar el documento independiente del renderizador
* Servir como contrato interno del motor

Incluye:

* Nodos jerárquicos
* Elementos de contenido
* Condiciones
* Iteraciones
* Referencias a datos

Este modelo cumple RN-05 (independencia del renderizador).

---

### 5.5 Resolver de Datos

**Responsabilidad**

* Obtener datos desde fuentes externas o payloads
* Mapear datos a la plantilla
* Resolver referencias dentro del DSL

Incluye:

* Resolución de variables
* Evaluación de expresiones simples
* Integración con APIs

---

### 5.6 Motor de Evaluación

**Responsabilidad**

* Evaluar condiciones (CU-17)
* Ejecutar iteraciones (CU-16)
* Resolver lógica dinámica del documento

---

### 5.7 Motor de Layout

**Responsabilidad**

* Determinar distribución espacial lógica del documento
* Aplicar reglas de alineación, agrupación y orden
* Preparar estructura para renderizado

No depende del renderizador final.

---

### 5.8 Renderizadores

**Responsabilidad**

Convertir el modelo abstracto en salida específica.

Tipos:

* ESC/POS → impresión térmica lineal
* UI → representación visual interactiva
* TEXT → salida plana para debug
* PDF → representación paginada (futuro)

Cumple RN-03 y RN-06 en cuanto a adaptación estructural.

---

### 5.9 Gestor de Perfiles de Dispositivo

**Responsabilidad**

* Definir capacidades del dispositivo
* Aplicar restricciones de salida
* Ajustar layout según el target

Incluye:

* Ancho de papel
* Fuentes soportadas
* Resolución
* Limitaciones específicas

---

### 5.10 Orquestador del Motor

**Responsabilidad**

Coordinar todo el pipeline:

1. Cargar plantilla
2. Validar DSL
3. Parsear
4. Resolver datos
5. Evaluar lógica
6. Construir modelo abstracto
7. Ejecutar layout
8. Renderizar

Actúa como punto de entrada principal del sistema.

---

## 6. Flujo principal — Renderización de documento

```text
Entrada:
  - DSL
  - Datos
  - Perfil de dispositivo

Flujo:

Cargar plantilla
   ↓
Validar DSL
   ↓
Parsear DSL
   ↓
Construir modelo abstracto
   ↓
Resolver datos
   ↓
Evaluar condiciones e iteraciones
   ↓
Ejecutar motor de layout
   ↓
Seleccionar renderizador según perfil
   ↓
Renderizar salida final
```

---

## 7. Decisiones técnicas clave

| Decisión           | Elección                | Motivo                   |
| ------------------ | ----------------------- | ------------------------ |
| Arquitectura       | Modular + Pipeline      | Extensibilidad           |
| Modelo intermedio  | Abstract Document Model | Independencia del render |
| DSL                | Declarativo             | Flexibilidad             |
| Renderizadores     | Plug-in based           | Escalabilidad            |
| Layout engine      | Separado                | Reutilización            |
| Perfil dispositivo | Configurable            | Adaptabilidad            |

---

## 8. Consideraciones de calidad

### Extensibilidad

* Nuevos renderizadores pueden agregarse sin modificar el core
* Nuevos elementos DSL soportables mediante extensión

### Mantenibilidad

* Separación estricta de responsabilidades
* Modelo abstracto desacoplado
* Validaciones centralizadas

### Escalabilidad

* Pipeline stateless
* Posibilidad de paralelización
* Cache de plantillas y datos

### Observabilidad

* Logging por etapa del pipeline
* Trazabilidad por documento
* Identificador de ejecución

---

## 9. Riesgos conocidos

* Complejidad creciente del DSL
* Dificultad en debugging de plantillas complejas
* Diferencias entre renderizadores
* Dependencia de perfiles de dispositivo bien definidos
* Coste de mantenimiento del motor de layout

---

## 10. Evolución prevista

Futuras versiones podrán incluir:

* Compilación previa de plantillas DSL
* Cache de modelo abstracto
* Renderizado incremental
* Editor visual de plantillas
* Sistema de plugins para elementos DSL
* Optimización de layout basada en heurísticas
* Integración con motores de reglas externos

---

## 11. Relación con otros documentos

Esta arquitectura se basa en:

* modelo-abstracto-documento_v1.0.md
* wireframes-documentos_v1.0.md
* CU-01 a CU-32
* RN-02 a RN-06
* RC-01 a RC-06
* prompts de renderizado y clasificación

---

## 12. Historial de versiones

| Versión | Fecha      | Autor          | Cambios                         |
| ------- | ---------- | -------------- | ------------------------------- |
| 1.0     | 2026-03-28 | Equipo Técnico | Versión inicial de arquitectura |

---

