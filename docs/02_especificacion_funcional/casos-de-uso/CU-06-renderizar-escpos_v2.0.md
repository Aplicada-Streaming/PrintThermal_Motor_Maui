# Caso de Uso: Renderizar Documento a ESC/POS

**Código:** CU-06
**Archivo:** CU-06-renderizar-escpos_v2.0.md
**Versión:** 2.0
**Estado:** Aprobado
**Fecha:** 2026-05-27
**Autor:** Equipo Funcional / Arquitectura

---

# 1. Propósito

Este caso de uso describe el proceso mediante el cual el motor DSL convierte la representación abstracta del documento en una secuencia de comandos compatibles con el estándar ESC/POS.

El objetivo es traducir el documento a un formato entendible por impresoras térmicas, respetando las capacidades y restricciones del dispositivo.

> **Novedad v2.0:** `RenderResult` expone métodos `ToHexString()` y `ToBase64()` para facilitar la transmisión del ticket por canales alternativos (email, API REST, almacenamiento) en caso de fallo de impresión o para respaldo.

---

# 2. Actores

## Actor Primario

**Motor DSL** — Responsable del proceso de renderizado.

## Actores Secundarios

**Renderizador ESC/POS** — Traduce la representación abstracta a comandos.
**Perfil de Dispositivo** — Define capacidades de impresión.
**Sistema consumidor** — Recibe `RenderResult` y decide cómo utilizar la salida.

---

# 3. Precondiciones

* Existe una representación abstracta del documento (CU-05).
* Existe un perfil de impresora válido.
* El motor DSL se encuentra inicializado.

---

# 4. Postcondiciones

## En caso de éxito

* Se genera una secuencia de comandos ESC/POS válida.
* El documento queda listo para ser enviado a la impresora.
* Se respetan las capacidades del dispositivo.
* `RenderResult.Output` contiene `byte[]` con los comandos ESC/POS.
* `RenderResult.ToHexString()` retorna la representación hexadecimal.
* `RenderResult.ToBase64()` retorna la representación en Base64.

## En caso de fallo

* No se genera la salida ESC/POS.
* El sistema informa el error correspondiente.
* Se registra el evento para diagnóstico.

---

# 5. Flujo Principal

1. El sistema inicia el proceso de renderizado.
2. El motor DSL recibe:

   * DocumentoRepresentacionAbstracta
   * PerfilDispositivo
3. El renderizador ESC/POS recorre la estructura del documento.
4. Para cada elemento, el sistema:

   * identifica el tipo (texto, imagen, QR, tabla, etc.)
   * traduce a comandos ESC/POS correspondientes
5. El sistema aplica configuraciones:

   * alineación
   * tamaño de fuente
   * estilos
6. Para contenido especial:

   * QR → comandos de generación QR (`GS ( k`)
   * imágenes → conversión a bitmap compatible
   * tablas → formato columnar con separadores
7. Se agregan comandos de control:

   * inicialización (`ESC @`)
   * salto de línea (`LF`)
   * corte de papel (`GS V`)
8. Se construye la secuencia final de bytes ESC/POS.
9. Se genera `RenderResult` con:

   * `Output` = `byte[]`
   * `Target` = `"escpos"`
   * sin errores → `IsSuccessful` es `true` (propiedad calculada: `true` cuando `Errors` está vacío)
10. El sistema consumidor puede:

    * Enviar `byte[]` a impresora BT (CU-10)
    * Obtener hex con `result.ToHexString()` para diagnóstico
    * Obtener Base64 con `result.ToBase64()` para transmisión por email/API
11. El caso de uso finaliza.

---

# 6. Flujos Alternativos

## FA-01 Elemento no soportado

**En el paso 4**

* Si el dispositivo no soporta el elemento:

  * El sistema omite o degrada el contenido.
  * Registra advertencia.

---

## FA-02 Error en conversión de imagen

**En el paso 6**

* Si la imagen no puede procesarse:

  * El sistema informa error.
  * Puede omitir la imagen.

---

## FA-03 Perfil inválido

**En el paso 2**

* Si el perfil no es válido:

  * El sistema detiene el proceso.
  * Informa el error.

---

## FA-04 Fallo de impresión → transmisión alternativa

**En el paso 10**

* Si la impresión Bluetooth falla:

  * El cliente puede usar `result.ToBase64()` para enviar el ticket por email.
  * El cliente puede usar `result.ToHexString()` para almacenar y reintentar.
  * Los bytes ESC/POS se preservan para reenvío posterior.

---

## FA-05 Error técnico

**En cualquier punto**

* El sistema informa error interno.
* Registra el evento.
* El caso de uso finaliza.

