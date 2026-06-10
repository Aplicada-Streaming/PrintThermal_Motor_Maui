# Regla de Negocio: Validación de Esquema de Plantilla DSL

**Código:** RN-01
**Archivo:** RN-01-validacion-esquema-dsl_v1.0.md
**Versión:** 1.0
**Estado:** Aprobada
**Fecha:** 2026-03-28
**Autor:** Equipo Funcional / Arquitectura

---

# 1. Descripción

Esta regla de negocio establece que toda plantilla DSL debe cumplir con el esquema estructural definido por el motor antes de ser utilizada en cualquier proceso de generación de documentos.

El objetivo es garantizar que las plantillas sean válidas, consistentes y ejecutables, evitando errores en tiempo de ejecución y asegurando la estabilidad del sistema.

---

# 2. Motivación de Negocio

El uso de plantillas DSL permite desacoplar el diseño del documento del código, pero introduce el riesgo de configuraciones inválidas que pueden afectar el funcionamiento del sistema.

La validación del esquema asegura que todas las plantillas cumplan con una estructura definida, reduciendo errores, facilitando mantenimiento y permitiendo que usuarios no técnicos definan documentos de forma controlada.

---

# 3. Definición de la Regla

El sistema debe validar que toda plantilla DSL cumpla con el esquema estructural definido antes de ser procesada por el motor.

La validación debe incluir:

* Estructura jerárquica correcta
* Nodos permitidos
* Tipos de elementos válidos
* Propiedades obligatorias
* Sintaxis correcta

Si la plantilla no cumple el esquema, el sistema debe rechazarla y no permitir su ejecución.

---

# 4. Esquema de Plantilla (Tipos de Nodo Soportados)

Los tipos de nodo soportados por el parser/validador son exactamente seis. El nodo raíz es un `container`; el encabezado, cuerpo y pie se modelan agrupando con `container` anidados.

| Tipo        | Descripción                                                  |
| ----------- | ------------------------------------------------------------ |
| text        | Elemento de texto                                            |
| container   | Agrupador de elementos (nodo raíz y secciones)              |
| conditional | Bloque condicional (control de flujo, formato clásico)       |
| loop        | Iteración sobre una colección (control de flujo, clásico)    |
| table       | Estructura de datos tabulares (Headers/Rows)                 |
| image       | Imagen; el QR es un `image` con `imageType` `"qrcode"` (y barcode con `"barcode"`) |

---

# 5. Condiciones de Aplicación

La regla se ejecuta en los siguientes momentos:

* Al cargar una plantilla (CU-13)
* Al validar una plantilla (CU-14)
* Antes de ejecutar el motor DSL

Debe aplicarse a todas las plantillas, independientemente de su origen:

* local
* descargada por API
* generada dinámicamente

---

# 6. Resultados Esperados

* Solo plantillas válidas son utilizadas por el motor.
* Se previenen errores en tiempo de ejecución.
* Se asegura consistencia en el diseño de documentos.

---

# 7. Excepciones

## EX-01 Plantilla con advertencias

Si la plantilla presenta problemas no críticos:

* El sistema puede permitir su uso.
* Debe registrar advertencias.
* Debe notificar al usuario o desarrollador.

---

## EX-02 Esquema no actualizado

Si el esquema del motor cambia:

* Plantillas antiguas pueden volverse incompatibles.
* El sistema debe informar la incompatibilidad.
* Puede requerirse migración de plantillas.

---

# 8. Criterios de Validación

## CV-01 Estructura válida

**Dado** una plantilla DSL
**Cuando** se valida
**Entonces** cumple el esquema definido.

---

## CV-02 Rechazo de plantilla inválida

**Dado** una plantilla incorrecta
**Cuando** se valida
**Entonces** el sistema la rechaza.

---

## CV-03 Validación de nodos

**Dado** una plantilla
**Cuando** se analiza
**Entonces** solo contiene nodos permitidos.

---

## CV-04 Validación de propiedades

**Dado** un elemento DSL
**Cuando** se valida
**Entonces** contiene propiedades requeridas.

---

# 9. Impacto

Esta regla impacta directamente en:

* CU-13 Cargar plantilla DSL
* CU-14 Validar plantilla DSL
* CU-30 Manejar errores de plantilla inválida
* Pipeline completo del motor DSL

Cualquier modificación en el esquema DSL impactará en todas las plantillas existentes y deberá gestionarse mediante versionado.

---

# 10. Control de Cambios

| Versión | Fecha      | Descripción     |
| ------- | ---------- | --------------- |
| 1.0     | 2026-03-28 | Versión inicial |

---

**Fin del documento**
