# ~~Caso de Uso: Descargar Perfil de Impresora~~ — FUSIONADO

**Código:** CU-26
**Archivo:** CU-26-proveer-perfil-motor_v2.0.md
**Versión:** 2.0
**Estado:** ⚠️ FUSIONADO con CU-21 v2.0
**Fecha:** 2026-05-27
**Autor:** Equipo Funcional / Arquitectura

---

# AVISO — CASO DE USO FUSIONADO

> **Este caso de uso ha sido absorbido por CU-21 v2.0 (Cargar Perfil de Impresora).**

## Motivo de la fusión

En la versión 1.0, CU-26 describía la descarga de perfiles de impresora desde una API REST como un flujo independiente. Con la adopción del patrón de **proveedor inyectable** (`IDeviceProfileProvider`) en CU-21 v2.0, la descarga remota se convierte en una posible implementación del proveedor, no en un caso de uso separado.

## Cómo se cubre ahora

* **CU-21 v2.0** define el contrato `IDeviceProfileProvider`.
* La librería provee `InMemoryDeviceProfileProvider` como implementación default.
* Si el cliente necesita obtener perfiles desde una API REST, implementa `IDeviceProfileProvider` con su propia lógica HTTP:

  ```csharp
  public class ApiDeviceProfileProvider : IDeviceProfileProvider
  {
      private readonly HttpClient _httpClient;

      public ApiDeviceProfileProvider(HttpClient httpClient)
          => _httpClient = httpClient;

      public DeviceProfile? GetProfile(string name)
      {
          // Lógica HTTP del cliente
      }

      public IEnumerable<DeviceProfile> GetAll() { ... }
      // Nota: el contrato IDeviceProfileProvider solo exige GetProfile y GetAll.
      // Add(DeviceProfile) NO forma parte del contrato (es propio de
      // InMemoryDeviceProfileProvider), por lo que una implementación de API no lo requiere.
  }
  ```

* El registro en DI:

  ```csharp
  services.AddSingleton<IDeviceProfileProvider, ApiDeviceProfileProvider>();
  ```

## Referencia

Ver: **CU-21-cargar-perfil-impresora_v2.0.md**

---

# Contenido original v1.0 (conservado como referencia)

El contenido original de este caso de uso describía:

* Descarga de perfil desde API REST
* Construcción de petición HTTP con identificador y versión
* Validación de respuesta HTTP y JSON
* Almacenamiento en memoria/caché
* Manejo de errores: API no disponible, 404, JSON inválido, fallos de red

Todos estos escenarios quedan cubiertos por la implementación personalizada que el cliente realice de `IDeviceProfileProvider`.

---

**Fin del documento**
