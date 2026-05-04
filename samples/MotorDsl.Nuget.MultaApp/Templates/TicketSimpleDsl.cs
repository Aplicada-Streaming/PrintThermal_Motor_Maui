namespace MotorDsl.Nuget.MultaApp.Templates;

/// <summary>
/// Template DSL de un ticket simple de multa (versión resumida).
/// </summary>
public static class TicketSimpleDsl
{
    public const string Template = """
    {
      "id": "ticket-simple-001",
      "version": "1.0",
      "root": {
        "type": "container",
        "layout": "vertical",
        "children": [
          {
            "type": "text",
            "text": "TICKET DE MULTA",
            "style": { "align": "center", "bold": true }
          },
          {
            "type": "text",
            "text": "================================"
          },
          {
            "type": "text",
            "text": "N°: {{nroActa}}",
            "style": { "bold": true }
          },
          {
            "type": "text",
            "text": "Fecha: {{fecha}}  Hora: {{hora}}"
          },
          {
            "type": "text",
            "text": "Infractor: {{infractor.apellido}}, {{infractor.nombre}}"
          },
          {
            "type": "text",
            "text": "DNI: {{infractor.dni}}"
          },
          {
            "type": "text",
            "text": "Patente: {{vehiculo.patente}}"
          },
          {
            "type": "text",
            "text": "================================"
          },
          {
            "type": "text",
            "text": "TOTAL: ${{totalMonto}}",
            "style": { "align": "center", "bold": true }
          },
          {
            "type": "text",
            "text": "Vence: {{fechaVencimiento}}"
          },
          {
            "type": "text",
            "text": "================================"
          },
          {
            "type": "text",
            "text": "Inspector: {{inspector.nombre}}"
          }
        ]
      }
    }
    """;

    public static Dictionary<string, object> GetSampleData() => new()
    {
        ["nroActa"] = "2026-00123",
        ["fecha"]   = "31/03/2026",
        ["hora"]    = "14:35",
        ["infractor"] = new Dictionary<string, object>
        {
            ["apellido"] = "García",
            ["nombre"]   = "Carlos Alberto",
            ["dni"]      = "28.456.789"
        },
        ["vehiculo"] = new Dictionary<string, object>
        {
            ["patente"] = "AB 123 CD"
        },
        ["totalMonto"]       = "23000",
        ["fechaVencimiento"] = "30/04/2026",
        ["inspector"] = new Dictionary<string, object>
        {
            ["nombre"] = "Juan Pérez"
        }
    };
}
