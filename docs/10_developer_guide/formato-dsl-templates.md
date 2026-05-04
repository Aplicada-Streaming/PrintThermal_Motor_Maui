# Formato DSL — Especificación de Templates JSON

## 1. Estructura raíz del documento

Todo template DSL es un objeto JSON con la siguiente estructura:

### Propiedades

| Propiedad | Tipo | Obligatoria | Descripción |
|-----------|------|-------------|-------------|
| `id` | string | Sí | Identificador único del template. |
| `version` | string | Sí | Versión del template (semántica libre). |
| `root` | object | Sí | Nodo raíz del documento (generalmente un `container`). |
| `metadata` | object | No | Información adicional libre (autor, fecha, descripción). |

### Ejemplo mínimo válido

```json
{
  "id": "mi-template",
  "version": "1.0",
  "root": {
    "type": "text",
    "text": "Hola mundo"
  }
}
```

### Ejemplo con metadata

```json
{
  "id": "ticket-venta-001",
  "version": "2.1",
  "metadata": {
    "autor": "equipo-ventas",
    "descripcion": "Ticket de venta estándar",
    "fecha": "2026-03-01"
  },
  "root": {
    "type": "container",
    "layout": "vertical",
    "children": [
      { "type": "text", "text": "{{storeName}}", "style": { "align": "center", "bold": true } },
      { "type": "text", "text": "Gracias por su compra" }
    ]
  }
}
```

---

## 2. Tipos de nodos — Resumen

| Tipo | Descripción | Propiedades obligatorias | Propiedades opcionales |
|------|-------------|--------------------------|------------------------|
| `text` | Texto estático o con binding. Nodo hoja. | `type` | `text`, `bindPath`, `style` |
| `container` | Agrupa nodos hijos con un layout. | `type` | `layout`, `children`, `style` |
| `loop` | Itera sobre una colección y repite su body. | `type`, `source`, `itemAlias` | `body` |
| `conditional` | Incluye contenido si una expresión es verdadera. | `type`, `expression` | `trueBranch`, `falseBranch` |
| `table` | Tabla con encabezados y filas. | `type` | `headers`, `rows`, `style` |
| `image` | Imagen, código QR o código de barras. | `type`, `source` | `width`, `height`, `imageType`, `style` |

> `type` es obligatorio en **todos** los nodos. Es el discriminador que el parser usa para instanciar la clase correcta.

---

## 3. Especificación detallada de cada nodo

### 3.1 text

Nodo hoja que contiene texto estático o texto con bindings.

| Propiedad | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `type` | `"text"` | — | Discriminador de tipo. |
| `text` | string | `""` | Contenido de texto. Puede contener expresiones `{{binding}}`. |
| `bindPath` | string | `null` | Ruta directa a un dato. Si está presente, el texto se resuelve desde los datos. |
| `style` | object | `null` | Estilos: `align`, `bold`. |

**Ejemplo — texto estático:**

```json
{
  "type": "text",
  "text": "================================"
}
```

**Ejemplo — texto con binding:**

```json
{
  "type": "text",
  "text": "Cliente: {{cliente.nombre}}",
  "style": { "align": "left" }
}
```

**Ejemplo — texto con estilo:**

```json
{
  "type": "text",
  "text": "TOTAL: ${{total}}",
  "style": { "align": "right", "bold": true }
}
```

**Ejemplo — línea vacía (espaciador):**

```json
{
  "type": "text",
  "text": ""
}
```

---

### 3.2 container

Agrupa nodos hijos. Es el bloque estructural principal.

| Propiedad | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `type` | `"container"` | — | Discriminador de tipo. |
| `layout` | string | `"vertical"` | Dirección de distribución: `"vertical"` o `"horizontal"`. |
| `children` | array | `[]` | Lista de nodos hijos. |
| `style` | object | `null` | Estilos aplicados al contenedor. |

**Ejemplo:**

```json
{
  "type": "container",
  "layout": "vertical",
  "children": [
    { "type": "text", "text": "Línea 1" },
    { "type": "text", "text": "Línea 2" },
    { "type": "text", "text": "Línea 3" }
  ]
}
```

**Ejemplo — container horizontal (columnas):**

