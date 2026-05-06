# Developer Guide — MotorDsl

Guía completa para desarrolladores que consumen la librería **MotorDsl** desde sus aplicaciones .NET.

---

## Contenido

| # | Documento | Descripción |
|---|-----------|-------------|
| 1 | [conceptos-fundamentales.md](conceptos-fundamentales.md) | Qué es MotorDsl, el pipeline de renderizado, conceptos clave (template, data, profile, renderer) y qué NO hace la librería. |
| 2 | [formato-dsl-templates.md](formato-dsl-templates.md) | Especificación completa del formato JSON DSL: tipos de nodos, binding de datos, estilos, separadores y reglas de validación. |
| 3 | [formato-perfiles-impresora.md](formato-perfiles-impresora.md) | DeviceProfile y PrinterProfile: propiedades, perfiles predefinidos, anchos típicos por modelo de impresora y cómo afectan al layout. |
| 4 | [guia-integracion-maui.md](guia-integracion-maui.md) | Paso a paso para integrar MotorDsl en una app .NET MAUI: registro DI, inyección, renderizado, envío Bluetooth y manejo de errores. |
| 5 | [componentes-ux-maui.md](componentes-ux-maui.md) | Referencia de los controles MAUI bindeables (`PrinterStatusBadge`, `PrinterPickerView`, `MauiRasterPreview`, `MauiDocumentPreview`): API, BindableProperties, eventos, XAML. |
| 6 | [transports-y-extensibilidad.md](transports-y-extensibilidad.md) | Cómo escribir un `IThermalPrinterTransport` propio (USB, WiFi, BLE) usando `BluetoothPrinterTransport` como referencia. |
| 7 | [render-pixelado-y-pdf.md](render-pixelado-y-pdf.md) | Renderers MAUI: `RasterPreviewRenderer`, `QrCodeRasterizer`, `PdfRenderer` (QR + barcode-text + word-wrap + page-break). |

---

## Ejemplos de templates

| Ejemplo | Archivo | Descripción |
|---------|---------|-------------|
| Ticket simple | [ejemplos-templates/ticket-simple.json](ejemplos-templates/ticket-simple.json) | Ticket de venta con items, totales y footer. |
| Ticket con QR | [ejemplos-templates/ticket-con-qr.json](ejemplos-templates/ticket-con-qr.json) | Ticket de venta con código QR de verificación al final. |
| Ticket con barcode | [ejemplos-templates/ticket-con-barcode.json](ejemplos-templates/ticket-con-barcode.json) | Comprobante con código de barras EAN13. |
| Acta de infracción | [ejemplos-templates/acta-infraccion.json](ejemplos-templates/acta-infraccion.json) | Acta de tránsito con datos del infractor, infracciones, QR de pagos y firma. |
| Factura simplificada | [ejemplos-templates/factura-simplificada.json](ejemplos-templates/factura-simplificada.json) | Factura con tabla de items, datos del cliente, condiciones de pago y QR electrónico. |

---

## Audiencia

Esta documentación está dirigida a **desarrolladores que consumen la librería** desde aplicaciones .NET (.NET MAUI, APIs, servicios backend, herramientas CLI).

No cubre la implementación interna del motor. Para eso, ver `docs/05_arquitectura_tecnica/`.

---

## Requisitos previos

- .NET 10 o superior
- Conocimiento básico de C# y JSON
- Familiaridad con inyección de dependencias (recomendado)

---

## Quick Start

```csharp
// 1. Registrar servicios
services.AddMotorDslEngine()
    .AddTemplates(t => t.Add("ticket", dslJson))
    .AddProfiles(p => p.Add(new DeviceProfile("58mm", 32, "escpos")));

// 2. Inyectar y usar
var result = engine.Render(dslJson, data, profile);

// 3. Resultado
if (result.IsSuccessful)
{
    byte[] escposBytes = (byte[])result.Output!;
    // Enviar a impresora BT, guardar, o transmitir
    string hex = result.ToHexString()!;     // diagnóstico
    string b64 = result.ToBase64()!;        // transmisión por email/API
}
```

Para el paso a paso completo, empezá por [conceptos-fundamentales.md](conceptos-fundamentales.md).
