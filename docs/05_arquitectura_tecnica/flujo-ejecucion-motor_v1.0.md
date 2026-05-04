# Flujo de Ejecución del Motor — Librería DSL de Renderizado  
**Archivo:** flujo-ejecucion-motor_v1.0.md  
**Versión:** 1.0  
**Fecha:** 2026-03-28  
**Autor:** Equipo Técnico  
**Estado:** Borrador  

---

## 1. Propósito

Este documento describe el flujo de ejecución interno del Motor DSL de renderizado. Define paso a paso cómo una plantilla DSL, junto con datos de entrada y un perfil de dispositivo, se transforma en un resultado renderizado.

El objetivo es proporcionar una visión clara del pipeline interno del motor para facilitar:

- Comprensión del comportamiento del sistema  
- Depuración de errores  
- Implementación de extensiones  
- Optimización del rendimiento  
- Trazabilidad de la ejecución  

---

## 2. Visión general del flujo

```text id="flujo-general-001"
DSL / Template + Datos + DeviceProfile
                ↓
            IDocumentEngine
                ↓
            Parsing DSL
                ↓
        DocumentTemplate (AST)
                ↓
         Data Resolution
                ↓
        Expression Evaluation
                ↓
            Layout Engine
                ↓
            Renderer Selection
                ↓
            Render Process
                ↓
             RenderResult
````

---

## 3. Entrada del sistema

El motor recibe tres entradas principales:

* **Template DSL**: definición del documento
* **Data Model**: objeto con los datos dinámicos
* **DeviceProfile**: configuración del dispositivo destino

Estas entradas son inmutables durante el proceso de renderizado.

---

## 4. Etapa 1 — Parsing del DSL

### Descripción

El DSL es interpretado y convertido en un modelo estructurado (`DocumentTemplate`).

### Responsabilidades

* Validar sintaxis DSL
* Construir el árbol de nodos (AST)
* Resolver estructuras básicas del documento

### Resultado

```text
string DSL → DocumentTemplate (AST)
```

---

## 5. Etapa 2 — Construcción del modelo abstracto

El resultado del parsing es un árbol jerárquico de nodos (`DocumentNode`).

Este árbol representa:

* Estructura del documento
* Contenido
* Lógica condicional
* Iteraciones
* Componentes visuales

No contiene aún información específica del render final.

---

## 6. Etapa 3 — Resolución de datos

### Descripción

Se vinculan los datos externos con los nodos del documento.

### Responsabilidades

* Evaluar `BindPath` en nodos como `TextNode`
* Resolver colecciones para `LoopNode`
* Inyectar valores dinámicos en el árbol

### Ejemplo

```text id="data-binding-001"
User.Name → "Juan Pérez"
```

---

## 7. Etapa 4 — Evaluación de expresiones

### Descripción

Se evalúan expresiones DSL utilizadas en:

* ConditionalNode
* LoopNode
* Bindings dinámicos

### Responsabilidades

* Interpretar expresiones lógicas
* Acceder al contexto de datos
* Resolver condiciones booleanas
* Determinar ramas de ejecución

### Resultado

El árbol puede modificarse dinámicamente según las condiciones evaluadas.

---

## 8. Etapa 5 — Aplicación de layout

### Descripción

El Layout Engine organiza los nodos según el perfil del dispositivo.

### Responsabilidades

* Ajustar distribución visual
* Aplicar reglas de layout (vertical, horizontal, grid)
* Adaptar contenido al ancho disponible
* Reorganizar nodos si es necesario

### Dependencias

* DeviceProfile
* Tipo de renderer
* Capacidades del dispositivo

---

## 9. Etapa 6 — Selección del renderer

### Descripción

El motor selecciona el renderer adecuado según el target definido en el DeviceProfile.

### Mecanismo

* Matching por `Target` en `IRenderer`
* Resolución desde contenedor DI
* Fallback opcional

### Ejemplo

```text id="renderer-selection-001"
DeviceProfile.RenderTarget = "escpos"
→ EscPosRenderer
```

---

## 10. Etapa 7 — Renderizado

### Descripción

El renderer transforma el DocumentNode en una salida concreta.

### Responsabilidades

* Interpretar el AST
* Generar output específico (texto, bytes, UI, etc.)
* Aplicar formateo final
* Manejar nodos recursivamente

### Resultado

Depende del renderer:

* String (HTML / texto)
* Bytes (impresión / PDF)
* Objetos UI
* Representaciones intermedias

---

## 11. Etapa 8 — Construcción del resultado

Se encapsula la salida en un objeto `RenderResult`.

Incluye:

* Output final
* Target
* Advertencias
* Errores

---

## 12. Manejo de errores

Durante el flujo pueden ocurrir errores en distintas etapas:

| Etapa               | Tipo de error                   |
| ------------------- | ------------------------------- |
| Parsing             | Errores de sintaxis DSL         |
| Resolución de datos | Paths inválidos                 |
| Evaluación          | Expresiones inválidas           |
| Layout              | Incompatibilidad de dispositivo |
| Render              | Fallos específicos del renderer |

Todos los errores deben propagarse dentro de `RenderResult`.

---

## 13. Logging y trazabilidad

El motor puede incluir:

* Logs por etapa
* Identificadores de ejecución
* Trazas de evaluación
* Diagnóstico de nodos

Esto permite debugging avanzado del pipeline.

---

## 14. Flujo detallado paso a paso

```text id="flujo-detallado-001"
1. Cliente invoca IDocumentEngine.Render()
2. Se recibe DSL + data + DeviceProfile
3. IDslParser.Parse() convierte DSL → DocumentTemplate
4. Se obtiene el AST (DocumentNode root)
5. IDataResolver enlaza datos al árbol
6. Expresiones DSL son evaluadas
7. ILayoutEngine ajusta estructura según DeviceProfile
8. Se selecciona IRenderer según Target
9. IRenderer.Render() procesa el AST
10. Se genera RenderResult
11. Se retornan output + warnings + errors
```

---

## 15. Consideraciones de rendimiento

* Parsing puede cachearse si el DSL no cambia
* Evaluación de expresiones debe ser optimizada
* Layout puede precomputarse en escenarios repetitivos
* Renderers deben evitar recalcular nodos innecesariamente

---

## 16. Consideraciones de concurrencia

El motor debe soportar:

* Ejecución concurrente
* Instancias independientes por render
* Inmutabilidad del AST durante render

Recomendaciones:

* Evitar estado compartido mutable
* Usar objetos thread-safe
* No reutilizar contextos entre ejecuciones

---

## 17. Puntos de extensión en el flujo

Durante el pipeline se pueden inyectar extensiones en:

* Parsing (custom DSL features)
* Resolución de datos (IDataResolver)
* Evaluación de expresiones
* Layout (ILayoutEngine)
* Renderizado (IRenderer)

---

## 18. Relación con otros documentos

Este flujo se alinea con:

* arquitectura-solucion_v1.0.md
* contratos-del-motor_v1.0.md
* modelo-datos-logico_v1.0.md
* extensibilidad-motor_v1.0.md
* guia-uso-libreria_v1.0.md

---

## 19. Riesgos conocidos

* DSL complejo puede hacer el parsing costoso
* Evaluaciones dinámicas pueden impactar performance
* Layout adaptable puede introducir variabilidad difícil de predecir
* Dependencia de múltiples renderers puede complicar debugging
* Errores en expresiones pueden propagarse silenciosamente

---

## 20. Evolución prevista

Posibles mejoras futuras:

* Pipeline configurable por el usuario
* Middleware/interceptors en cada etapa
* Caching de AST y resultados intermedios
* Ejecución paralela de subárboles
* Instrumentación avanzada del pipeline
* Perfilado de performance por etapa

---

## 21. Flujo alternativo — Documento Integrado

### Descripción

Cuando el JSON de entrada declara `"format": "integrated"`, el motor expone un overload simplificado:

```csharp
RenderResult Render(string integratedJson, DeviceProfile profile);
```

El AST llega ya resuelto: sin placeholders `{{...}}`, sin nodos `loop`, sin nodos `conditional`. La etapa de Evaluate es innecesaria y se omite.

### Pipeline simplificado

```text
Documento Integrado JSON + DeviceProfile
                ↓
            IDocumentEngine
                ↓
       TemplateValidator
       (modo "integrated":
        prohíbe loops/conditionals,
        rechaza placeholders residuales)
                ↓
            Parser DSL
       (mapea "value" → TextNode.Text)
                ↓
       DocumentTemplate (AST resuelto)
                ↓
        ProfileValidator
                ↓
   (Evaluator NO se invoca — el AST ya está resuelto)
                ↓
            Layout Engine
                ↓
            Renderer Selection
                ↓
            Render Process
                ↓
             RenderResult
