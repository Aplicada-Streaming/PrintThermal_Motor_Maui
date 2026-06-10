# Caso de Uso: Proveer Datos al Motor

**Código:** CU-25
**Archivo:** CU-25-proveer-datos-motor_v2.0.md
**Versión:** 2.0
**Estado:** Aprobado
**Fecha:** 2026-05-27
**Autor:** Equipo Funcional / Arquitectura

> **Renombrado en v2.0:** Anteriormente "Descargar Datos desde API" (CU-25 v1.0). El concepto se generalizó a "Proveer Datos al Motor" para reflejar que la fuente de datos es responsabilidad del cliente.

---

# 1. Propósito

Este caso de uso describe el mecanismo mediante el cual la librería del motor DSL recibe los datos del documento a través de un proveedor configurable (`IDataProvider`).

El objetivo es desacoplar la fuente de datos del motor, permitiendo que el sistema cliente decida cómo y desde dónde se obtienen los datos (objetos en memoria, API REST, base de datos, archivos, etc.).

---

# 2. Actores

## Actor Primario

**Sistema consumidor** — Registra e inyecta su implementación de `IDataProvider`.

## Actores Secundarios

**Librería Motor DSL** — Define el contrato `IDataProvider` y provee `InMemoryDataProvider` como implementación default.
**Motor DSL** — Consume datos a través del proveedor registrado.

---

# 3. Precondiciones

* El motor DSL se encuentra inicializado vía `AddMotorDslEngine()`.
* El cliente ha registrado una implementación de `IDataProvider` (o utiliza el default `InMemoryDataProvider`).
* Los datos necesarios han sido provistos al proveedor o se pasan directamente vía `engine.Render()`.

---

# 4. Postcondiciones

## En caso de éxito

* El motor obtiene los datos del documento a través del proveedor.
* Los datos quedan disponibles para validación y procesamiento (CU-18, CU-19, CU-15).
* El proveedor puede gestionar múltiples conjuntos de datos por clave.

## En caso de fallo

* El proveedor no encuentra los datos solicitados.
* El motor recibe `null` o una excepción controlada.
* Se informa el error al sistema cliente.

---

# 5. Flujo Principal

1. El sistema cliente provee datos al motor:

   ```csharp
   var data = new Dictionary<string, object>
   {
       ["empresa"] = "Mi Tienda",
       ["items"] = new List<object> { ... },
       ["total"] = 1500.00
   };

   var result = engine.Render(templateId, data, profileName);
   ```

   O mediante proveedor registrado:

   ```csharp
   services.AddSingleton<IDataProvider, ApiDataProvider>();
   ```

2. El motor DSL solicita datos al `IDataProvider`.
3. El proveedor obtiene los datos de su fuente interna.
4. El proveedor retorna el `IDictionary<string, object>` encontrado.
5. El motor procesa los datos (evaluación, binding, render).
6. El caso de uso finaliza.

---

# 6. Flujos Alternativos

## FA-01 Datos no encontrados

**En el paso 3**

* Si los datos no existen en el proveedor:

  * El proveedor retorna `null`.
  * El motor informa al cliente que los datos no fueron encontrados.

---

## FA-02 Proveedor no registrado

**En la configuración**

* Si el cliente no registra ningún `IDataProvider`:

  * El motor utiliza `InMemoryDataProvider` (vacío por defecto).
  * El cliente puede pasar datos directamente en `engine.Render()`.

---

## FA-03 Implementación personalizada (API, BD, archivo)

**En el paso 1**

* El cliente puede inyectar cualquier implementación:

  ```csharp
  services.AddSingleton<IDataProvider, ApiDataProvider>();
  ```

* La implementación maneja conectividad, serialización, cache, reintentos.
* La librería no impone restricciones sobre la fuente de datos.

---

## FA-04 Error técnico

**En cualquier punto**

* El proveedor puede lanzar excepciones si la fuente falla.
* El motor captura y reporta el error.
* El caso de uso finaliza.

---

# 7. Contrato del Proveedor

```csharp
public interface IDataProvider
{
    IDictionary<string, object>? GetData(string dataKey);
}
```

> **Nota:** `Add(string, IDictionary<string, object>)` **no** forma parte del contrato `IDataProvider`; es un método de la implementación `InMemoryDataProvider` (ver abajo).

**Implementación default:**

```csharp
public class InMemoryDataProvider : IDataProvider
{
    private readonly Dictionary<string, IDictionary<string, object>> _data = new();

    public IDictionary<string, object>? GetData(string dataKey)
        => _data.TryGetValue(dataKey, out var d) ? d : null;

    public void Add(string dataKey, IDictionary<string, object> data)
        => _data[dataKey] = data;
}
```

---

# 8. Reglas de Negocio Relacionadas

* RN-01: La librería define el contrato; el cliente provee la implementación.
* RN-02: `InMemoryDataProvider` es el default si el cliente no registra otro.
* RN-03: Cada conjunto de datos se identifica por una clave (string).
* RN-04: La fuente de datos (API, BD, archivo, hardcoded) es decisión del cliente.
* RN-05: El motor no depende de HTTP ni de infraestructura externa para obtener datos.
* RN-06: Los datos también pueden pasarse directamente vía `engine.Render()`.

---

# 9. Datos Utilizados

## Entrada

* DataKey (string)

## Salida

* IDictionary<string, object> (o null si no existe)

---

# 10. Criterios de Aceptación

## CA-01 Obtención exitosa

**Dado** datos registrados en el proveedor
**Cuando** el motor los solicita por clave
**Entonces** obtiene el diccionario correspondiente.

---

## CA-02 Datos inexistentes

**Dado** una clave no registrada
**Cuando** el motor la solicita
**Entonces** recibe `null`.

---

## CA-03 InMemoryDataProvider funcional

**Dado** el proveedor default
**Cuando** se registran datos con `Add()`
**Entonces** se obtienen correctamente con `GetData()`.

---

## CA-04 Datos directos en Render

**Dado** un diccionario de datos pasado directamente a `engine.Render()`
**Cuando** el motor procesa
**Entonces** utiliza los datos provistos sin necesidad de proveedor.

---

## CA-05 Implementación personalizada

**Dado** un cliente que registra su propia implementación de `IDataProvider`
**Cuando** el motor solicita datos
**Entonces** los obtiene de la implementación del cliente.

---

# 11. Cambios respecto a versión 1.0

* **Renombrado:** De "Descargar Datos desde API" a "Proveer Datos al Motor".
* **Modelo de proveedor:** Se reemplaza el modelo de descarga HTTP por el patrón de proveedor inyectable (`IDataProvider`).
* **Implementación default:** Se introduce `InMemoryDataProvider` como implementación por defecto.
* **Sin dependencia HTTP:** El motor no requiere conectividad ni HTTP client.
* **Datos directos:** Se mantiene la capacidad de pasar datos directamente en `engine.Render()`.
* **Responsabilidad del cliente:** La fuente de datos es responsabilidad exclusiva del sistema consumidor.

---

# 12. Notas

Este caso de uso redefine la relación entre el motor y los datos. En lugar de que el motor "descargue" datos, el motor "recibe" datos a través de un proveedor o parámetro directo. Esto mantiene la librería libre de dependencias de infraestructura.

---

**Fin del documento**
