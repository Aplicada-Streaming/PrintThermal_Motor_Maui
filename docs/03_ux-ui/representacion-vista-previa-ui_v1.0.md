# Representación de Documento en Vista Previa UI

**Proyecto:** Motor DSL de Documentos para Impresión
**Documento:** representacion-vista-previa-ui_v1.0.md
**Versión:** 1.0
**Estado:** Aprobado
**Fecha:** 2026-03-28
**Autor:** Equipo de Arquitectura / Renderización

---

# 1. Introducción

Este documento describe la representación de documentos en vista previa (UI) dentro del motor DSL. Su objetivo es definir cómo un documento abstracto es visualizado en una interfaz gráfica, permitiendo a los usuarios validar la salida antes de su impresión o exportación.

La vista previa UI constituye una representación no física del documento, orientada a simulación visual en pantalla, respetando en la medida de lo posible las restricciones del dispositivo destino.

> Nota técnica: la vista previa no utiliza un "Renderizador UI" (no existe ningún `IRenderer` con `Target == "ui"`). En la práctica se construye con el control MAUI `MauiDocumentPreview` a partir del `LayoutedDocument` que devuelve `IDocumentEngine.RenderLayout(template, data, profile)`, o con `MauiRasterPreview` mostrando el PNG del target real `raster-preview`. Los términos "vista UI" / "Renderizador UI" de este documento son conceptuales.

---

# 2. Alcance

Este documento cubre:

* Representación visual del documento en UI
* Simulación de layout similar al dispositivo final
* Renderizado de elementos estructurados
* Adaptación visual según perfil de impresora
* Interacción básica de visualización

No incluye:

* Renderizado a impresoras físicas
* Generación de comandos ESC/POS
* Persistencia de datos
* Lógica de negocio del documento

---

# 3. Concepto de Vista Previa UI

La vista previa UI es una representación gráfica del documento que permite:

* Visualizar el layout final
* Validar distribución de elementos
* Detectar errores de formato
* Simular condiciones reales de impresión

A diferencia de ESC/POS, esta representación está orientada a interfaces gráficas (pantallas), no a dispositivos físicos.

---

# 4. Estructura General de la Vista

La vista previa del documento se organiza en:

1. Contenedor principal (viewport)
2. Área de documento
3. Secciones estructuradas
4. Elementos individuales
5. Indicadores visuales (opcional)

---

# 5. Representación de Elementos

## 5.1 Texto

El texto se representa respetando:

* Tipografía estándar del sistema
* Tamaños relativos
* Estilos visuales (negrita, cursiva)
* Alineación

**Ejemplo visual:**

```text id="ui_text_1"
Total: 100.00
```

Se muestra con estilos aplicados según el modelo abstracto.

---

## 5.2 Alineación

La alineación se refleja visualmente en la posición del texto dentro del contenedor:

* Izquierda → alineado al margen izquierdo
* Centro → centrado horizontalmente
* Derecha → alineado al margen derecho

---

## 5.3 Bloques de Contenido

Los bloques representan secciones del documento:

* Encabezado
* Detalle
* Totales
* Pie

Cada bloque se renderiza como una unidad visual separada.

---

## 5.4 Separadores

Se visualizan como líneas horizontales o espacios entre secciones.

**Ejemplo:**

```text id="ui_sep_1"
--------------------------
```

En UI pueden representarse como líneas gráficas.

---

## 5.5 Tablas Simuladas

Las tablas se representan utilizando layouts de columnas:

* Grid o flex layout
* Columnas alineadas
* Espaciado proporcional

**Ejemplo visual:**

```text id="ui_table_1"
Producto      Cantidad     Precio
----------------------------------
Café          2            5.00
```

En UI se renderiza como una tabla estructurada.

---

## 5.6 Imágenes

Se representan como elementos visuales:

* Logos
* QR codes
* Códigos de barras

**Consideraciones:**

* Escalado proporcional
* Ajuste al ancho del contenedor
* Fallback si no hay soporte

---

## 5.7 Códigos QR

Se visualizan como imágenes generadas dinámicamente.

**Uso típico:**

* URLs
* Identificadores de transacción

---

## 5.8 Espaciado y Layout

El layout en UI simula el comportamiento del dispositivo final:

* Márgenes
* Padding
* Espaciado entre elementos
* Saltos de línea

---

# 6. Flujo de Renderizado a Vista Previa

```text id="flowui1"
Documento Abstracto
        ↓
Modelo Interno
        ↓
Motor de Layout (RenderLayout)
        ↓
LayoutedDocument  /  target raster-preview (PNG)
        ↓
Control UI (MauiDocumentPreview / MauiRasterPreview)
        ↓
Componentes Visuales
        ↓
Pantalla (Vista Previa)
```

---

# 7. Consideraciones de Adaptación Visual

La vista previa debe adaptarse según:

* Perfil de impresora (ancho en caracteres o mm)
* Resolución de pantalla
* Escala visual
* Tipo de dispositivo (desktop / mobile)

Esto permite aproximar la representación real del output final.

---

# 8. Simulación de Dispositivo

La UI puede incluir simulaciones como:

* Ancho equivalente a impresora de 58mm o 80mm
* Visualización de límites de papel
* Líneas guía o márgenes
* Indicadores de corte

---

# 9. Interacción en la Vista Previa

Opcionalmente, la vista previa puede incluir:

* Zoom
* Scroll vertical
* Navegación por páginas
* Inspección de elementos (debug visual)

---

# 10. Limitaciones de la Vista Previa

* No garantiza fidelidad absoluta con el dispositivo físico
* Puede existir diferencia en fuentes y rendering
* No reproduce exactamente ESC/POS
* Depende del entorno gráfico

---

# 11. Estrategias de Representación

El motor puede aplicar:

* Mapeo de bloques a componentes UI
* Conversión de layout abstracto a layout visual
* Simulación de restricciones de dispositivo
* Adaptación responsiva

---

# 12. Relación con Otros Componentes

Esta representación depende de:

* CU-05: Generar representación abstracta
* CU-08: Renderizar vista previa UI
* CU-11: Seleccionar perfil de impresora
* CU-12: Adaptar documento al perfil
* RC-06: Perfil de dispositivo condiciona la salida final

---

# 13. Ejemplo Simplificado

Documento abstracto:

```text id="doc_ui_1"
Ticket
Total: 100
```

Vista previa UI:

```text id="ui_preview_1"
+----------------------+
|       Ticket         |
|----------------------|
| Total:      100      |
+----------------------+
```

Representado como una estructura visual centrada en pantalla.

---

# 14. Historial de Versiones

| Versión | Fecha      | Autor               | Cambios                            |
| ------- | ---------- | ------------------- | ---------------------------------- |
| 1.0     | 2026-03-28 | Equipo Arquitectura | Versión inicial de vista previa UI |

---

**Fin del documento**
