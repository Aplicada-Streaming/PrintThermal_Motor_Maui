# Casos de Prueba Referenciales — Motor DSL de Renderizado  
**Archivo:** casos-prueba-referenciales_v1.0.md  
**Versión:** 1.0  
**Fecha:** 2026-03-28  
**Autor:** Equipo Técnico  
**Estado:** Activo  

---

## 1. Propósito

Definir un conjunto de casos de prueba estándar que sirvan como referencia para:

- Unit tests  
- Integration tests  
- Golden (snapshot) tests  
- Validación manual  
- Ejemplos de uso de la librería  

Estos casos representan escenarios reales y recurrentes del motor DSL, incluyendo parsing, evaluación, layout y renderizado.

---

## 2. Estructura de un caso de prueba

Cada caso incluye:

- Identificador  
- Descripción  
- Entrada DSL  
- Datos de entrada  
- DeviceProfile (si aplica)  
- Output esperado  
- Tipo de prueba sugerido  
- Notas  

> **Nota importante sobre la notación.** El DSL real del motor es **JSON** (parseado con `System.Text.Json`), no la sintaxis de bloques `Document { ... }` / `Text(...)` / `If(...)` / `ForEach(...)`. Las "Entradas DSL" de los casos de abajo son **pseudo-DSL ilustrativo** para describir la intención de cada escenario de forma legible. El equivalente JSON real usa:
> - Envoltura raíz: `{ "id", "version", "root" }` (más `"format": "integrated"` en formato integrado).
> - Nodos con discriminador `"type"`: `text`, `container`, `conditional`, `loop`, `table`, `image`.
> - Texto: clave `"text"` con placeholders `{{nombre}}` (formato clásico) o clave `"value"` ya resuelta (formato integrado).
> - Loop: `{ "type": "loop", "source", "itemAlias", "body" }`.
> - Conditional: `{ "type": "conditional", "expression", "trueBranch", "falseBranch" }`.
>
> No existen tokens `Document`, `Text()`, `If()` ni `ForEach()` en el motor.

---

# 3. Casos de prueba

---

## CP-001 — Documento simple de texto

**Descripción:** Renderizar un documento con un nodo de texto simple.

