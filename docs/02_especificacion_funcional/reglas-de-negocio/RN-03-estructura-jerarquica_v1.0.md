# Regla de Negocio: Estructura Jerárquica del Documento DSL

**Código:** RN-03
**Archivo:** RN-03-estructura-jerarquica_v1.0.md
**Versión:** 1.0
**Estado:** Aprobada
**Fecha:** 2026-03-28
**Autor:** Equipo Funcional / Arquitectura

---

# 1. Descripción

Esta regla de negocio establece que toda plantilla DSL debe definirse mediante una estructura jerárquica de nodos, donde cada elemento puede contener hijos formando un árbol de composición del documento.

El objetivo es asegurar una representación consistente, ordenada y procesable del documento, permitiendo al motor DSL interpretar correctamente la disposición y relación entre los elementos.

---

# 2. Motivación de Negocio

Una estructura jerárquica permite modelar documentos complejos de forma clara y escalable, facilitando:

* la organización del contenido
* la reutilización de componentes
* la correcta interpretación por parte del motor
* la compatibilidad con múltiples renderizadores

Sin esta estructura, el sistema perdería capacidad de composición, generando ambigüedades en el layout.

---

# 3. Definición de la Regla

El sistema debe garantizar que toda plantilla DSL esté organizada como un árbol jerárquico donde:

* existe un único nodo raíz
* cada nodo puede tener cero o más hijos
* la relación padre-hijo define la composición del documento
* no se permiten estructuras planas sin jerarquía ni ciclos

---

# 4. Estructura Jerárquica (Ejemplo Simplificado)

```
container (raíz)
 ├── container (encabezado)
 │    └── text
 ├── container (cuerpo)
 │    ├── table        (Headers/Rows como datos)
 │    └── image        (QR: imageType "qrcode")
 └── container (pie)
      └── text
```

> Los tipos de nodo válidos son `text`, `container`, `conditional`, `loop`, `table`, `image`. El árbol se compone con un `container` raíz; encabezado, cuerpo y pie son `container` anidados. El QR se representa como `image` con `imageType` `"qrcode"`.

---

# 5. Condiciones de Aplicación

La regla se ejecuta en los siguientes momentos:

* Validación de plantilla (CU-14)
* Construcción del modelo interno (CU-03)
* Ejecución del motor de layout (CU-04)

Debe aplicarse a todas las plantillas DSL.

---

# 6. Resultados Esperados

* El documento puede ser interpretado como un árbol estructurado.
* El motor puede recorrer y procesar cada nodo correctamente.
* Los renderizadores pueden respetar la estructura del documento.

---

# 7. Excepciones

## EX-01 Nodo sin hijos permitido

Si un nodo es terminal:

* Puede no tener hijos.
* No se considera error.

---

## EX-02 Estructura inválida

Si la plantilla contiene:

* múltiples raíces
* ciclos
* nodos huérfanos

Entonces:

* el sistema debe rechazar la plantilla.

---

# 8. Criterios de Validación

## CV-01 Nodo raíz único

**Dado** una plantilla DSL
**Cuando** se valida
**Entonces** existe un único nodo raíz.

---

## CV-02 Relación padre-hijo válida

**Dado** un nodo
**Cuando** se analiza
**Entonces** sus hijos están correctamente definidos.

---

## CV-03 Ausencia de ciclos

**Dado** la estructura del documento
**Cuando** se valida
**Entonces** no existen referencias cíclicas.

---

## CV-04 Procesamiento jerárquico

**Dado** una plantilla válida
**Cuando** se ejecuta el motor
**Entonces** el recorrido respeta la jerarquía.

---

# 9. Impacto

Esta regla impacta directamente en:

* CU-03 Construir modelo interno del documento
* CU-04 Ejecutar motor de layout
* CU-05 Generar representación abstracta
* CU-14 Validar plantilla DSL

La estructura jerárquica es la base del funcionamiento del motor DSL.

---

# 10. Control de Cambios

| Versión | Fecha      | Descripción     |
| ------- | ---------- | --------------- |
| 1.0     | 2026-03-28 | Versión inicial |

---

**Fin del documento**
