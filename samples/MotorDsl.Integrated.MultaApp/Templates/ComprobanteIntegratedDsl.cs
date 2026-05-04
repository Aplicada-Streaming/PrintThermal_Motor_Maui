namespace MotorDsl.Integrated.MultaApp.Templates;

/// <summary>
/// Comprobante de pago en formato integrado (<c>"format": "integrated"</c>).
/// El JSON ya viene con todos los valores resueltos: sin <c>{{placeholders}}</c>.
/// </summary>
public static class ComprobanteIntegratedDsl
{
    public static readonly string Document = """
    {
      "id": "comprobante-pago-001",
      "version": "1.0",
      "format": "integrated",
      "root": {
        "type": "container",
        "layout": "vertical",
        "children": [
          {
            "type": "text",
            "value": "COMPROBANTE DE PAGO",
            "style": { "align": "center", "bold": true }
          },
          { "type": "text", "value": "================================" },
          { "type": "text", "value": "Acta N°: 2026-00123" },
          { "type": "text", "value": "Fecha de pago: 15/04/2026" },
          { "type": "text", "value": "================================" },
          { "type": "text", "value": "Pagador: García, Carlos Alberto" },
          { "type": "text", "value": "DNI: 28.456.789" },
          { "type": "text", "value": "================================" },
          {
            "type": "text",
            "value": "MONTO PAGADO: $23000",
            "style": { "align": "center", "bold": true }
          },
          { "type": "text", "value": "Medio de pago: Transferencia bancaria" },
          { "type": "text", "value": "N° Transacción: TXN-2026-87654" },
          { "type": "text", "value": "================================" },
          {
            "type": "text",
            "value": "Gracias por regularizar su situación.",
            "style": { "align": "center" }
          }
        ]
      }
    }
    """;
}