```json
{
  "type": "container",
  "layout": "horizontal",
  "children": [
    { "type": "text", "text": "Izquierda" },
    { "type": "text", "text": "Derecha" }
  ]
}
```

> El `root` del documento es casi siempre un `container` con `layout: "vertical"`.

---

### 3.3 loop

Itera sobre una colección de datos y repite el `body` para cada elemento.

| Propiedad | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `type` | `"loop"` | — | Discriminador de tipo. |
| `source` | string | — | Ruta al array en los datos (ej. `"items"`, `"pedido.lineas"`). |
| `itemAlias` | string | — | Nombre de variable para cada elemento dentro del body. |
| `body` | object | `null` | Nodo a repetir por cada item. Generalmente un `container`. |

**Ejemplo:**

```json
{
  "type": "loop",
  "source": "items",
  "itemAlias": "item",
  "body": {
    "type": "container",
    "layout": "vertical",
    "children": [
      { "type": "text", "text": "{{item.nombre}}" },
      { "type": "text", "text": "  {{item.cantidad}} x ${{item.precio}}    ${{item.total}}" }
    ]
  }
}
```

**Con estos datos:**

```json
{
  "items": [
    { "nombre": "Café", "cantidad": "2", "precio": "150.00", "total": "300.00" },
    { "nombre": "Medialunas", "cantidad": "6", "precio": "50.00", "total": "300.00" }
  ]
}
```

**Genera:**

```
Café
  2 x $150.00    $300.00
Medialunas
  6 x $50.00    $300.00
```

---

### 3.4 conditional

Incluye contenido condicionalmente según una expresión evaluada en runtime.

| Propiedad | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `type` | `"conditional"` | — | Discriminador de tipo. |
| `expression` | string | — | Expresión booleana a evaluar contra los datos. |
| `trueBranch` | object | `null` | Nodo a renderizar si la expresión es verdadera. |
| `falseBranch` | object | `null` | Nodo a renderizar si la expresión es falsa (opcional). |

**Ejemplo — con ambas ramas:**

```json
{
  "type": "conditional",
  "expression": "cliente.esVip == true",
  "trueBranch": {
    "type": "text",
    "text": "★ CLIENTE VIP — 10% de descuento",
    "style": { "bold": true }
  },
  "falseBranch": {
    "type": "text",
    "text": "Gracias por su compra"
  }
}
```

**Ejemplo — solo trueBranch (sin falseBranch):**

```json
{
  "type": "conditional",
  "expression": "observaciones != ''",
  "trueBranch": {
    "type": "text",
    "text": "Obs: {{observaciones}}"
  }
}
```

> Si `falseBranch` se omite y la expresión es falsa, no se renderiza nada.

---

### 3.5 table

Tabla con encabezados y filas de datos.

| Propiedad | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `type` | `"table"` | — | Discriminador de tipo. |
| `headers` | string[] | `[]` | Nombres de columnas. |
| `rows` | string[][] | `[]` | Filas de datos. Cada fila es un array de celdas. |
| `style` | object | `null` | Estilos del encabezado (ej. bold). |

**Ejemplo:**

```json
{
  "type": "table",
  "headers": ["Producto", "Cant", "Precio", "Subtotal"],
  "rows": [
    ["Café", "2", "$150.00", "$300.00"],
    ["Medialunas", "6", "$50.00", "$300.00"],
    ["Agua", "1", "$80.00", "$80.00"]
  ],
  "style": { "bold": true }
}
```

**Render en texto (32 chars):**

```
Producto  Cant  Precio  Subtotal
--------------------------------
Café         2  $150.00  $300.00
Medialunas   6   $50.00  $300.00
Agua         1   $80.00   $80.00
```

> El ancho de cada columna se calcula proporcionalmente según el `DeviceProfile.Width`.

---

### 3.6 image

Imagen, código QR o código de barras.

| Propiedad | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `type` | `"image"` | — | Discriminador de tipo. |
| `source` | string | — | Contenido: URL, path, base64, o texto para QR/barcode. |
| `width` | int | `null` | Ancho en píxeles o unidades del dispositivo. |
| `height` | int | `null` | Alto en píxeles o unidades del dispositivo. |
| `imageType` | string | `null` | Tipo: `"png"`, `"jpeg"`, `"qrcode"`, `"barcode"`, `"ean13"`. |
| `style` | object | `null` | Estilos (ej. `align: "center"`). |

