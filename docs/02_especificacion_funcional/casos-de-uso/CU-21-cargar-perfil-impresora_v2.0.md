# Caso de Uso: Cargar Perfil de Impresora

**Código:** CU-21
**Archivo:** CU-21-cargar-perfil-impresora_v2.0.md
**Versión:** 2.0
**Estado:** Aprobado
**Fecha:** 2026-05-27
**Autor:** Equipo Funcional / Arquitectura

---

# 1. Propósito

Este caso de uso describe el mecanismo mediante el cual la librería del motor DSL obtiene perfiles de impresora a través de un proveedor configurable (`IDeviceProfileProvider`).

El objetivo es desacoplar completamente la fuente del perfil del motor, permitiendo que el sistema cliente decida cómo y desde dónde se obtienen los perfiles (archivo local, API REST, base de datos, hardcodeados en memoria, etc.).

> **Nota v2.0:** Este CU absorbe el antiguo CU-26 (Descargar Perfil de Impresora desde API), ya que la descarga remota es ahora una implementación posible del proveedor, no un caso de uso separado.

---

# 2. Actores

## Actor Primario

**Sistema consumidor** — Registra e inyecta su implementación de `IDeviceProfileProvider`.

## Actores Secundarios

**Librería Motor DSL** — Define el contrato `IDeviceProfileProvider` y provee `InMemoryDeviceProfileProvider` como implementación default.
**Motor DSL** — Consume perfiles a través del proveedor registrado.

---

# 3. Precondiciones

* El motor DSL se encuentra inicializado vía `AddMotorDslEngine()`.
* El cliente ha registrado una implementación de `IDeviceProfileProvider` (o utiliza el default `InMemoryDeviceProfileProvider`).
* Al menos un perfil de dispositivo ha sido registrado en el proveedor.

---

# 4. Postcondiciones

## En caso de éxito

* El motor obtiene el `DeviceProfile` solicitado a través del proveedor.
* El perfil queda disponible para adaptación (CU-12) y renderizado (CU-06, CU-07).
* El proveedor puede gestionar múltiples perfiles por nombre.

## En caso de fallo

* El proveedor no encuentra el perfil solicitado.
* El motor recibe `null` o una excepción controlada.
* Se informa el error al sistema cliente.

---

# 5. Flujo Principal

1. El sistema cliente registra una implementación de `IDeviceProfileProvider` durante la configuración:

   ```csharp
   services.AddMotorDslEngine()
       .AddProfiles(profiles =>
       {
           profiles.Add(new DeviceProfile("58mm", 32, "escpos"));
           profiles.Add(new DeviceProfile("80mm", 48, "escpos"));
       });
   ```

2. El motor DSL solicita un perfil por nombre al `IDeviceProfileProvider`.
3. El proveedor busca el perfil en su almacenamiento interno.
4. El proveedor retorna el `DeviceProfile` encontrado.
5. El motor utiliza el perfil para layout y renderizado.
6. El caso de uso finaliza.

---

# 6. Flujos Alternativos

## FA-01 Perfil no encontrado

**En el paso 3**

* Si el perfil no existe en el proveedor:

  * El proveedor retorna `null`.
  * El motor informa al cliente que el perfil no fue encontrado.

---

## FA-02 Proveedor no registrado

**En la configuración**

* Si el cliente no registra ningún `IDeviceProfileProvider`:

  * El motor utiliza `InMemoryDeviceProfileProvider` (vacío por defecto).
  * El cliente debe agregar perfiles o registrar su propia implementación.

---

## FA-03 Implementación personalizada (API, archivo, BD)

**En el paso 1**

* El cliente puede inyectar cualquier implementación:

  ```csharp
  services.AddSingleton<IDeviceProfileProvider, ApiDeviceProfileProvider>();
  ```

* La implementación es responsabilidad exclusiva del cliente.
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
public interface IDeviceProfileProvider
{
    DeviceProfile? GetProfile(string name);
    IEnumerable<DeviceProfile> GetAll();
}
```

> **Nota:** `Add(DeviceProfile)` **no** forma parte del contrato `IDeviceProfileProvider`; es un método de la implementación `InMemoryDeviceProfileProvider` (ver abajo).

**Implementación default:**

```csharp
public class InMemoryDeviceProfileProvider : IDeviceProfileProvider
{
    private readonly Dictionary<string, DeviceProfile> _profiles = new();

    public DeviceProfile? GetProfile(string name)
        => _profiles.TryGetValue(name, out var p) ? p : null;

    public IEnumerable<DeviceProfile> GetAll() => _profiles.Values;

    public void Add(DeviceProfile profile)
        => _profiles[profile.Name] = profile;
}
```

---

# 8. Reglas de Negocio Relacionadas

* RN-01: La librería define el contrato; el cliente provee la implementación.
* RN-02: `InMemoryDeviceProfileProvider` es el default si el cliente no registra otro.
* RN-03: Cada perfil se identifica por nombre (`DeviceProfile.Name`).
* RN-04: La fuente del perfil (archivo, API, BD, hardcoded) es decisión del cliente.
* RN-05: El motor no depende de infraestructura externa para obtener perfiles.

---

# 9. Datos Utilizados

## Entrada

* NombrePerfil (string)

## Salida

* DeviceProfile (o null si no existe)

---

# 10. Criterios de Aceptación

## CA-01 Obtención exitosa

**Dado** un perfil registrado en el proveedor
**Cuando** el motor lo solicita por nombre
**Entonces** obtiene el `DeviceProfile` correspondiente.

---

## CA-02 Perfil inexistente

**Dado** un nombre no registrado
**Cuando** el motor lo solicita
**Entonces** recibe `null`.

---

## CA-03 InMemoryDeviceProfileProvider funcional

**Dado** el proveedor default
**Cuando** se registran perfiles con `Add()`
**Entonces** se obtienen correctamente con `GetProfile()` y `GetAll()`.

---

## CA-04 Implementación personalizada

**Dado** un cliente que registra su propia implementación de `IDeviceProfileProvider`
**Cuando** el motor solicita perfiles
**Entonces** los obtiene de la implementación del cliente.

---

## CA-05 Registro vía fluent API

**Dado** la configuración con `AddMotorDslEngine().AddProfiles()`
**Cuando** se resuelve el service provider
**Entonces** `IDeviceProfileProvider` contiene los perfiles registrados.

---

# 11. Cambios respecto a versión 1.0

* **Modelo de proveedor:** Se reemplaza el modelo de "carga desde fuente" por el patrón de proveedor inyectable (`IDeviceProfileProvider`).
* **Implementación default:** Se introduce `InMemoryDeviceProfileProvider` como implementación por defecto.
* **Fluent API:** Se agrega `AddProfiles()` como extensión de `AddMotorDslEngine()`.
* **Absorción CU-26:** Este CU ahora cubre el escenario de CU-26 (Descargar Perfil de Impresora desde API) como una posible implementación del proveedor.
* **Responsabilidad del cliente:** La fuente de datos es responsabilidad exclusiva del sistema consumidor.

---

# 12. Notas

Este caso de uso es clave para mantener la librería del motor desacoplada de infraestructura. Al definir `IDeviceProfileProvider` como contrato, cualquier sistema cliente puede decidir cómo proveer los perfiles sin que el motor conozca los detalles de implementación.

---

**Fin del documento**
