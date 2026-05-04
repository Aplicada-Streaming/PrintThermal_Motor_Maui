using MotorDsl.Core.Contracts;
using MotorDsl.Core.Engine;
using MotorDsl.Core.Evaluators;
using MotorDsl.Core.Layout;
using MotorDsl.Core.Models;
using MotorDsl.Core.Validation;
using MotorDsl.Parser;
using MotorDsl.Rendering;

namespace MotorDsl.Tests;

/// <summary>
/// End-to-end integration tests for the integrated document format.
/// Validates that the integrated pipeline (Parse → Validate → Layout → Render) produces
/// the same output as the classic pipeline with equivalent template + data.
/// </summary>
public class IntegratedFormatIntegrationTests
{
    private static IDocumentEngine CreateEngine()
    {
        var registry = new RendererRegistry();
        registry.Register(new TextRenderer());
        registry.Register(new EscPosRenderer());
        return new DocumentEngine(
            new DslParser(),
            new Evaluator(),
            new LayoutEngine(),
            registry,
            new DataValidator(),
            new TemplateValidator(),
            new ProfileValidator()
        );
    }

    private static DeviceProfile TextProfile() => new("thermal-58mm", 32, "text");

    [Fact]
    public void Pipeline_IntegratedFullDocument_GeneratesValidEscPos()
    {
        var engine = CreateEngine();
        var json = """
        {
            "id": "acta-001",
            "version": "1.0",
            "format": "integrated",
            "root": {
                "type": "container",
                "layout": "vertical",
                "children": [
                    { "type": "text", "value": "MUNICIPALIDAD DE EJEMPLO", "style": { "align": "center", "bold": true } },
                    { "type": "text", "value": "ACTA N° 2026-00123" },
                    { "type": "text", "value": "Fecha: 31/03/2026" }
                ]
            }
        }
        """;

        var profile = new DeviceProfile("thermal-58mm", 32, "escpos");
        var result = engine.Render(json, profile);

        Assert.True(result.IsSuccessful);
        Assert.IsType<byte[]>(result.Output);
        var bytes = (byte[])result.Output!;
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void Pipeline_IntegratedVsClassic_ProduceIdenticalOutput()
    {
        var engine = CreateEngine();

        var classicTemplate = """
        {
            "id": "doc-1",
            "version": "1.0",
            "root": {
                "type": "container",
                "layout": "vertical",
                "children": [
                    { "type": "text", "text": "Hola {{nombre}}" },
                    { "type": "text", "text": "Total: {{total}}" }
                ]
            }
        }
        """;
        var data = new Dictionary<string, object>
        {
            ["nombre"] = "Juan Pérez",
            ["total"] = "12345"
        };

        var integratedJson = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": {
                "type": "container",
                "layout": "vertical",
                "children": [
                    { "type": "text", "value": "Hola Juan Pérez" },
                    { "type": "text", "value": "Total: 12345" }
                ]
            }
        }
        """;

        var classicResult = engine.Render(classicTemplate, data, TextProfile());
        var integratedResult = engine.Render(integratedJson, TextProfile());

        Assert.True(classicResult.IsSuccessful);
        Assert.True(integratedResult.IsSuccessful);
        Assert.Equal(classicResult.Output?.ToString(), integratedResult.Output?.ToString());
    }

    [Fact]
    public void Pipeline_IntegratedActaShape_RendersWithExpandedLoopAndBitmap()
    {
        // Mirrors the structure produced by samples/MotorDsl.Integrated.MultaApp/Templates/MultaIntegratedDsl.cs:
        // a "loop" of multas already expanded into N concrete containers, plus bitmap and qrcode images.
        var engine = CreateEngine();
        var json = """
        {
            "id": "acta-infraccion-001",
            "version": "1.0",
            "format": "integrated",
            "root": {
                "type": "container",
                "layout": "vertical",
                "children": [
                    {
                        "type": "image",
                        "source": "data:image/bmp;base64,Qk0+KQAAAAAA",
                        "imageType": "bitmap",
                        "width": 200
                    },
                    { "type": "text", "value": "ACTA DE INFRACCIÓN N°: 2026-00123" },
                    {
                        "type": "container",
                        "layout": "vertical",
                        "children": [
                            { "type": "text", "value": "Art. 77 inc. 2 - Exceso de velocidad" },
                            { "type": "text", "value": "Puntos: 3  Monto: $15000" }
                        ]
                    },
                    {
                        "type": "container",
                        "layout": "vertical",
                        "children": [
                            { "type": "text", "value": "Art. 48 - Uso de teléfono celular" },
                            { "type": "text", "value": "Puntos: 2  Monto: $8000" }
                        ]
                    },
                    {
                        "type": "image",
                        "source": "https://multas.ejemplo.gob.ar/pago/2026-00123",
                        "imageType": "qrcode"
                    }
                ]
            }
        }
        """;

        var profile = new DeviceProfile("preview", 80, "text");
        var result = engine.Render(json, profile);

        Assert.True(result.IsSuccessful);
        var output = result.Output!.ToString()!;
        Assert.Contains("ACTA DE INFRACCIÓN N°: 2026-00123", output);
        Assert.Contains("Art. 77 inc. 2", output);
        Assert.Contains("Art. 48", output);
        Assert.Contains("Puntos: 3", output);
        Assert.Contains("Puntos: 2", output);
    }

    [Fact]
    public void Pipeline_IntegratedWithAllNodeTypes_ProducesExpectedTextOutput()
    {
        var engine = CreateEngine();
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": {
                "type": "container",
                "layout": "vertical",
                "children": [
                    { "type": "text", "value": "ENCABEZADO" },
                    {
                        "type": "container",
                        "layout": "vertical",
                        "children": [
                            { "type": "text", "value": "Sub 1" },
                            { "type": "text", "value": "Sub 2" }
                        ]
                    },
                    {
                        "type": "table",
                        "headers": ["A", "B"],
                        "rows": [["1", "2"], ["3", "4"]]
                    },
                    {
                        "type": "image",
                        "source": "https://example.com/qr/2026-00123",
                        "imageType": "qrcode"
                    }
                ]
            }
        }
        """;

        // Use a wider profile so the QR placeholder line isn't truncated.
        var profile = new DeviceProfile("preview", 80, "text");
        var result = engine.Render(json, profile);

        Assert.True(result.IsSuccessful);
        var output = result.Output!.ToString()!;
        Assert.Contains("ENCABEZADO", output);
        Assert.Contains("Sub 1", output);
        Assert.Contains("Sub 2", output);
        // Table headers are rendered as text rows
        Assert.Contains("A", output);
        Assert.Contains("B", output);
        // QR placeholder is preserved in the text representation
        Assert.Contains("[QR:", output);
    }
}