**Entrada DSL:**
```dsl
Document {
  Text("Hola mundo")
}
````

**Datos:**

```json
{}
```

**Output esperado:**

* Texto: "Hola mundo"
* Sin estructuras adicionales

**Tipo de prueba:**

* Unit
* Snapshot

---

## CP-002 — Texto con binding de datos

**Descripción:** Uso de datos dinámicos en el texto.

**Entrada DSL:**

```dsl
Document {
  Text("Hola {{nombre}}")
}
```

**Datos:**

```json
{
  "nombre": "Juan"
}
```

**Output esperado:**

* "Hola Juan"

**Tipo de prueba:**

* Unit
* Integration
* Snapshot

---

## CP-003 — Condicional verdadero

**Descripción:** Render condicional cuando la expresión es verdadera.

**Entrada DSL:**

```dsl
Document {
  If(data.estado == "activo") {
    Text("Activo")
  }
}
```

**Datos:**

```json
{
  "estado": "activo"
}
```

**Output esperado:**

* "Activo"

**Tipo de prueba:**

* Unit
* Snapshot

---

## CP-004 — Condicional falso

**Descripción:** El bloque no debe renderizarse.

**Datos:**

```json
{
  "estado": "inactivo"
}
```

**Output esperado:**

* Documento vacío

---

## CP-005 — Loop sobre colección

**Descripción:** Iteración sobre una lista de elementos.

**Entrada DSL:**

```dsl
Document {
  ForEach(items) {
    Text(item.nombre)
  }
}
```

**Datos:**

```json
{
  "items": [
    { "nombre": "A" },
    { "nombre": "B" }
  ]
}
```

**Output esperado:**

* A
* B

**Tipo de prueba:**

* Integration
* Snapshot

---

## CP-006 — Documento con estructura jerárquica

**Descripción:** Validar composición de contenedores.

**Entrada DSL:**

```dsl
Document {
  Container {
    Text("Titulo")
    Container {
      Text("Contenido")
    }
  }
}
```

**Output esperado:**

* Jerarquía preservada
* Orden correcto de render

**Tipo de prueba:**

* Unit
* AST validation
* Snapshot

---

## CP-007 — Layout con restricción de ancho

**Descripción:** Ver comportamiento del layout con límites de dispositivo.

**DeviceProfile:**

```json
{
  "width": 20
}
```

**Entrada DSL:**

```dsl
Document {
  Text("Texto muy largo que debe ajustarse")
}
```

**Output esperado:**

* Texto envuelto o truncado según reglas de layout
* Sin overflow

**Tipo de prueba:**

* Layout
* Snapshot

---

## CP-008 — Render ESC/POS básico

**Descripción:** Generación de salida para impresora.

**Entrada DSL:**

```dsl
Document {
  Text("Ticket")
}
```

**Output esperado:**

* Comandos ESC/POS equivalentes al texto

**Tipo de prueba:**

* Renderer
* Snapshot

---

## CP-009 — Documento con múltiples nodos

**Descripción:** Combinación de texto, condiciones y loops.

**Entrada DSL:**

```dsl
Document {
  Text("Inicio")
  If(true) {
    Text("Condicional")
  }
  ForEach(items) {
    Text(item)
  }
}
```

**Datos:**

```json
{
  "items": ["A", "B"]
}
```

**Output esperado:**

* Inicio
* Condicional
* A
* B

---

## CP-010 — Expresión compleja

**Descripción:** Evaluación de expresiones combinadas.

**Entrada DSL:**

```dsl
Document {
  If(data.total > 100 && data.estado == "activo") {
    Text("Aplicar descuento")
  }
}
```

**Datos:**

```json
{
  "total": 150,
  "estado": "activo"
}
```

**Output esperado:**

* "Aplicar descuento"

---

## CP-011 — Datos nulos

**Descripción:** Manejo de valores null o inexistentes.

**Entrada DSL:**

```dsl
Document {
  Text("Nombre: {{nombre}}")
}
```

**Datos:**

```json
{}
```

**Output esperado:**

* "Nombre: " (sin crash)

---

## CP-012 — Nodo no soportado

**Descripción:** Validación de errores ante DSL inválido.

**Entrada DSL:**

```dsl
Document {
  UnknownNode()
}
```

**Output esperado:**

* Error de parsing o validación
* Mensaje descriptivo

---

## CP-013 — Render múltiple por DeviceProfile

**Descripción:** Cambios de salida según dispositivo.

**DeviceProfiles (por `RenderTarget`):**

* `"text"` — texto plano (`string`)
* `"escpos"` — comandos ESC/POS (`byte[]`)
* `"pdf"` — documento PDF (`byte[]`, renderer de MAUI)

**Output esperado:**

* Misma entrada DSL
* Diferente representación según el `IRenderer` seleccionado por el `RenderTarget` del perfil

> Targets de renderer reales: `text` (TextRenderer), `escpos` (EscPosRenderer); en MAUI además `pdf`, `escpos-bitmap` y `raster-preview`. No existe un renderer con target `"UI"`: la previsualización en pantalla se compone con controles MAUI a partir del `LayoutedDocument`, no es una salida de un `IRenderer`.

---

## CP-014 — Documento vacío

**Descripción:** Caso borde sin contenido.

**Entrada DSL:**

```dsl
Document {}
```

**Output esperado:**

* Render vacío válido
* Sin errores

---

## CP-015 — Texto con caracteres especiales

**Descripción:** Manejo de encoding.

**Entrada DSL:**

```dsl
Document {
  Text("áéíóú ñ @ # $")
}
```

**Output esperado:**

* Render correcto sin corrupción de caracteres

---

# 4. Clasificación de casos

| Tipo           | Ejemplos                    |
| -------------- | --------------------------- |
| Unit           | parsing, nodos individuales |
| Integration    | AST + layout + render       |
| Snapshot       | outputs completos           |
| Regression     | CP críticos                 |
| Layout         | CP-007                      |
| Renderer       | CP-008                      |
| Extensibilidad | futuros nodos/renderers     |

---

# 5. Uso de estos casos

Estos casos pueden utilizarse como:

* Base para unit tests automatizados
* Fixtures para tests de integración
* Golden tests (snapshots)
* Ejemplos en documentación
* Validación manual de cambios

---

# 6. Mapeo con componentes del motor

| Componente      | Casos relacionados |
| --------------- | ------------------ |
| Parser DSL      | CP-001, CP-012     |
| AST             | CP-006             |
| Expresiones     | CP-010             |
| Layout Engine   | CP-007             |
| Renderer        | CP-008, CP-013     |
| Engine completo | CP-009             |

---

# 7. Control de versiones

| Versión | Fecha      | Autor          | Descripción                   |
| ------- | ---------- | -------------- | ----------------------------- |
| 1.0     | 2026-03-28 | Equipo Técnico | Casos iniciales de referencia |

---
