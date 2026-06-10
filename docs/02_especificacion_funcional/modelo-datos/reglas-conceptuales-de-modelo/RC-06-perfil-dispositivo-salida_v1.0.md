# Regla Conceptual: Perfil de Dispositivo Condiciona la Salida Final

**Código:** RC-06
**Archivo:** RC-06-perfil-dispositivo-salida_v1.0.md
**Versión:** 1.0
**Estado:** Aprobada
**Fecha:** 2026-03-28
**Autor:** Equipo Funcional / Arquitectura

---

# 1. Descripción

Esta regla conceptual establece que el perfil del dispositivo define y condiciona la forma en que se genera la salida final del documento.

El objetivo es asegurar que el contenido renderizado sea compatible con las capacidades del dispositivo destino, evitando errores de impresión o representación.

---

# 2. Motivación

Los dispositivos de salida pueden variar significativamente en sus capacidades, tales como:

* ancho de impresión
* soporte de imágenes
* soporte de códigos QR
* tipo de encoding
* limitaciones de memoria o resolución

Sin considerar estas diferencias, el documento podría:

* no visualizarse correctamente
* truncarse o deformarse
* fallar durante la impresión

El perfil del dispositivo permite adaptar la salida a estas restricciones de forma controlada.

---

# 3. Definición de la Regla

* Todo proceso de renderizado debe considerar un perfil de dispositivo activo.
* El perfil define las capacidades y restricciones que afectan la generación de la salida.
* La salida final debe ajustarse a las características del perfil seleccionado.
* Diferentes perfiles pueden producir distintas representaciones a partir del mismo documento abstracto.

---

# 4. Condiciones de Aplicación

* Aplica durante la adaptación del documento al perfil y el renderizado (CU-12 Adaptar documento al perfil, CU-23 Aplicar restricciones del dispositivo).
* Aplica al seleccionar el dispositivo de salida antes de la impresión (CU-11).
* Aplica a todos los documentos que sean enviados a dispositivos físicos o virtuales.

---

# 5. Resultados Esperados

* La salida generada es compatible con el dispositivo destino.
* No se producen errores por incompatibilidad de capacidades.
* El contenido se adapta a restricciones como ancho, resolución y formato.
* El mismo documento puede renderizarse correctamente en distintos dispositivos.

---

# 6. Excepciones

## EX-01 Perfil inexistente o inválido

Si no se especifica un perfil de dispositivo válido:

* el sistema debe rechazar la operación de renderizado
* debe informarse el error correspondiente
* no se debe continuar con la generación de salida

---

## EX-02 Incompatibilidad de capacidades

Si el documento contiene elementos no soportados por el perfil:

* el sistema debe aplicar estrategias de adaptación (si están definidas)
* o rechazar el renderizado si no es posible la adaptación
* debe registrarse la incidencia

---

# 7. Criterios de Validación

## CV-01 Selección de perfil

**Dado** un proceso de renderizado
**Cuando** se inicia
**Entonces** se utiliza un perfil de dispositivo válido

---

## CV-02 Adaptación de salida

**Dado** un documento abstracto
**Cuando** se renderiza con un perfil específico
**Entonces** la salida se ajusta a las capacidades del dispositivo

---

## CV-03 Compatibilidad de elementos

**Dado** un perfil de dispositivo
**Cuando** se procesan elementos del documento
**Entonces** solo se utilizan aquellos soportados por el perfil

---

## CV-04 Consistencia entre dispositivos

**Dado** un mismo documento abstracto
**Cuando** se renderiza en distintos perfiles
**Entonces** cada salida respeta las limitaciones de su dispositivo correspondiente

---

# 8. Impacto

Esta regla impacta directamente en:

* CU-11 Seleccionar perfil de impresora
* CU-12 Adaptar documento al perfil
* CU-23 Aplicar restricciones del dispositivo
* CU-29 Extender motor con nuevos renderizadores
* CU-32 Manejar errores de impresión
* CU-21 Cargar perfil de impresora (absorbe el antiguo CU-26 "Descargar perfil de impresora" en v2.0)

Es clave para garantizar que la salida del sistema sea adaptable y funcional en distintos entornos de impresión.

---

# 9. Control de Cambios

| Versión | Fecha      | Descripción                 |
| ------- | ---------- | --------------------------- |
| 1.0     | 2026-03-28 | Versión inicial de la regla |

---

**Fin del documento**
