# Regla de Negocio: Tipos de Elementos Soportados en DSL

**Código:** RN-02
**Archivo:** RN-02-tipos-elementos-soportados_v1.0.md
**Versión:** 1.0
**Estado:** Aprobada
**Fecha:** 2026-03-28
**Autor:** Equipo Funcional / Arquitectura

---

# 1. Descripción

Esta regla de negocio establece que las plantillas DSL solo pueden utilizar tipos de elementos previamente definidos y soportados por el motor.

El objetivo es garantizar consistencia en la interpretación de las plantillas, evitando ambigüedades o comportamientos no definidos durante el procesamiento del documento.

---

# 2. Motivación de Negocio

Permitir tipos de elementos arbitrarios en las plantillas introduce riesgos de incompatibilidad, errores de ejecución y dificultades de mantenimiento.

Al restringir los elementos a un conjunto controlado, se asegura que:

* el motor pueda procesarlos correctamente
* los renderizadores puedan interpretarlos
* el sistema sea predecible y extensible

---

# 3. Definición de la Regla

El sistema debe validar que todos los elementos utilizados en una plantilla DSL pertenezcan al conjunto de tipos soportados por el motor.

Si un elemento no está soportado:

* la plantilla debe ser rechazada (error crítico), o
* el elemento debe ser ignorado o degradado (según configuración)

---

# 4. Tipos de Elementos Soportados

El conjunto cerrado de tipos soportados por el motor son exactamente seis. Los nodos `loop` y `conditional` son los elementos de control de flujo. Las filas de una tabla son datos (`Headers`/`Rows`) dentro de `TableNode`, no nodos independientes.

| Tipo        | Descripción                                                            |
| ----------- | --------------------------------------------------------------------- |
| text        | Texto simple                                                          |
| container   | Agrupador de elementos                                                |
| conditional | Bloque condicional (control de flujo)                                 |
| loop        | Iteración sobre una colección (control de flujo)                      |
| table       | Tabla de datos (Headers/Rows)                                         |
| image       | Imagen; el QR es un `image` con `imageType` `"qrcode"` (barcode con `"barcode"`) |

---

# 5. Condiciones de Aplicación

La regla se ejecuta en los siguientes momentos:

* Validación de plantilla (CU-14)
* Ejecución del motor DSL
* Extensión del sistema con nuevos elementos

Debe aplicarse a todas las plantillas sin excepción.

---

# 6. Resultados Esperados

* Las plantillas contienen únicamente elementos válidos.
* El motor puede procesar todos los nodos sin ambigüedad.
* Los renderizadores pueden interpretar correctamente el documento.

---

# 7. Excepciones

## EX-01 Elemento desconocido con tolerancia

Si el sistema está configurado en modo tolerante:

* El elemento desconocido puede ser ignorado.
* Se registra una advertencia.
* El procesamiento continúa.

---

## EX-02 Extensión del motor

Si se incorpora un nuevo tipo de elemento:

* Debe registrarse formalmente en el motor.
* Debe actualizarse el esquema DSL.
* Debe garantizarse compatibilidad con renderizadores.

---

# 8. Criterios de Validación

## CV-01 Uso de elementos válidos

**Dado** una plantilla DSL
**Cuando** se valida
**Entonces** todos los elementos pertenecen al conjunto soportado.

---

## CV-02 Rechazo de elemento inválido

**Dado** un elemento no soportado
**Cuando** se procesa
**Entonces** el sistema lo rechaza o informa error.

---

## CV-03 Compatibilidad con renderizadores

**Dado** un elemento DSL
**Cuando** se renderiza
**Entonces** es interpretado correctamente.

---

## CV-04 Registro de advertencias

**Dado** un elemento desconocido en modo tolerante
**Cuando** se procesa
**Entonces** se registra advertencia.

---

# 9. Impacto

Esta regla impacta directamente en:

* CU-14 Validar plantilla DSL
* CU-29 Extender motor con nuevos renderizadores
* CU-30 Manejar errores de plantilla inválida
* Modelo de documento abstracto

Cualquier modificación en los tipos soportados implica actualizar el motor y los renderizadores asociados.

---

# 10. Control de Cambios

| Versión | Fecha      | Descripción     |
| ------- | ---------- | --------------- |
| 1.0     | 2026-03-28 | Versión inicial |

---

**Fin del documento**