```

### Diferencias clave con el flujo clásico

| Etapa | Flujo clásico | Flujo integrado |
|---|---|---|
| Validación template | reglas estándar | reglas estándar **+** prohíbe loops/conditionals **+** detecta placeholders residuales |
| Parser | mapea `text` a `TextNode.Text` | mapea `value` a `TextNode.Text` |
| DataValidator | aplica si está registrado | no se invoca (no hay datos) |
| Evaluator | resuelve bindings, expande loops, evalúa conditionals | **se omite por completo** |
| Layout / Render | sin cambios | sin cambios |

### Garantía de equivalencia

El `LayoutedDocument` que produce el flujo integrado es **estructuralmente idéntico** al que produce el flujo clásico cuando los datos resuelven a los mismos valores. Esta invariante está cubierta por el test `Pipeline_IntegratedVsClassic_ProduceIdenticalOutput`.

### Cuándo se usa

Apropiado cuando el productor del JSON ya conoce todos los valores y quiere evitar el costo de transportar template + diccionario por separado: snapshots de auditoría, reimpresiones, integraciones server-to-device donde el documento se arma server-side.

---

## 22. Historial de versiones

| Versión | Fecha      | Autor          | Cambios                                                |
| ------- | ---------- | -------------- | ------------------------------------------------------ |
| 1.0     | 2026-03-28 | Equipo Técnico | Flujo inicial del motor                                |
| 1.1     | 2026-05-04 | Equipo Técnico | Agregado flujo alternativo para documento integrado    |

---
