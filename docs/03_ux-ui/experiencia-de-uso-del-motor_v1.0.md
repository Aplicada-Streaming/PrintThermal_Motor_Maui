# Experiencia de Uso del Motor DSL

**Proyecto:** Motor DSL de Documentos para Impresión
**Documento:** experiencia-de-uso-del-motor_v1.0.md
**Versión:** 1.0
**Estado:** Aprobado
**Fecha:** 2026-03-28
**Autor:** Equipo de Arquitectura / Funcional

---

# 1. Introducción

Este documento describe la experiencia de uso del motor DSL desde la perspectiva de los sistemas consumidores (developers). Su objetivo es definir cómo una aplicación cliente interactúa con el motor para generar documentos a partir de plantillas, datos y perfiles de dispositivos.

A diferencia de un sistema tradicional de UX/UI orientado a usuarios finales, este documento se centra en la experiencia de integración, configuración y ejecución del motor como una librería reutilizable.

---

# 2. Alcance

Este documento cubre:

* Integración del motor en aplicaciones cliente
* Flujo de uso del motor DSL
* Interacción con plantillas
* Provisión de datos
* Selección de perfiles de impresora
* Ejecución de renderizados
* Obtención de salidas en distintos formatos

No incluye:

* Interfaces gráficas de usuario final
* Diseño visual de aplicaciones consumidoras
* Implementaciones internas del motor

---

# 3. Actores

## 3.1 Aplicación Cliente

Sistema que utiliza el motor DSL para generar documentos.

**Ejemplo:** Aplicación .NET MAUI que imprime tickets desde una interfaz de usuario.

---

## 3.2 Motor DSL

Librería encargada de interpretar plantillas, procesar datos y generar representaciones renderizadas.

**Ejemplo:** Motor que convierte una plantilla DSL en comandos ESC/POS.

---

## 3.3 Fuente de Datos

Origen de los datos utilizados por el documento.

**Ejemplo:** API REST, base de datos o modelo en memoria.

---

## 3.4 Dispositivo de Salida

Destino final del documento renderizado.

**Ejemplo:** Impresora térmica Bluetooth o visor en pantalla.

---

# 4. Descripción General de la Experiencia

La experiencia de uso del motor DSL se basa en un flujo estructurado de etapas que una aplicación cliente debe seguir para generar un documento:

1. Configuración del motor
2. Carga de plantilla DSL
3. Carga de datos
4. Selección de perfil de dispositivo
5. Construcción del documento
6. Renderizado
7. Consumo de la salida

Este flujo permite desacoplar completamente el contenido del documento de su representación final.

---

# 5. Flujo de Uso del Motor

## 5.1 Inicialización del Motor

La aplicación cliente debe inicializar el motor con la configuración base requerida.

**Ejemplo conceptual:**

```text
Motor.Initialize(configuración)
```

**Objetivo:**

Preparar el entorno del motor para procesar documentos.

---

## 5.2 Carga de Plantilla DSL

La aplicación debe proporcionar una plantilla DSL válida al motor.

**Ejemplo:**

```text
Motor.CargarPlantilla(plantillaDSL)
```

**Resultado esperado:**

La plantilla queda registrada y lista para ser procesada.

---

## 5.3 Validación de Plantilla

El motor valida que la plantilla cumpla con el esquema DSL definido.

**Si es válida:**

* Se acepta la plantilla

**Si no es válida:**

* Se devuelve error de validación
* Se detiene el flujo

---

## 5.4 Provisión de Datos

La aplicación cliente proporciona los datos necesarios para resolver la plantilla.

**Ejemplo:**

```text
Motor.CargarDatos(datos)
```

**Consideraciones:**

* Los datos deben cumplir con la estructura esperada
* Pueden provenir de APIs o fuentes internas

---

## 5.5 Selección de Perfil de Dispositivo

La aplicación selecciona el perfil que define las capacidades del dispositivo destino.

