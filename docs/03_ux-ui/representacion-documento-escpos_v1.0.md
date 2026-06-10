# Representación de Documento en ESC/POS

**Proyecto:** Motor DSL de Documentos para Impresión
**Documento:** representacion-documento-escpos_v1.0.md
**Versión:** 1.0
**Estado:** Aprobado
**Fecha:** 2026-03-28
**Autor:** Equipo de Arquitectura / Renderización

---

# 1. Introducción

Este documento describe la representación de documentos en formato ESC/POS dentro del motor DSL. Su objetivo es definir cómo un documento abstracto es transformado en comandos compatibles con impresoras térmicas que utilizan el estándar ESC/POS.

La representación ESC/POS constituye una de las salidas principales del motor y está orientada a dispositivos físicos con restricciones de ancho, encoding y capacidades específicas.

---

# 2. Alcance

Este documento cubre:

* Transformación del modelo abstracto a comandos ESC/POS
* Representación de elementos básicos del documento
* Manejo de layout en impresoras térmicas
* Codificación de texto e imágenes
* Construcción de secuencias de impresión

No incluye:

* Implementación interna del parser DSL
* Lógica de resolución de datos
* Renderizadores distintos a ESC/POS

---

# 3. Concepto de Representación ESC/POS

La representación ESC/POS consiste en una secuencia de bytes que contiene comandos específicos entendidos por impresoras térmicas compatibles.

Estos comandos permiten controlar:

* Texto
* Formato (negrita, alineación, tamaño)
* Imágenes
* Saltos de línea
* Corte de papel
* Códigos de barras y QR (según soporte)

El motor traduce el documento abstracto en esta secuencia de comandos respetando el perfil del dispositivo.

---

# 4. Estructura General de la Salida

La salida ESC/POS se compone de:

1. Inicialización de la impresora
2. Configuración de estilos
3. Renderizado de bloques de contenido
4. Inserción de elementos especiales
5. Finalización del documento
6. Comando de corte (opcional)

---

# 5. Representación de Elementos

## 5.1 Texto

El texto es el elemento base en ESC/POS.

**Características:**

* Codificación fija en CP437 (PC437) para el texto plano
* Soporte de estilos (negrita, subrayado, tamaño)
* Alineación (izquierda, centro, derecha)

**Ejemplo conceptual:**

```text id="t2x8aa"
[NEGRITA] Total: $100.00 [/NEGRITA]
```

Se traduce a comandos ESC/POS que activan estilos antes de imprimir el texto.

---

## 5.2 Alineación

Permite posicionar el contenido horizontalmente.

**Tipos:**

* Izquierda
* Centro
* Derecha

**Ejemplo:**

```text id="z9kq2m"
[CENTER] Ticket de Venta [/CENTER]
```

---

## 5.3 Líneas y Separadores

Se utilizan para estructurar visualmente el documento.

**Ejemplo:**

```text id="l1p4ss"
------------------------------
```

Puede generarse dinámicamente según el ancho del dispositivo.

---

## 5.4 Bloques de Contenido

Representan secciones del documento.

**Ejemplo:**

* Encabezado
* Detalle
* Totales

Cada bloque se renderiza secuencialmente respetando el layout definido por el motor.

---

## 5.5 Tablas Simuladas

ESC/POS no soporta tablas nativas, por lo que se simulan mediante:

* Espaciado manual
* Alineación de columnas
* Formateo de texto

**Ejemplo:**

```text id="k8d3lm"
Producto        Cantidad   Precio
----------------------------------
Café            2          5.00
```

---

## 5.6 Imágenes

Soportadas en impresoras compatibles.

**Características:**

* Conversión a bitmap
* Escalado según ancho del dispositivo
* Conversión a monocromo

**Uso típico:**

* Logos
* QR codes
* Códigos de barras

---

## 5.7 Códigos QR y Barras

Dependiendo del dispositivo, pueden generarse comandos específicos.

**Ejemplo conceptual:**

```text id="qr1x9p"
[QR] https://mi-url.com [/QR]
```

---

## 5.8 Saltos de Línea

Permiten separar contenido verticalmente.

**Ejemplo:**

```text id="nl3k8a"
\n\n
```

O mediante comandos ESC/POS equivalentes.

---

## 5.9 Corte de Papel

Comando final para finalizar la impresión.

**Tipos:**

* Corte total
* Corte parcial

---

# 6. Flujo de Renderizado a ESC/POS

```text id="flowescpos1"
Documento Abstracto
        ↓
Modelo Interno
        ↓
Motor de Layout
        ↓
Representación Abstracta
        ↓
Renderizador ESC/POS
        ↓
Secuencia de Comandos
        ↓
Impresora
```

---

# 7. Consideraciones de Layout

El renderizado a ESC/POS debe considerar:

* Ancho máximo en caracteres
* Espaciado entre columnas
* Ajuste de líneas largas
* Truncamiento o wrapping
* Alineación consistente

El layout se adapta dinámicamente según el perfil del dispositivo.

---

# 8. Codificación y Compatibilidad

La representación debe considerar:

* Encoding del texto plano: el renderer ESC/POS **fuerza CP437 (PC437)** mediante `ESC t 0` (`1B 74 00`) y codifica siempre el texto en CP437 (fallback ASCII). No hay soporte UTF-8 en la salida ESC/POS de texto.
* La capability `codepage` (`"850"`/`"437"`/otro→ASCII) **solo** influye en la codificación de los datos de QR/barcode, **no** en el texto plano.
* Compatibilidad de caracteres especiales
* Normalización de texto
* Sustitución de caracteres no soportados

---

# 9. Limitaciones del Formato ESC/POS

* No soporta layouts complejos tipo HTML
* No tiene sistema de posicionamiento absoluto
* Limitado en tipografías
* Dependiente del hardware
* Variaciones entre fabricantes

Estas limitaciones deben ser manejadas por el motor mediante adaptación del contenido.

---

# 10. Estrategias de Adaptación

El motor puede aplicar:

* Ajuste de ancho de columnas
* Reducción de contenido
* Reemplazo de elementos no soportados
* Fallback de imágenes a texto
* Reestructuración de bloques

---

# 11. Relación con Otros Componentes

Esta representación depende de:

* CU-05: Generar representación abstracta
* CU-06: Renderizar a ESC/POS
* CU-11: Seleccionar perfil de impresora
* CU-12: Adaptar documento al perfil
* RC-06: Perfil de dispositivo condiciona la salida

---

# 12. Ejemplo Simplificado

Documento abstracto:

```text id="doc1"
Título: Ticket
Total: 100
```

Representación ESC/POS aproximada:

```text id="esc1"
[INIT]
[CENTER] Ticket [/CENTER]
--------------------------------
Total: 100
--------------------------------
[CUT]
```

---

# 13. Historial de Versiones

| Versión | Fecha      | Autor               | Cambios                                   |
| ------- | ---------- | ------------------- | ----------------------------------------- |
| 1.0     | 2026-03-28 | Equipo Arquitectura | Versión inicial de representación ESC/POS |

---

**Fin del documento**