**Ejemplo — código QR:**

```json
{
  "type": "image",
  "source": "https://pagos.ejemplo.com/verificar?id={{transaccionId}}",
  "imageType": "qrcode",
  "width": 200,
  "style": { "align": "center" }
}
```

**Ejemplo — código de barras EAN13:**

```json
{
  "type": "image",
  "source": "{{codigoBarras}}",
  "imageType": "ean13",
  "style": { "align": "center" }
}
```

**Ejemplo — logo como imagen:**

```json
{
  "type": "image",
  "source": "logo_empresa.png",
  "imageType": "png",
  "width": 200,
  "height": 50,
  "style": { "align": "center" }
}
```

> En ESC/POS, los QR se generan con comandos nativos de la impresora (`GS ( k`). Las imágenes bitmap requieren conversión a formato compatible.

---

## 4. Sistema de binding

Los bindings permiten insertar datos dinámicos en el texto de los nodos. Se escriben entre dobles llaves: `{{ruta}}`.

### Sintaxis

| Patrón | Descripción | Ejemplo |
|--------|-------------|---------|
| `{{variable}}` | Acceso directo a un campo del diccionario raíz. | `{{storeName}}` → `"Mi Tienda"` |
| `{{objeto.propiedad}}` | Acceso a propiedad anidada con punto. | `{{cliente.nombre}}` → `"Juan Pérez"` |
| `{{items[0].campo}}` | Acceso por índice a un elemento de array. | `{{items[0].precio}}` → `"150.00"` |
| `{{alias.campo}}` | Dentro de un loop, acceso al item actual por su alias. | `{{item.nombre}}` → `"Café"` |

### Ejemplos de datos y bindings

**Datos:**

```json
{
  "storeName": "Mi Tienda",
  "fecha": "29/03/2026 14:30",
  "cliente": {
    "nombre": "Juan Pérez",
    "dni": "30123456"
  },
  "items": [
    { "nombre": "Café", "cantidad": "2", "precio": "150.00", "total": "300.00" },
    { "nombre": "Medialunas", "cantidad": "6", "precio": "50.00", "total": "300.00" }
  ],
  "total": "600.00"
}
```

**Bindings →  resultado:**

| Expresión en template | Resultado |
|-----------------------|-----------|
| `"{{storeName}}"` | `"Mi Tienda"` |
| `"Fecha: {{fecha}}"` | `"Fecha: 29/03/2026 14:30"` |
| `"DNI: {{cliente.dni}}"` | `"DNI: 30123456"` |
| `"{{items[0].nombre}}"` | `"Café"` |
| `"TOTAL: ${{total}}"` | `"TOTAL: $600.00"` |

### Bindings en loops

Dentro de un `loop`, el `itemAlias` crea una variable temporal que apunta al elemento actual:

```json
{
  "type": "loop",
  "source": "items",
  "itemAlias": "item",
  "body": {
    "type": "text",
    "text": "{{item.nombre}} — ${{item.total}}"
  }
}
```

Genera una línea por cada item:

```
Café — $300.00
Medialunas — $300.00
```

### Múltiples bindings en un mismo texto

Se pueden combinar varios bindings en un solo campo `text`:

```json
{ "type": "text", "text": "{{item.cantidad}} x ${{item.precio}}    ${{item.total}}" }
```

---

## 5. Sistema de estilos

Los estilos se definen en la propiedad `style` de cada nodo.

### Propiedades de estilo

| Propiedad | Tipo | Valores | Default | Descripción |
|-----------|------|---------|---------|-------------|
| `align` | string | `"left"`, `"center"`, `"right"` | `"left"` | Alineación horizontal del texto dentro del ancho disponible. |
| `bold` | bool | `true`, `false` | `false` | Texto en negrita. En ESC/POS usa `ESC E 1`. En texto plano no tiene efecto visible. |

### Cómo se aplican en el layout

- **`align: "left"`** — El texto queda a la izquierda, se rellena con espacios a la derecha.
- **`align: "center"`** — Se calcula el padding izquierdo: `(anchoDisponible - largoTexto) / 2`.
- **`align: "right"`** — Se rellena con espacios a la izquierda hasta completar el ancho.
- **`bold: true`** — En ESC/POS, se emite `ESC E 1` antes del texto y `ESC E 0` después. En texto plano no genera diferencia visible.

