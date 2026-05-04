namespace MotorDsl.Nuget.MultaApp.Templates;

/// <summary>
/// Template DSL de un comprobante de pago.
/// </summary>
public static class ComprobanteDsl
{
    public const string Template = """
    {
      "id": "comprobante-pago-001",
      "version": "1.0",
      "root": {
        "type": "container",
        "layout": "vertical",
        "children": [
          {
            "type": "text",
            "text": "COMPROBANTE DE PAGO",
            "style": { "align": "center", "bold": true }
          },
          {
            "type": "text",
            "text": "================================"
          },
          {
            "type": "text",
            "text": "Acta N°: {{nroActa}}"
          },
          {
            "type": "text",
            "text": "Fecha de pago: {{fechaPago}}"
          },
          {
            "type": "text",
            "text": "================================"
          },
          {
            "type": "text",
            "text": "Pagador: {{pagador.apellido}}, {{pagador.nombre}}"
          },
          {
            "type": "text",
            "text": "DNI: {{pagador.dni}}"
          },
          {
            "type": "text",
            "text": "================================"
          },
          {
            "type": "text",
            "text": "MONTO PAGADO: ${{montoPagado}}",
            "style": { "align": "center", "bold": true }
          },
          {
            "type": "text",
            "text": "Medio de pago: {{medioPago}}"
          },
          {
            "type": "text",
            "text": "N° Transacción: {{nroTransaccion}}"
          },
          {
            "type": "text",
            "text": "================================"
          },
          {
            "type": "text",
            "text": "Gracias por regularizar su situación.",
            "style": { "align": "center" }
          }
        ]
      }
    }
    """;

    public static Dictionary<string, object> GetSampleData() => new()
    {
        ["nroActa"]   = "2026-00123",
        ["fechaPago"] = "15/04/2026",
        ["pagador"] = new Dictionary<string, object>
        {
            ["apellido"] = "García",
            ["nombre"]   = "Carlos Alberto",
            ["dni"]      = "28.456.789"
        },
        ["montoPagado"]    = "23000",
        ["medioPago"]      = "Transferencia bancaria",
        ["nroTransaccion"] = "TXN-2026-87654"
    };
}
