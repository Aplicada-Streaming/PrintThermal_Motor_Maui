# Caso de Uso: Proveer Plantilla al Motor

**Código:** CU-24
**Archivo:** CU-24-proveer-plantilla-motor_v2.0.md
**Versión:** 2.0
**Estado:** Aprobado
**Fecha:** 2026-05-27
**Autor:** Equipo Funcional / Arquitectura

> **Renombrado en v2.0:** Anteriormente "Descargar Plantilla desde API" (CU-24 v1.0). El concepto se generalizó a "Proveer Plantilla al Motor" para reflejar que la fuente de la plantilla es responsabilidad del cliente.

---

# 1. Propósito

Este caso de uso describe el mecanismo mediante el cual la librería del motor DSL obtiene plantillas DSL a través de un proveedor configurable (`ITemplateProvider`).

El objetivo es desacoplar la fuente de plantillas del motor, permitiendo que el sistema cliente decida cómo y desde dónde se obtienen (memoria, archivo local, API REST, base de datos, etc.).

---

# 2. Actores

## Actor Primario

**Sistema consumidor** — Registra e inyecta su implementación de `ITemplateProvider`.

## Actores Secundarios

**Librería Motor DSL** — Define el contrato `ITemplateProvider` y provee `InMemoryTemplateProvider` como implementación default.
**Motor DSL** — Consume plantillas a través del proveedor registrado.

---

# 3. Precondiciones

* El motor DSL se encuentra inicializado vía `AddMotorDslEngine()`.
* El cliente ha registrado una implementación de `ITemplateProvider` (o utiliza el default `InMemoryTemplateProvider`).
* Al menos una plantilla ha sido registrada en el proveedor.

---

# 4. Postcondiciones

## En caso de éxito

* El motor obtiene la plantilla DSL solicitada a través del proveedor.
* La plantilla queda disponible para carga y validación (CU-13, CU-14).
* El proveedor puede gestionar múltiples plantillas por ID.

## En caso de fallo

* El proveedor no encuentra la plantilla solicitada.
* El motor recibe `null` o una excepción controlada.
* Se informa el error al sistema cliente.

---

# 5. Flujo Principal

1. El sistema cliente registra plantillas durante la configuración:

   ```csharp
   services.AddMotorDslEngine()
       .AddTemplates(templates =>
       {
           templates.Add("ticket-venta", dslJsonString);
           templates.Add("recibo", otroDslJsonString);
       });
   ```

2. El motor DSL solicita una plantilla por ID al `ITemplateProvider`.
3. El proveedor busca la plantilla en su almacenamiento interno.
4. El proveedor retorna el string DSL encontrado.
5. El motor procesa la plantilla (parse, evaluación, render).
6. El caso de uso finaliza.

---

# 6. Flujos Alternativos

## FA-01 Plantilla no encontrada

**En el paso 3**

* Si la plantilla no existe en el proveedor:

  * El proveedor retorna `null`.
  * El motor informa al cliente que la plantilla no fue encontrada.

---

## FA-02 Proveedor no registrado

**En la configuración**

* Si el cliente no registra ningún `ITemplateProvider`:

  * El motor utiliza `InMemoryTemplateProvider` (vacío por defecto).
  * El cliente debe agregar plantillas o registrar su propia implementación.

---

## FA-03 Implementación personalizada (API, archivo, BD)

**En el paso 1**

* El cliente puede inyectar cualquier implementación:

  ```csharp
  services.AddSingleton<ITemplateProvider, ApiTemplateProvider>();
  ```

* La implementación maneja conectividad, cache, reintentos, versionado.
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
public interface ITemplateProvider
{
    string? GetTemplate(string templateId);
    IEnumerable<string> GetAvailableTemplateIds();
}
```

> **Nota:** `Add(string, string)` **no** forma parte del contrato `ITemplateProvider`; es un método de la implementación `InMemoryTemplateProvider` (ver abajo).

**Implementación default:**

```csharp
public class InMemoryTemplateProvider : ITemplateProvider
{
    private readonly Dictionary<string, string> _templates = new();

    public string? GetTemplate(string templateId)
        => _templates.TryGetValue(templateId, out var t) ? t : null;

    public IEnumerable<string> GetAvailableTemplateIds() => _templates.Keys;

    public void Add(string templateId, string dslContent)
        => _templates[templateId] = dslContent;
}
```

---

# 8. Reglas de Negocio Relacionadas

* RN-01: La librería define el contrato; el cliente provee la implementación.
* RN-02: `InMemoryTemplateProvider` es el default si el cliente no registra otro.
* RN-03: Cada plantilla se identifica por un ID único (string).
* RN-04: La fuente de la plantilla (archivo, API, BD, hardcoded) es decisión del cliente.
* RN-05: El motor no depende de HTTP ni de infraestructura externa para obtener plantillas.

---

# 9. Datos Utilizados

## Entrada

* TemplateId (string)

## Salida

* PlantillaDSL (string, o null si no existe)

---

# 10. Criterios de Aceptación

## CA-01 Obtención exitosa

**Dado** una plantilla registrada en el proveedor
**Cuando** el motor la solicita por ID
**Entonces** obtiene el string DSL correspondiente.

---

## CA-02 Plantilla inexistente

**Dado** un ID no registrado
**Cuando** el motor lo solicita
**Entonces** recibe `null`.

---

## CA-03 InMemoryTemplateProvider funcional

**Dado** el proveedor default
**Cuando** se registran plantillas con `Add()`
**Entonces** se obtienen correctamente con `GetTemplate()` y `GetAvailableTemplateIds()`.

---

## CA-04 Implementación personalizada

**Dado** un cliente que registra su propia implementación de `ITemplateProvider`
**Cuando** el motor solicita plantillas
**Entonces** las obtiene de la implementación del cliente.

---

## CA-05 Registro vía fluent API

**Dado** la configuración con `AddMotorDslEngine().AddTemplates()`
**Cuando** se resuelve el service provider
**Entonces** `ITemplateProvider` contiene las plantillas registradas.

---

# 11. Cambios respecto a versión 1.0

* **Renombrado:** De "Descargar Plantilla desde API" a "Proveer Plantilla al Motor".
* **Modelo de proveedor:** Se reemplaza el modelo de descarga HTTP por el patrón de proveedor inyectable (`ITemplateProvider`).
* **Implementación default:** Se introduce `InMemoryTemplateProvider` como implementación por defecto.
* **Fluent API:** Se agrega `AddTemplates()` como extensión de `AddMotorDslEngine()`.
* **Sin dependencia HTTP:** El motor no requiere conectividad ni HTTP client. Si el cliente necesita descargar plantillas desde una API, implementa `ITemplateProvider` con su propia lógica HTTP.
* **Responsabilidad del cliente:** La fuente de datos es responsabilidad exclusiva del sistema consumidor.

---

# 12. Notas

Este caso de uso redefine la relación entre el motor y las plantillas. En lugar de que el motor "descargue" plantillas, el motor "recibe" plantillas a través de un proveedor que el cliente configura. Esto mantiene la librería libre de dependencias de infraestructura y permite cualquier estrategia de obtención.

---

**Fin del documento**