### Ejemplo

```json
{
  "type": "text",
  "text": "MI NEGOCIO",
  "style": { "align": "center", "bold": true }
}
```

En un perfil de 32 caracteres de ancho:

```
           MI NEGOCIO           
```

(11 espacios + "MI NEGOCIO" + 11 espacios)

---

## 6. Separadores y líneas

No existe un nodo `separator` dedicado. Los separadores se construyen con un nodo `text` que contiene guiones o caracteres repetidos.

### Línea completa (32 chars)

```json
{ "type": "text", "text": "--------------------------------" }
```

### Línea doble

```json
{ "type": "text", "text": "================================" }
```

### Línea de puntos

```json
{ "type": "text", "text": "................................" }
```

### Línea vacía (espaciador)

```json
{ "type": "text", "text": "" }
```

> **Consejo:** Ajustá la cantidad de caracteres al ancho del perfil. Para 58mm (32 chars) usá 32 guiones; para 80mm (48 chars) usá 48.

---

## 7. Operadores en condicionales

La propiedad `expression` del nodo `conditional` soporta los siguientes operadores:

### Operadores de comparación

| Operador | Descripción | Ejemplo |
|----------|-------------|---------|
| `==` | Igual a | `"estado == 'activo'"` |
| `!=` | Distinto de | `"descuento != 0"` |
| `>` | Mayor que | `"total > 1000"` |
| `<` | Menor que | `"cantidad < 5"` |
| `>=` | Mayor o igual | `"items.length >= 1"` |
| `<=` | Menor o igual | `"edad <= 65"` |

### Operadores lógicos

| Operador | Descripción | Ejemplo |
|----------|-------------|---------|
| `&&` | AND lógico | `"esVip == true && total > 500"` |
| `\|\|` | OR lógico | `"descuento > 0 \|\| esPromocion == true"` |

### Ejemplos de expresiones válidas

```json
"expression": "cliente.esVip == true"
```

```json
"expression": "total > 1000"
```

```json
"expression": "observaciones != ''"
```

```json
"expression": "items.length > 0 && total > 0"
```

```json
"expression": "formaPago == 'tarjeta' || formaPago == 'transferencia'"
```

---

## 8. Reglas de validación

### Campos obligatorios por tipo

| Tipo | Campos obligatorios |
|------|---------------------|
| Documento raíz | `id`, `version`, `root` |
| Todos los nodos | `type` |
| `loop` | `source`, `itemAlias` |
| `conditional` | `expression` |
| `image` | `source` |

### Binding no resuelto

Si un binding referencia un dato que no existe en el diccionario, el evaluador lo reemplaza por la cadena literal `UNRESOLVED:{{ruta}}`.

| Binding | Dato presente | Resultado |
|---------|---------------|-----------|
| `{{storeName}}` | Sí | `"Mi Tienda"` |
| `{{storeName}}` | No | `"UNRESOLVED:{{storeName}}"` |

Esto permite detectar datos faltantes en el output sin que el motor falle.

### Tipo de `root` válido

El nodo `root` puede ser cualquier tipo de nodo válido, pero por convención es un `container` con `layout: "vertical"`.

### Nodos vacíos

- Un `container` sin `children` es válido (no genera output).
- Un `loop` sin `body` es válido (no genera output).
- Un `conditional` sin `trueBranch` ni `falseBranch` es válido (no genera output).
- Un `text` sin `text` ni `bindPath` genera una línea vacía.

### Errores comunes

| Error | Causa | Solución |
|-------|-------|----------|
| Nodo no reconocido | `type` con valor desconocido | Usar: `text`, `container`, `loop`, `conditional`, `table`, `image` |
| Loop sin datos | `source` apunta a un campo inexistente | Verificar que los datos contengan el array referenciado |
| Binding vacío | `{{}}` sin contenido | Escribir la ruta completa: `{{campo}}` |
| JSON inválido | Error de sintaxis en el template | Validar con un linter JSON antes de cargar |

---

## 9. Formato Integrado

El motor admite una segunda modalidad de entrada llamada **formato integrado**: el JSON ya viene con todos los datos resueltos, sin `{{placeholders}}`, sin `loop` y sin `conditional`. Es útil cuando el documento ya se construyó completo en la app consumidora (por ejemplo, después de una transformación server-side) y solo falta layout + render.

