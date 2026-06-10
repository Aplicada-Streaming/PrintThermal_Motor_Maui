# Perfiles de Impresora — DeviceProfile y PrinterProfile

## 1. DeviceProfile

`DeviceProfile` define las capacidades y restricciones del dispositivo destino. El motor lo usa para ajustar el layout (anchos, alineación) y seleccionar el renderer correcto.

### Propiedades

| Propiedad | Tipo | Obligatoria | Descripción |
|-----------|------|-------------|-------------|
| `Name` | string | Sí | Identificador del perfil (ej. `"58mm"`, `"80mm"`, `"preview"`). |
| `Width` | int | Sí | Ancho disponible en caracteres. Determina el layout de todo el documento. |
| `RenderTarget` | string | Sí | Target de renderizado: `"text"`, `"escpos"`, o un target custom registrado. |
| `Capabilities` | Dictionary<string, object> | No | Capacidades del dispositivo como pares clave-valor. |

### Capabilities opcionales

| Clave | Tipo | Descripción |
|-------|------|-------------|
| `"supports_qrcode"` | bool | Si la impresora soporta QR nativo (`GS ( k`). |
| `"supports_barcode"` | bool | Si soporta códigos de barras nativos (ej. EAN-13 `GS k`). |
| `"supports_images"` | bool | Si soporta impresión de imágenes bitmap. |
| `"bitmap_max_width_px"` | int | Ancho máximo del bitmap en píxeles (default `384`). Lo leen `EscPosRenderer` y `RasterPreviewRenderer`. |

### Ejemplo de creación

```csharp
var profile = new DeviceProfile("58mm", 32, "escpos");

// Con capabilities
var profile80 = new DeviceProfile("80mm", 48, "escpos");
profile80.SetCapability("supports_qrcode", true);
profile80.SetCapability("supports_barcode", true);
profile80.SetCapability("supports_images", false);
```

### Constructor

```csharp
public DeviceProfile(string name, int width, string renderTarget)
```

Los tres parámetros son obligatorios. `Capabilities` se inicializa como diccionario vacío.

---

## 2. PrinterProfile

`PrinterProfile` controla los tiempos de comunicación con la impresora Bluetooth. Es un modelo de la app consumidora (no de la librería core) que define delays entre comandos para evitar pérdida de datos.

> **Nota:** `PrinterProfile` vive en la app consumidora (ej. `MotorDsl.SampleApp`), no en la librería core. Cada app puede implementar su propia versión según su estrategia de comunicación BT.

### Propiedades

| Propiedad | Tipo | Default | Para qué sirve |
|-----------|------|---------|----------------|
| `Name` | string | `"default"` | Identificador del perfil de timing. |
| `LineDelayMs` | int | `150` | Pausa base entre cada línea enviada. Da tiempo a la impresora para procesar. |
| `ByteDelayMs` | int | `5` | Pausa adicional por cada byte en la línea. Líneas largas → delay más largo. |
| `InitDelayMs` | int | `100` | Pausa antes de empezar a enviar datos. Permite que la impresora se prepare. |
| `FinalDelayMs` | int | `500` | Pausa después del último comando. Asegura que se complete la impresión. |
| `QrDelayMs` | int | `300` | Pausa después de enviar un QR. La impresora necesita tiempo para generarlo. |
| `ImageDelayMs` | int | `500` | Pausa después de enviar una imagen. El procesamiento de bitmap es lento. |
| `CutDelayMs` | int | `500` | Pausa después del comando de corte de papel. |
| `InitCommandDelayMs` | int | `300` | Pausa después del comando de inicialización (`ESC @`). |

### Cuándo ajustar los delays

| Síntoma | Causa probable | Ajuste |
|---------|---------------|--------|
| Se cortan líneas o faltan datos | Delay entre líneas muy bajo | Subir `LineDelayMs` y `ByteDelayMs` |
| QR no se imprime o sale corrupto | Poco tiempo para generar QR | Subir `QrDelayMs` a 400-500 |
| El ticket sale incompleto al final | No se espera a que termine | Subir `FinalDelayMs` a 700-1000 |
| La impresora no responde al inicio | Inicialización incompleta | Subir `InitDelayMs` y `InitCommandDelayMs` |
| Impresión muy lenta | Delays excesivos | Usar `PrinterProfile.Fast` o reducir valores |
| Imágenes corruptas | Bitmap requiere más procesamiento | Subir `ImageDelayMs` a 700-1000 |

---

## 3. Perfiles predefinidos

### Perfil real: 58HB6-6101 (basado en self-test)

