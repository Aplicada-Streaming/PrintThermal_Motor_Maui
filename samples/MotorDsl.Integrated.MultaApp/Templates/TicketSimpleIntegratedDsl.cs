namespace MotorDsl.Integrated.MultaApp.Templates;

/// <summary>
/// Ticket simple de multa en formato integrado (<c>"format": "integrated"</c>).
/// El JSON ya viene con todos los valores resueltos: sin <c>{{placeholders}}</c>.
/// </summary>
public static class TicketSimpleIntegratedDsl
{
    public static readonly string Document = """
    {
      "id": "ticket-simple-001",
      "version": "1.0",
      "format": "integrated",
      "root": {
        "type": "container",
        "layout": "vertical",
        "children": [
          {
            "type": "text",
            "value": "TICKET DE MULTA",
            "style": { "align": "center", "bold": true }
          },
          { "type": "text", "value": "================================" },
          {
            "type": "text",
            "value": "N°: 2026-00123",
            "style": { "bold": true }
          },
          { "type": "text", "value": "Fecha: 31/03/2026  Hora: 14:35" },
          { "type": "text", "value": "Infractor: García, Carlos Alberto" },
          { "type": "text", "value": "DNI: 28.456.789" },
          { "type": "text", "value": "Patente: AB 123 CD" },
          { "type": "text", "value": "================================" },
          {
            "type": "text",
            "value": "TOTAL: $23000",
            "style": { "align": "center", "bold": true }
          },
          { "type": "text", "value": "Vence: 30/04/2026" },
          { "type": "text", "value": "================================" },
          { "type": "text", "value": "Inspector: Juan Pérez" }
        ]
      }
    }
    """;
}