### 9.1 Discriminador y firma del motor

El campo `format` en la raíz del JSON discrimina entre las dos modalidades:

| Valor | Pipeline interno | Llamada al motor |
|---|---|---|
| `"template"` (default) o ausente | Parse → Validate → **Evaluate** → Layout → Render | `engine.Render(json, data, profile)` |
| `"integrated"` | Parse → Validate → Layout → Render (sin Evaluate) | `engine.Render(json, profile)` |

Ambas modalidades coexisten sin afectarse: omitir el campo `format` mantiene el comportamiento histórico.

### 9.2 Diferencias respecto al formato clásico

| Aspecto | Clásico (`template`) | Integrado (`integrated`) |
|---|---|---|
| Campo `format` raíz | ausente o `"template"` | `"integrated"` |
| Texto en `text` | `{ "type":"text", "text":"Hola {{nombre}}" }` | `{ "type":"text", "value":"Hola Juan" }` |
| Nodos `loop` | permitidos | **prohibidos** (ya expandidos como N `container`) |
| Nodos `conditional` | permitidos | **prohibidos** (ya resueltos a la rama válida) |
| `image.source` | `"https://api/qr/{{id}}"` | `"https://api/qr/2026-00123"` |
| Diccionario de datos | requerido | no se usa |

### 9.3 Validaciones específicas del modo integrado

`TemplateValidator` aplica reglas adicionales cuando detecta `"format": "integrated"`:

| Regla | Tipo de error | Severidad |
|---|---|---|
| Aparece un nodo `loop` o `conditional` | `UnsupportedInIntegratedFormat` | Error |
| El `value` de un `text` contiene `{{...}}` | `UnresolvedPlaceholder` | Error |
| El `source` de un `image` contiene `{{...}}` | `UnresolvedPlaceholder` | Error |
| `format` tiene un valor distinto a `template` o `integrated` | `InvalidSyntax` | Error |

### 9.4 Ejemplo de documento integrado

```json
{
  "id": "acta-infraccion-001",
  "version": "1.0",
  "format": "integrated",
  "root": {
    "type": "container",
    "layout": "vertical",
    "children": [
      {
        "type": "image",
        "source": "data:image/bmp;base64,Qk0+KQAAA...",
        "imageType": "bitmap",
        "width": 200,
        "style": { "align": "center" }
      },
      {
        "type": "text",
        "value": "MUNICIPALIDAD DE EJEMPLO",
        "style": { "align": "center", "bold": true }
      },
      { "type": "text", "value": "ACTA DE INFRACCIÓN N°: 2026-00123" },
      { "type": "text", "value": "Fecha: 31/03/2026  Hora: 14:35" },
      {
        "type": "container",
        "layout": "vertical",
        "children": [
          { "type": "text", "value": "Art. 77 inc. 2 - Exceso de velocidad" },
          { "type": "text", "value": "Puntos: 3  Monto: $15000" }
        ]
      },
      {
        "type": "image",
        "source": "https://multas.ejemplo.gob.ar/pago/2026-00123",
        "imageType": "qrcode"
      }
    ]
  }
}
```

### 9.5 Uso en C#

```csharp
// Modo clásico — template + datos por separado
var classicResult = engine.Render(jsonTemplate, data, profile);

// Modo integrado — un solo string con todo resuelto
var integratedResult = engine.Render(jsonIntegrated, profile);
```

Ambos producen un `RenderResult` equivalente (mismo `Output`, mismo `Target`, mismas `Warnings`/`Errors`). Los renderers (`EscPosRenderer`, `TextRenderer`, custom) no distinguen el origen del documento.

### 9.6 Cuándo conviene cada modalidad

| Escenario | Modalidad recomendada |
|---|---|
| Documento parametrizable con datos del usuario | Clásico |
| Documento ya armado por un backend o un job batch | Integrado |
| Templates reutilizables con bindings dinámicos | Clásico |
| Snapshots inmutables (auditoría, reimpresión idéntica) | Integrado |
| Lógica condicional o iteraciones declarativas | Clásico |
| Producer-consumer con AST serializado entre servicios | Integrado |
