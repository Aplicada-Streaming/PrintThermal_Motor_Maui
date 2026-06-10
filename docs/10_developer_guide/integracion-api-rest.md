# Integración con API REST

> Guía para persistir, recuperar y distribuir tickets generados por MotorDsl.

## Contexto

`RenderResult` expone dos helpers para serializar la salida binaria (`byte[]`) a formatos transportables por JSON:

| Método         | Retorno   | Uso típico                              |
|----------------|-----------|-----------------------------------------|
| `ToBase64()`   | `string?` | Guardar en DB, enviar por REST, email   |
| `ToHexString()`| `string?` | Debug, logs, visualización hex dump     |

Ambos retornan `null` si `Output` no es `byte[]` (por ejemplo, cuando el renderer es `text`).

---

## 1. Guardar un ticket en base de datos vía REST

### Flujo

```
App → MotorDsl Engine → RenderResult (byte[])
                            │
                            ├── .ToBase64()  → string
                            │
                            └── POST /api/tickets  → DB
```

### Ejemplo C\#

```csharp
// 1. Renderizar el ticket
var result = engine.Render(templateDsl, data, profile);

if (!result.IsSuccessful)
{
    // Manejar errores de validación
    foreach (var error in result.Errors)
        Console.WriteLine($"Error: {error}");
    return;
}

// 2. Serializar a Base64
var base64 = result.ToBase64();

// 3. Armar el payload
var payload = new
{
    ticketId    = Guid.NewGuid().ToString(),
    timestamp   = DateTime.UtcNow,
    target      = result.Target,       // "escpos", "pdf"
    contentB64  = base64,
    warnings    = result.Warnings
};

// 4. Enviar al backend
using var httpClient = new HttpClient();
var response = await httpClient.PostAsJsonAsync(
    "https://api.ejemplo.com/api/tickets", payload);

response.EnsureSuccessStatusCode();
```

### Modelo sugerido para el backend

```json
{
  "ticketId":   "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp":  "2026-03-30T14:30:00Z",
  "target":     "escpos",
  "contentB64": "G0AbaQEASG9sYSBNdW5kbw==",
  "warnings":   []
}
```

---

## 2. Recuperar y reimprimir un ticket

```csharp
// 1. Obtener el ticket guardado
var ticket = await httpClient.GetFromJsonAsync<TicketDto>(
    $"https://api.ejemplo.com/api/tickets/{ticketId}");

// 2. Decodificar el contenido
byte[] rawBytes = Convert.FromBase64String(ticket.ContentB64);

// 3. Enviar directamente a la impresora (ya son comandos ESC/POS)
await printerService.SendBytesAsync(rawBytes);
```

> **Nota:** Los bytes guardados ya contienen los comandos ESC/POS completos. No es necesario volver a pasar por el motor de renderizado.

---

## 3. Enviar PDF por email

Cuando el target es `pdf`, el `Output` ya contiene el PDF como `byte[]`:

```csharp
// Renderizar como PDF
var pdfProfile = new DeviceProfile("a4-pdf", 80, "pdf");
var result = engine.Render(templateDsl, data, pdfProfile);

if (!result.IsSuccessful) return;

// Opción A: Guardar en Base64 para API de email
var pdfBase64 = result.ToBase64();
var emailPayload = new
{
    to         = "cliente@ejemplo.com",
    subject    = "Su ticket de multa",
    attachment = new
    {
        filename    = "multa.pdf",
        contentType = "application/pdf",
        contentB64  = pdfBase64
    }
};
await httpClient.PostAsJsonAsync(
    "https://api.ejemplo.com/api/email/send", emailPayload);

// Opción B: Guardar como archivo temporal
var pdfBytes = (byte[])result.Output!;
var tempPath = Path.Combine(
    FileSystem.CacheDirectory, "multa.pdf");
await File.WriteAllBytesAsync(tempPath, pdfBytes);
```

---

## 4. Patrón de integración completo

```
┌─────────────────────────────────────────────────────┐
│                    App MAUI                          │
│                                                      │
│  template DSL + datos + DeviceProfile                │
│         │                                            │
│         ▼                                            │
│  ┌─────────────┐                                     │
│  │ IDocumentEngine.Render()                          │
│  └──────┬──────┘                                     │
│         │ RenderResult                               │
│         ├──────────────┐                             │
│         │              │                             │
│    .ToBase64()    .ToHexString()                     │
│         │              │                             │
│         ▼              ▼                             │
│   POST /api/tickets   Hex dump (debug)               │
│         │                                            │
│         ▼                                            │
│   GET /api/tickets/{id}                              │
│         │                                            │
│         ├──► Reimprimir (SendBytesAsync)             │
│         └──► Enviar PDF por email                    │
└─────────────────────────────────────────────────────┘
```

---

## Advertencias

- `ToBase64()` y `ToHexString()` solo funcionan cuando `Output` es `byte[]`. Con el renderer `text` retornan `null`.
- Para reimprimir, enviar los bytes directamente a la impresora — no re-renderizar.
- El tamaño del Base64 es ~33% mayor que el binario original. Para tickets ESC/POS típicos (<10 KB) esto no es un problema.
- Validar siempre `result.IsSuccessful` antes de intentar serializar.