| Propiedad              | Valor                                                        |
|------------------------|--------------------------------------------------------------|
| Modelo                 | 58HB6-6101                                                  |
| Ancho papel            | 58mm                                                         |
| Chars por línea        | 32                                                           |
| Codepage               | PC437 (IBM USA Std. Europe)                                  |
| ESC t command          | 0x1B 0x74 0x00                                               |
| CMD Type               | ESC/POS                                                      |
| Baudrate BT            | 115200                                                       |
| BT PIN                 | 0000                                                         |
| Barcodes soportados    | UPC-A, UPC-E, EAN13, EAN8, CODE39, CODEBAR, ITF, CODE93, CODE128, QR |
| Temperatura impresión  | 35°C                                                         |
| Print depth            | 38 level                                                     |
| Quality level          | 4 level                                                      |
| BT Multi-connection    | No soportado                                                 |

#### Ejemplo de código C# para crear el perfil 58HB6

```csharp
var profile58HB6 = new DeviceProfile("58HB6", 32, "escpos")
{
    CodePage = 437,
    CodePageCommand = new byte[] { 0x1B, 0x74, 0x00 },
    BaudRate = 115200,
    SupportedBarcodes = new List<string> 
    { 
        "UPC-A", "UPC-E", "EAN13", "EAN8", 
        "CODE39", "CODEBAR", "ITF", "CODE93", "CODE128", "QR" 
    }
};
profile58HB6.SetCapability("supports_qrcode", true);
profile58HB6.SetCapability("supports_barcode", true);
profile58HB6.SetCapability("supports_images", false);
```

#### Cómo configurar el codepage correcto antes de imprimir

Al inicializar la impresora, enviar el comando ESC t correspondiente:

```csharp
// Seleccionar codepage PC437
buffer.AddRange(new byte[] { 0x1B, 0x74, 0x00 });
```

#### Tabla de caracteres españoles en PC437

| Carácter | Hex  |
|----------|------|
| á        | 0xA0 |
| é        | 0x82 |
| í        | 0xA1 |
| ó        | 0xA2 |
| ú        | 0xA3 |
| ñ        | 0xA4 |
| Ñ        | 0xA5 |

`PrinterProfile` incluye tres perfiles estáticos listos para usar:

### Tabla comparativa

| Propiedad | `Thermal58mm` | `Thermal80mm` | `Fast` |
|-----------|:-------------:|:-------------:|:------:|
| `Name` | `"thermal_58mm"` | `"thermal_80mm"` | `"fast"` |
| `LineDelayMs` | 150 | 100 | 50 |
| `ByteDelayMs` | 5 | 3 | 1 |
| `InitDelayMs` | 100 | 50 | 50 |
| `FinalDelayMs` | 500 | 300 | 200 |
| `QrDelayMs` | 300 | 300 | 300 |
| `ImageDelayMs` | 500 | 500 | 500 |
| `CutDelayMs` | 500 | 500 | 500 |
| `InitCommandDelayMs` | 300 | 300 | 300 |

### Uso

```csharp
var printerProfile = PrinterProfile.Thermal58mm;  // 58mm, delays conservadores
var printerProfile = PrinterProfile.Thermal80mm;  // 80mm, más rápida
var printerProfile = PrinterProfile.Fast;          // Delays mínimos
```

### Cuándo usar cada uno

- **`Thermal58mm`** — Impresoras económicas de 58mm. Delays conservadores que funcionan con la mayoría de dispositivos.
- **`Thermal80mm`** — Impresoras de 80mm que suelen ser más rápidas. Delays reducidos.
- **`Fast`** — Para impresoras rápidas o cuando se prefiere velocidad sobre seguridad. Si se pierden datos, cambiar a un perfil más conservador.

---

## 4. Tabla de anchos típicos por modelo

| Modelo | Ancho papel | Chars aprox | Width recomendado | Uso típico |
|--------|-------------|-------------|-------------------|------------|
| 58mm | 58mm | 32 chars | `32` | Tickets de venta, recibos pequeños, comprobantes portátiles |
| 80mm | 80mm | 48 chars | `48` | Facturas, tickets con tablas, documentos con más detalle |
| Custom | Variable | Variable | Medir en físico | Impresoras especiales, labels, formatos no estándar |

> La relación ancho-caracteres depende de la fuente de la impresora. Los valores de 32 y 48 corresponden a la fuente monoespaciada estándar (Font A) de la mayoría de impresoras térmicas compatibles con ESC/POS.

---

## 5. Cómo medir el ancho real de tu impresora

Si no estás seguro del ancho exacto de tu impresora, podés medirlo con un test simple:

### Paso 1 — Crear un template de medición

```json
{
  "id": "test-ancho",
  "version": "1.0",
  "root": {
    "type": "container",
    "layout": "vertical",
    "children": [
      { "type": "text", "text": "1234567890123456789012345678901234567890" },
      { "type": "text", "text": "----+----1----+----2----+----3----+----4----+----5" }
    ]
  }
}
```

### Paso 2 — Renderizar con EscPosRenderer

```csharp
var profile = new DeviceProfile("test", 50, "escpos");
var result = engine.Render(testTemplate, new Dictionary<string, object>(), profile);
// Enviar result.Output (byte[]) a la impresora
```

### Paso 3 — Leer el resultado impreso

