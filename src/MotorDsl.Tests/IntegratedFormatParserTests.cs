using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;
using MotorDsl.Parser;

namespace MotorDsl.Tests;

/// <summary>
/// Unit tests for the integrated document format support in <see cref="DslParser"/>.
/// Verifies that "format": "integrated" is detected, that "value" is mapped to TextNode.Text,
/// and that loop/conditional nodes are rejected.
/// </summary>
public class IntegratedFormatParserTests
{
    private IDslParser CreateParser() => new DslParser();

    // ─── Format detection ─────────────────────────────────────────

    [Fact]
    public void Parse_NoFormatField_DefaultsToTemplate()
    {
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "root": { "type": "text", "text": "Hola {{nombre}}" }
        }
        """;

        var template = CreateParser().Parse(json);

        Assert.Equal(DocumentTemplate.FormatTemplate, template.Format);
    }

    [Fact]
    public void Parse_FormatTemplateExplicit_ParsesAsClassic()
    {
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "template",
            "root": { "type": "text", "text": "Hola {{nombre}}" }
        }
        """;

        var template = CreateParser().Parse(json);

        Assert.Equal(DocumentTemplate.FormatTemplate, template.Format);
        Assert.IsType<TextNode>(template.Root);
        Assert.Equal("Hola {{nombre}}", ((TextNode)template.Root!).Text);
    }

    [Fact]
    public void Parse_FormatIntegrated_FlagsTemplateAsIntegrated()
    {
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": { "type": "text", "value": "Hola Juan" }
        }
        """;

        var template = CreateParser().Parse(json);

        Assert.Equal(DocumentTemplate.FormatIntegrated, template.Format);
    }

    [Fact]
    public void Parse_UnknownFormatValue_ThrowsArgumentException()
    {
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "weird-format",
            "root": { "type": "text", "value": "Hola" }
        }
        """;

        var ex = Assert.Throws<ArgumentException>(() => CreateParser().Parse(json));
        Assert.Contains("weird-format", ex.Message);
    }

    // ─── Integrated format: value → Text mapping ──────────────────

    [Fact]
    public void Parse_IntegratedSimpleText_ReadsValueAsText()
    {
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": { "type": "text", "value": "ACTA N° 2026-00123" }
        }
        """;

        var template = CreateParser().Parse(json);
        var textNode = Assert.IsType<TextNode>(template.Root);

        Assert.Equal("ACTA N° 2026-00123", textNode.Text);
    }

    [Fact]
    public void Parse_IntegratedContainerWithChildren_PreservesTree()
    {
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": {
                "type": "container",
                "layout": "vertical",
                "children": [
                    { "type": "text", "value": "Línea 1" },
                    { "type": "text", "value": "Línea 2" },
                    { "type": "text", "value": "Línea 3" }
                ]
            }
        }
        """;

        var template = CreateParser().Parse(json);
        var container = Assert.IsType<ContainerNode>(template.Root);

        Assert.Equal("vertical", container.Layout);
        Assert.NotNull(container.Children);
        Assert.Equal(3, container.Children!.Count);
        Assert.Equal("Línea 1", ((TextNode)container.Children[0]).Text);
        Assert.Equal("Línea 2", ((TextNode)container.Children[1]).Text);
        Assert.Equal("Línea 3", ((TextNode)container.Children[2]).Text);
    }

    [Fact]
    public void Parse_IntegratedImageBitmap_PreservesSource()
    {
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": {
                "type": "image",
                "source": "data:image/bmp;base64,Qk0+KQAAAAAA",
                "imageType": "bitmap",
                "width": 200
            }
        }
        """;

        var template = CreateParser().Parse(json);
        var image = Assert.IsType<ImageNode>(template.Root);

        Assert.Equal("data:image/bmp;base64,Qk0+KQAAAAAA", image.Source);
        Assert.Equal("bitmap", image.ImageType);
        Assert.Equal(200, image.Width);
    }

    [Fact]
    public void Parse_IntegratedImageQrcode_PreservesSource()
    {
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": {
                "type": "image",
                "source": "https://multas.ejemplo.gob.ar/pago/2026-00123",
                "imageType": "qrcode"
            }
        }
        """;

        var template = CreateParser().Parse(json);
        var image = Assert.IsType<ImageNode>(template.Root);

        Assert.Equal("https://multas.ejemplo.gob.ar/pago/2026-00123", image.Source);
        Assert.Equal("qrcode", image.ImageType);
    }

    // ─── Integrated format: rejects loop / conditional ────────────

    [Fact]
    public void Parse_FormatIntegratedWithLoopNode_ThrowsArgumentException()
    {
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": {
                "type": "loop",
                "source": "items",
                "itemAlias": "it",
                "body": { "type": "text", "value": "x" }
            }
        }
        """;

        var ex = Assert.Throws<ArgumentException>(() => CreateParser().Parse(json));
        Assert.Contains("loop", ex.Message);
        Assert.Contains("integrated", ex.Message);
    }

    [Fact]
    public void Parse_FormatIntegratedWithConditionalNode_ThrowsArgumentException()
    {
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": {
                "type": "conditional",
                "expression": "x == 1",
                "trueBranch": { "type": "text", "value": "yes" }
            }
        }
        """;

        var ex = Assert.Throws<ArgumentException>(() => CreateParser().Parse(json));
        Assert.Contains("conditional", ex.Message);
        Assert.Contains("integrated", ex.Message);
    }

    // ─── Backward compatibility: classic still uses "text" ────────

    [Fact]
    public void Parse_FormatTemplateClassic_ReadsTextField()
    {
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "root": {
                "type": "container",
                "layout": "vertical",
                "children": [
                    { "type": "text", "text": "Hola {{nombre}}" },
                    {
                        "type": "loop",
                        "source": "items",
                        "itemAlias": "it",
                        "body": { "type": "text", "text": "{{it}}" }
                    }
                ]
            }
        }
        """;

        var template = CreateParser().Parse(json);
        var container = Assert.IsType<ContainerNode>(template.Root);

        Assert.Equal(DocumentTemplate.FormatTemplate, template.Format);
        Assert.Equal("Hola {{nombre}}", ((TextNode)container.Children![0]).Text);
        Assert.IsType<LoopNode>(container.Children[1]);
    }
}
