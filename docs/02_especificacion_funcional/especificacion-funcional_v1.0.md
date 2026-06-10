# Especificación Funcional

**Proyecto:** Motor DSL de Documentos para Impresión
**Documento:** especificacion-funcional_v1.0.md
**Estado:** Aprobado
**Fecha:** 2026-03-28
**Autor:** Equipo de Arquitectura / Funcional

---

# 1. Introducción

Este documento describe el comportamiento funcional del motor DSL de documentos orientado a la generación y renderizado de salidas para distintos dispositivos de impresión y visualización.

Su propósito es detallar qué debe hacer el sistema desde la perspectiva funcional, incluyendo la interpretación de plantillas DSL, la resolución de datos, la construcción de un modelo interno del documento y su posterior renderizado a distintos formatos.

La especificación establece los procesos principales, reglas de negocio, actores involucrados y criterios de aceptación que validan que el sistema cumple con los requerimientos del dominio.

---

# 2. Alcance

El sistema permitirá:

* Cargar y validar plantillas DSL.
* Resolver datos dinámicos desde fuentes externas o internas.
* Construir un modelo interno del documento.
* Ejecutar un motor de layout.
* Renderizar el documento a múltiples formatos. Salidas reales:
  * **ESC/POS** (Target `escpos`, `EscPosRenderer`) y **texto plano** (Target `text`, `TextRenderer`) — núcleo (`MotorDsl.Core` + `MotorDsl.Rendering`).
  * **PDF** (Target `pdf`, `PdfRenderer` / PdfSharpCore), **ESC/POS bitmap** (Target `escpos-bitmap`, `BitmapEscPosRenderer`) y **vista raster PNG** (Target `raster-preview`, `RasterPreviewRenderer`) — vía `MotorDsl.Maui`.
* Adaptar la salida según perfiles de dispositivos.

Quedan fuera del alcance en esta versión:

* Integración con sistemas de pagos.
* Gestión de usuarios finales compleja (autenticación avanzada).
* Renderizado a formatos no definidos (más allá de los targets soportados: `escpos`, `text`, `pdf`, `escpos-bitmap`, `raster-preview`).

---

# 3. Actores

## 3.1 Sistema Cliente

Aplicación que consume el motor DSL para generar documentos.

**Ejemplo:** Aplicación .NET MAUI que envía datos y solicita renderizado.

---

## 3.2 Motor DSL

Componente central encargado de interpretar plantillas, procesar datos y generar salidas.

**Ejemplo:** Procesa una plantilla DSL y genera un ticket de impresión.

---

## 3.3 Fuente de Datos

Origen de los datos utilizados por el documento (API, base de datos, objetos en memoria).

**Ejemplo:** API REST que provee información de una orden.

---

## 3.4 Dispositivo de Salida

Hardware o software destino donde se renderiza el documento.

**Ejemplo:** Impresora Bluetooth ESC/POS o visor en pantalla.

---

# 4. Descripción General del Sistema

El sistema funciona como un motor de transformación de documentos basado en DSL.

El flujo general es:

1. Carga de plantilla DSL.
2. Validación de la plantilla.
3. Carga y validación de datos.
4. Resolución de referencias dinámicas.
5. Construcción del modelo interno del documento.
6. Ejecución del motor de layout.
7. Generación de representación abstracta.
8. Renderizado según el tipo de salida (ESC/POS, texto, UI).
9. Aplicación de perfil de dispositivo.
10. Envío al dispositivo de salida.

El sistema debe garantizar consistencia entre la plantilla, los datos y el dispositivo destino.

---

# 5. Funcionalidades Principales

## 5.1 Carga y Validación de Plantilla DSL

El sistema debe permitir cargar una plantilla DSL y validar su estructura conforme al esquema definido.

**Ejemplo:**
Se carga una plantilla de ticket de venta y se valida su sintaxis.

---

## 5.2 Resolución de Datos

El sistema debe resolver referencias a datos definidos en la plantilla utilizando fuentes externas o internas.

**Ejemplo:**
Una variable {{cliente.nombre}} se reemplaza por el valor real proveniente de la API.

---

## 5.3 Construcción del Modelo Interno

El sistema debe construir una representación estructurada del documento a partir de la plantilla y los datos resueltos.

**Ejemplo:**
Un árbol jerárquico que representa secciones, bloques y elementos del documento.

---

## 5.4 Evaluación de Lógica de Plantilla

El sistema debe evaluar condiciones e iteraciones definidas en la plantilla DSL.

**Ejemplo:**
Mostrar un bloque solo si el total es mayor a cero.

---

## 5.5 Motor de Layout

El sistema debe calcular la disposición visual o estructural de los elementos del documento.

**Ejemplo:**
Alinear columnas, ajustar saltos de línea, distribuir contenido según ancho disponible.

---

## 5.6 Generación de Representación Abstracta

El sistema debe producir una representación intermedia independiente del renderizador.

**Ejemplo:**
Un modelo abstracto que puede ser convertido a ESC/POS, texto o UI.

---

## 5.7 Renderizado a ESC/POS

El sistema debe convertir el documento a comandos compatibles con impresoras ESC/POS.