- Contar cuántos caracteres entran antes de que la impresora haga un salto de línea automático.
- Ese número es tu `Width` real.
- Ejemplo: si se corta en el carácter 32, tu Width es 32.

### Paso 4 — Usar ese Width en tu DeviceProfile

```csharp
var profile = new DeviceProfile("mi-impresora", 32, "escpos");
```

---

## 6. Cómo afecta Width al layout

El `Width` del `DeviceProfile` es el parámetro más importante del layout. Controla cómo se distribuye el texto en cada línea.

### Efectos

| Aspecto | Cómo lo afecta Width |
|---------|----------------------|
| **Texto largo** | Si el texto excede `Width`, se produce wrapping automático (la impresora corta la línea). |
| **Alineación center** | Se calcula como `(Width - largoTexto) / 2` espacios a la izquierda. Si Width es incorrecto, el centrado queda desplazado. |
| **Alineación right** | Se rellena con `Width - largoTexto` espacios a la izquierda. |
| **Separadores** | Deben tener exactamente `Width` caracteres para ocupar toda la línea. |
| **Tablas** | El ancho de columnas se distribuye proporcionalmente dentro de `Width`. |
| **Containers horizontales** | Los hijos se reparten el `Width` disponible. |

### Ejemplo — mismo texto, diferente Width

**Width = 32 (58mm):**

```
        MI NEGOCIO
--------------------------------
Café              2    $300.00
Medialunas        6    $300.00
--------------------------------
              TOTAL:   $600.00
```

**Width = 48 (80mm):**

```
                   MI NEGOCIO
------------------------------------------------
Café                          2          $300.00
Medialunas                    6          $300.00
------------------------------------------------
                              TOTAL:     $600.00
```

---

## 7. Cómo crear un perfil custom

### DeviceProfile custom

```csharp
// Impresora especial de 72mm con soporte QR
var customProfile = new DeviceProfile("72mm-qr", 42, "escpos");
customProfile.SetCapability("supports_qrcode", true);
customProfile.SetCapability("supports_barcode", true);
customProfile.SetCapability("supports_images", true);
```

### Registrarlo con fluent API

```csharp
services.AddMotorDslEngine()
    .AddProfiles(profiles =>
    {
        // Perfiles estándar
        profiles.Add(new DeviceProfile("58mm", 32, "escpos"));
        profiles.Add(new DeviceProfile("80mm", 48, "escpos"));

        // Perfil custom
        var custom = new DeviceProfile("72mm-qr", 42, "escpos");
        custom.SetCapability("supports_qrcode", true);
        profiles.Add(custom);

        // Perfil para preview en texto
        profiles.Add(new DeviceProfile("preview", 48, "text"));
    });
```

### Obtenerlo después via provider

```csharp
var profileProvider = serviceProvider.GetRequiredService<IDeviceProfileProvider>();
var profile = profileProvider.GetProfile("72mm-qr");
// profile es null si no fue registrado
```

### PrinterProfile custom

```csharp
var customPrinter = new PrinterProfile
{
    Name = "mi-impresora-lenta",
    LineDelayMs = 200,
    ByteDelayMs = 8,
    InitDelayMs = 150,
    FinalDelayMs = 800,
    QrDelayMs = 500,
    ImageDelayMs = 700,
    CutDelayMs = 600,
    InitCommandDelayMs = 400
};
```

---

## 8. RenderTarget values

La propiedad `RenderTarget` del `DeviceProfile` determina qué renderer se usa.

| Valor | Renderer | Output | Uso |
|-------|----------|--------|-----|
| `"text"` | `TextRenderer` | `string` | Preview, debug, tests, logs. No tiene formateo visual de bold. |
| `"escpos"` | `EscPosRenderer` | `byte[]` | Impresión térmica. Genera comandos binarios ESC/POS con estilos nativos. |
| `"html"` | (custom) | `string` | Si registrás un `HtmlRenderer` propio con `Target => "html"`. |
| `"pdf"` | `PdfRenderer` (MotorDsl.Maui) | `byte[]` | Provisto por `MotorDsl.Maui` (PdfSharpCore), registrado por `AddMotorDslMaui()`. Custom solo si no usás ese paquete. |
| Cualquier otro | (custom) | Variable | Lo que retorne tu `IRenderer` registrado con ese `Target`. |

### Cómo funciona la selección

```
profile.RenderTarget = "escpos"
    ↓
RendererRegistry.GetRenderer("escpos")
    ↓
Busca entre todos los IRenderer registrados
    ↓
Retorna el que tenga Target == "escpos"
    ↓
EscPosRenderer.Render(document, profile) → byte[]
```

Si no existe un renderer para el target solicitado, el `RendererRegistry` hace fallback al renderer `"text"` — pero solo si ese renderer está registrado. Si tampoco existe el target `"text"`, `GetRenderer` devuelve `null` y `DocumentEngine` lanza `InvalidOperationException` (`"No renderer found for target: {target}"`).