---

# 7. RenderResult Helpers (v2.0)

```csharp
public class RenderResult
{
    public string Target { get; set; }
    public object? Output { get; set; }
    public List<string> Warnings { get; set; }
    public List<string> Errors { get; set; }

    // IsSuccessful es una propiedad calculada de SOLO LECTURA:
    // es true cuando no hay errores (Errors vacío). No es settable.
    public bool IsSuccessful => Errors.Count == 0;

    /// <summary>
    /// Convierte Output (byte[]) a representación hexadecimal.
    /// Útil para diagnóstico, logging y visualización en UI.
    /// Ejemplo: "1B 40 48 6F 6C 61 0A 1D 56 00"
    /// </summary>
    public string? ToHexString()
    {
        if (Output is byte[] bytes)
            return BitConverter.ToString(bytes).Replace("-", " ");
        return null;
    }

    /// <summary>
    /// Convierte Output (byte[]) a representación Base64.
    /// Útil para transmisión por email, API REST o almacenamiento.
    /// </summary>
    public string? ToBase64()
    {
        if (Output is byte[] bytes)
            return Convert.ToBase64String(bytes);
        return null;
    }
}
```

**Casos de uso de los helpers:**

| Método | Caso de uso |
|--------|-------------|
| `ToHexString()` | Debug, logging, hex dump en UI, diagnóstico de bytes |
| `ToBase64()` | Envío por email, transmisión vía API REST, almacenamiento en BD, reintento diferido |

---

# 8. Reglas de Negocio Relacionadas

* RN-01: La salida debe ser compatible con ESC/POS.
* RN-02: Deben respetarse las capacidades del dispositivo.
* RN-03: Los elementos deben mapearse a comandos válidos.
* RN-04: El `RenderResult` debe proveer métodos de conversión para canales alternativos.
* RN-05: `ToHexString()` y `ToBase64()` retornan `null` si `Output` no es `byte[]`.

---

# 9. Datos Utilizados

## Entrada

* DocumentoRepresentacionAbstracta
* PerfilDispositivo

## Salida

* RenderResult con:
  * `Output` → `byte[]` (secuencia ESC/POS)
  * `ToHexString()` → `string` (representación hex)
  * `ToBase64()` → `string` (representación Base64)

---

# 10. Criterios de Aceptación

## CA-01 Renderizado exitoso

**Dado** un documento válido
**Cuando** se renderiza
**Entonces** se genera una secuencia ESC/POS válida.

---

## CA-02 Compatibilidad con dispositivo

**Dado** un perfil específico
**Cuando** se procesa
**Entonces** la salida respeta sus capacidades.

---

## CA-03 Manejo de elementos especiales

**Dado** contenido como QR o imágenes
**Cuando** se procesa
**Entonces** se generan los comandos correspondientes.

---

## CA-04 ToHexString funcional

**Dado** un `RenderResult` exitoso con `Output` tipo `byte[]`
**Cuando** se invoca `ToHexString()`
**Entonces** retorna una cadena hexadecimal separada por espacios.

---

## CA-05 ToBase64 funcional

**Dado** un `RenderResult` exitoso con `Output` tipo `byte[]`
**Cuando** se invoca `ToBase64()`
**Entonces** retorna una cadena Base64 válida que puede decodificarse al `byte[]` original.

---

## CA-06 Helpers con Output no-byte[]

**Dado** un `RenderResult` cuyo `Output` no es `byte[]` (por ejemplo, `string` del TextRenderer)
**Cuando** se invoca `ToHexString()` o `ToBase64()`
**Entonces** retorna `null`.

---

## CA-07 Manejo de errores

**Dado** un problema en el renderizado
**Cuando** ocurre
**Entonces** el sistema informa el error.

---

# 11. Cambios respecto a versión 1.0

* **RenderResult helpers:** Se agregan `ToHexString()` y `ToBase64()` a `RenderResult`.
* **Flujo alternativo FA-04:** Se documenta el escenario de transmisión alternativa ante fallo de impresión.
* **Actor adicional:** Se agrega "Sistema consumidor" como actor secundario.
* **Postcondiciones ampliadas:** Se documentan los métodos de conversión disponibles.
* **Criterios de aceptación:** Se agregan CA-04, CA-05 y CA-06 para validar los helpers.

---

# 12. Notas

Los métodos `ToHexString()` y `ToBase64()` permiten que el ticket ESC/POS no se pierda si la impresión falla. El cliente puede almacenar la representación Base64 y reintentarla después, o enviarla por email/API para impresión remota. Esto mejora la resiliencia del sistema de impresión.

---

**Fin del documento**