**Ejemplo:**
Generación de comandos para imprimir texto, líneas y códigos de barras.

---

## 5.8 Renderizado a Texto Plano

El sistema debe generar una representación textual del documento útil para debugging o logs.

**Ejemplo:**
Salida en consola con estructura legible del documento.

---

## 5.9 Renderizado a Vista Previa (UI)

El sistema debe permitir visualizar el documento en una interfaz gráfica.

**Ejemplo:**
Preview en pantalla dentro de una aplicación MAUI.

---

## 5.10 Selección de Perfil de Dispositivo

El sistema debe permitir seleccionar un perfil de dispositivo que defina capacidades y restricciones.

**Ejemplo:**
Perfil para impresora de 58mm vs 80mm.

---

## 5.11 Adaptación al Dispositivo

El sistema debe adaptar el documento según el perfil del dispositivo seleccionado.

**Ejemplo:**
Reducir ancho de columnas o truncar contenido si es necesario.

---

## 5.12 Envío a Impresora Bluetooth

El sistema debe enviar la salida renderizada a una impresora Bluetooth compatible.

**Ejemplo:**
Envío de bytes ESC/POS a una impresora portátil.

---

# 6. Reglas de Negocio

* RN-01: Toda plantilla debe cumplir el esquema DSL definido.
* RN-02: Solo se permiten elementos soportados por el motor.
* RN-03: La estructura del documento debe ser jerárquica.
* RN-04: Debe registrarse trazabilidad de ejecución y renderizado.
* RN-05: El documento abstracto no depende del renderizador.
* RN-06: El perfil del dispositivo condiciona la salida final.

**Ejemplo práctico:**
Una plantilla con elementos no soportados debe ser rechazada en validación.

---

# 7. Datos Principales

## 7.1 Documento

* DocumentoId
* PlantillaId
* DatosAsociados
* ModeloInterno
* RepresentacionAbstracta
* PerfilDispositivo
* ResultadoRenderizado

**Ejemplo:**
Documento de ticket generado para una orden de compra.

---

## 7.2 Plantilla DSL

* PlantillaId
* ContenidoDSL
* Version
* Estado

**Ejemplo:**
Plantilla de ticket con secciones de encabezado, detalle y totales.

---

## 7.3 Perfil de Dispositivo

* PerfilId
* TipoDispositivo
* AnchoCaracteres
* SoporteImagen
* SoporteQR

**Ejemplo:**
Perfil 58mm para impresoras térmicas.

---

# 8. Criterios de Aceptación

## CA-01 Carga de plantilla

**Dado** una plantilla DSL válida
**Cuando** se carga en el sistema
**Entonces** el sistema la acepta y la valida correctamente

---

## CA-02 Resolución de datos

**Dado** una plantilla con referencias a datos
**Cuando** se ejecuta el proceso
**Entonces** todas las referencias son resueltas correctamente

---

## CA-03 Renderizado exitoso

**Dado** un documento válido
**Cuando** se renderiza a un formato soportado
**Entonces** se genera una salida consistente con el perfil del dispositivo

---

## CA-04 Adaptación a dispositivo

**Dado** un perfil de dispositivo limitado
**Cuando** se renderiza un documento complejo
**Entonces** el sistema adapta la salida sin romper la estructura

---

## CA-05 Envío a impresora

**Dado** una salida ESC/POS válida
**Cuando** se envía a una impresora Bluetooth
**Entonces** el documento se imprime correctamente

---

# 9. Supuestos y Restricciones

Se asume que:

* Las plantillas DSL cumplen con el esquema definido.
* Las fuentes de datos están disponibles al momento de la ejecución.
* Los dispositivos de impresión soportan ESC/POS u otros formatos definidos.

El sistema deberá operar de forma multiplataforma y ser integrable con aplicaciones cliente como .NET MAUI.

---

# 10. Trazabilidad

| Necesidad de negocio             | Funcionalidad                | Caso de uso         |
| -------------------------------- | ---------------------------- | ------------------- |
| Generar documentos dinámicos     | Carga de plantilla DSL       | CU-13               |
| Validar estructura de plantillas | Validación DSL               | CU-14               |
| Personalizar documentos          | Resolución de datos          | CU-15               |
| Procesar listas                  | Iteraciones                  | CU-16               |
| Aplicar lógica condicional       | Evaluación de condiciones    | CU-17               |
| Inyectar datos externos          | Carga de datos               | CU-18               |
| Garantizar consistencia de datos | Validación de estructura     | CU-19               |
| Vincular datos con plantilla     | Mapeo de datos               | CU-20               |
| Configurar dispositivos          | Perfil de impresora          | CU-21               |
| Asegurar compatibilidad          | Validación de perfil         | CU-22               |
| Respetar limitaciones físicas    | Restricciones de dispositivo | CU-23               |
| Integración con APIs             | Descarga de recursos         | CU-24, CU-25, CU-26 |
| Integración con apps cliente     | Integración .NET MAUI        | CU-27               |
| Configuración del motor          | Configuración DSL            | CU-28               |
| Extensibilidad                   | Nuevos renderizadores        | CU-29               |

---

**Fin del documento**
