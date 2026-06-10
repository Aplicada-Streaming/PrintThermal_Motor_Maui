# wireframes-documentos_v1.0.md

## 1. Introducción

Este documento describe los wireframes conceptuales de los documentos soportados por el motor de renderizado DSL. Su objetivo es representar visualmente (a nivel lógico) cómo se estructuran los elementos dentro de un documento antes de su renderización final en distintos formatos (ESC/POS, texto plano, PDF, vista previa `raster-preview`).

Los wireframes no representan estilos gráficos finales, sino la organización jerárquica y distribución de componentes.

---

## 2. Objetivo

Definir una representación simplificada de los documentos para:

- Visualizar la estructura jerárquica.
- Facilitar el diseño de plantillas DSL.
- Servir como guía para implementadores del motor.
- Asegurar consistencia entre distintos renderizadores.

---

## 3. Principios de diseño

- **Jerarquía clara**: cada documento se organiza en bloques anidados.
- **Independencia del renderizador**: el wireframe no depende del formato final.
- **Abstracción visual**: representa posiciones relativas, no píxeles exactos.
- **Modularidad**: cada sección del documento es reutilizable.
- **Adaptabilidad**: los bloques deben poder reordenarse según el perfil del dispositivo.

---

## 4. Estructura general de un documento

Un documento típico se compone de:

- Header (encabezado)
- Body (contenido principal)
- Footer (pie de página)

Wireframe lógico:

```

+--------------------------------------+

| HEADER                                   |
| ---------------------------------------- |
|                                          |
| BODY                                     |
|                                          |
| --------------------------------------   |
| FOOTER                                   |
| +--------------------------------------+ |

```

---

## 5. Wireframe de elementos comunes

### 5.1 Texto simple

```

[ Texto ]

```

Representa una línea o bloque de texto.

---

### 5.2 Bloque con alineación

```

| Izquierda        Centro        Derecha |

```

Permite distribución horizontal del contenido.

---

### 5.3 Lista de ítems

```

* Item 1
* Item 2
* Item 3

```

O estructurado:

```

[Item]

* Nombre
* Cantidad
* Precio

```

---

### 5.4 Tabla lógica

```

+------------------------------+
| Col1     | Col2     | Col3   |
+------------------------------+
| Val1     | Val2     | Val3   |
| Val4     | Val5     | Val6   |
+------------------------------+

```

---

### 5.5 Sección condicional

```

[CONDICIÓN]
TRUE  -> Mostrar bloque A
FALSE -> Mostrar bloque B

```

---

### 5.6 Iteración (listas dinámicas)

```

[FOR cada item en Items]

* Item.Nombre
* Item.Cantidad
* Item.Precio
  [END FOR]

```

---

## 6. Wireframe de documento completo (ejemplo)

```

+--------------------------------------------------+

| LOGO / ENCABEZADO                                    |
| ---------------------------------------------------- |
| Cliente: Juan Pérez                                  |
| Fecha: 2026-03-28                                    |
| --------------------------------------------------   |
| PRODUCTOS                                            |
| --------------------------------------------------   |
| Item        Cant   Precio   Total                    |
| --------------------------------------------------   |
| Prod A      2      100      200                      |
| Prod B      1      50       50                       |
| --------------------------------------------------   |
| Subtotal:                           250              |
| Impuestos:                          50               |
| Total:                              300              |
| --------------------------------------------------   |
| PIE DE PÁGINA                                        |
| +--------------------------------------------------+ |

```

---

## 7. Representación jerárquica equivalente

```

Documento
├── Header
│   ├── Logo
│   └── Datos del cliente
├── Body
│   ├── Sección: Productos
│   │   ├── Tabla
│   │   └── Iteración de items
│   └── Sección: Totales
└── Footer
└── Información adicional

```

---

## 8. Relación con el motor DSL

Los wireframes se derivan de:

- Plantillas DSL (estructura declarativa)
- Modelo abstracto del documento
- Datos de entrada

Y se utilizan para:

- Validación visual conceptual
- Depuración de layout
- Diseño de plantillas
- Interpretación del renderizador

---

## 9. Consideraciones para dispositivos

Dependiendo del perfil del dispositivo:

- ESC/POS → Layout lineal, sin posicionamiento libre
- UI (previsualización en pantalla vía `LayoutedDocument`/`raster-preview`, no un render target propio) → Layout flexible con contenedores
- PDF → Layout paginado con control preciso

El wireframe se adapta a restricciones como:

- Ancho de papel
- Tamaño de pantalla
- Márgenes
- Capacidades del dispositivo

---

## 10. Extensibilidad

Los wireframes pueden extenderse para incluir:

- Elementos gráficos (líneas, separadores)
- Imágenes
- Códigos QR o barras
- Componentes personalizados

---

## 11. Conclusión

El wireframe de documentos permite abstraer la estructura visual de un documento antes de su renderización, facilitando la coherencia entre distintos formatos de salida y simplificando el diseño de plantillas DSL.

Sirve como puente conceptual entre:

- Datos
- Plantillas
- Motor de renderizado
- Dispositivo final