**Ejemplo:**

```text
Motor.SeleccionarPerfil(perfilImpresora)
```

**Objetivo:**

Adaptar la salida a restricciones como ancho, encoding o soporte de elementos.

---

## 5.6 Construcción del Documento

El motor procesa:

* Plantilla
* Datos
* Reglas DSL

y construye un modelo interno del documento.

**Resultado:**

Representación estructurada lista para renderizado.

---

## 5.7 Renderizado

El motor genera una salida específica según el target de renderizado solicitado. Los targets reales de `Render` son:

* `escpos`
* `escpos-bitmap`
* `text` (texto plano)
* `pdf`
* `raster-preview` (vista previa pixelada en PNG)

La **vista previa visual** en pantalla no es un target de `Render`: se obtiene con `RenderLayout(...)` que devuelve un `LayoutedDocument` (consumido por el control MAUI `MauiDocumentPreview`), o con el target `raster-preview` (PNG consumido por `MauiRasterPreview`).

**Ejemplo conceptual:**

```text
Motor.Renderizar(Formato.ESC_POS)
```

---

## 5.8 Consumo de la Salida

La aplicación cliente recibe el resultado del renderizado y lo utiliza según el caso:

* Enviar a impresora
* Mostrar en pantalla
* Guardar en archivo
* Log para debugging

---

# 6. Modos de Uso

## 6.1 Modo Impresión

Flujo orientado a generar comandos para impresoras térmicas.

**Características:**

* Uso de ESC/POS
* Adaptación a ancho del dispositivo
* Optimización de layout

---

## 6.2 Modo Vista Previa

Permite visualizar el documento antes de imprimirlo.

**Características:**

* Renderizado visual en UI
* Representación similar al output final
* Útil para validación

---

## 6.3 Modo Debug

Genera una representación en texto plano del documento.

**Características:**

* Facilita diagnóstico
* Permite inspección estructural
* No depende de hardware

---

# 7. Configuración del Motor

La experiencia de uso incluye parámetros configurables:

* Plantillas disponibles
* Fuentes de datos
* Renderizadores habilitados
* Perfiles de dispositivos
* Estrategias de layout

**Ejemplo:**

```text
Config:
- Renderizador ESC/POS
- Perfil: 58mm
- Fuente de datos: API REST
```

---

# 8. Manejo de Errores

El motor debe comunicar claramente los errores al consumidor:

* Error de plantilla inválida
* Error de datos inconsistentes
* Error de renderizado
* Error de dispositivo no soportado

**Principios:**

* Errores descriptivos
* Trazabilidad
* No ejecución parcial silenciosa

---

# 9. Consideraciones de Experiencia

* El motor debe ser fácil de integrar en distintas plataformas
* La API debe ser consistente y predecible
* El flujo debe ser claro y secuencial
* La separación entre datos, plantilla y renderizado debe ser explícita
* El comportamiento debe ser determinista

---

# 10. Casos de Uso Asociados

| Caso de Uso | Descripción                       |
| ----------- | --------------------------------- |
| CU-13       | Cargar plantilla DSL              |
| CU-14       | Validar plantilla DSL             |
| CU-15       | Resolver referencias a datos      |
| CU-18       | Cargar datos del documento        |
| CU-21       | Cargar perfil de impresora        |
| CU-29       | Extender motor con renderizadores |
| CU-06       | Renderizar a ESC/POS              |
| CU-07       | Renderizar a texto plano          |
| CU-08       | Renderizar vista previa UI (se obtiene con `RenderLayout`/`LayoutedDocument` o el target `raster-preview`, no con un renderizador "ui") |

---

# 11. Historial de Versiones

| Versión | Fecha      | Autor               | Cambios                                    |
| ------- | ---------- | ------------------- | ------------------------------------------ |
| 1.0     | 2026-03-28 | Equipo Arquitectura | Versión inicial del documento UX del motor |

---

**Fin del documento**
