using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;
using MotorDsl.Core.Validation;

namespace MotorDsl.Tests;

/// <summary>
/// Tests for <see cref="TemplateValidator"/> integrated-format rules:
/// loops/conditionals are forbidden, residual {{placeholders}} are reported,
/// and the classic format keeps its existing behavior.
/// </summary>
public class IntegratedFormatValidatorTests
{
    private ITemplateValidator CreateValidator() => new TemplateValidator();

    [Fact]
    public void Validator_IntegratedValidDocument_NoErrors()
    {
        var dsl = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": {
                "type": "container",
                "layout": "vertical",
                "children": [
                    { "type": "text", "value": "MUNICIPALIDAD DE EJEMPLO" },
                    { "type": "text", "value": "ACTA N° 2026-00123" }
                ]
            }
        }
        """;

        var result = CreateValidator().ValidateTemplate(dsl);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validator_IntegratedWithLoopNode_ReportsError()
    {
        var dsl = """
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

        var result = CreateValidator().ValidateTemplate(dsl);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.Type == ValidationErrorType.UnsupportedInIntegratedFormat && e.NodeType == "loop");
    }

    [Fact]
    public void Validator_IntegratedWithConditionalNode_ReportsError()
    {
        var dsl = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": {
                "type": "container",
                "layout": "vertical",
                "children": [
                    {
                        "type": "conditional",
                        "expression": "x == 1",
                        "trueBranch": { "type": "text", "value": "yes" }
                    }
                ]
            }
        }
        """;

        var result = CreateValidator().ValidateTemplate(dsl);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.Type == ValidationErrorType.UnsupportedInIntegratedFormat && e.NodeType == "conditional");
    }

    [Fact]
    public void Validator_IntegratedTextWithPlaceholder_ReportsError()
    {
        var dsl = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": { "type": "text", "value": "Hola {{nombre}}" }
        }
        """;

        var result = CreateValidator().ValidateTemplate(dsl);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.Type == ValidationErrorType.UnresolvedPlaceholder && e.Field == "value");
    }

    [Fact]
    public void Validator_IntegratedImageWithPlaceholder_ReportsError()
    {
        var dsl = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "integrated",
            "root": {
                "type": "image",
                "source": "https://api.example.com/qr/{{id}}",
                "imageType": "qrcode"
            }
        }
        """;

        var result = CreateValidator().ValidateTemplate(dsl);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.Type == ValidationErrorType.UnresolvedPlaceholder && e.Field == "source");
    }

    [Fact]
    public void Validator_TemplateFormatStillAcceptsLoopAndConditional()
    {
        // Backward-compat: classic templates with loops/conditionals
        // and {{placeholders}} are still valid.
        var dsl = """
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
                        "body": { "type": "text", "text": "{{it.name}}" }
                    },
                    {
                        "type": "conditional",
                        "expression": "showFooter == true",
                        "trueBranch": { "type": "text", "text": "footer" }
                    }
                ]
            }
        }
        """;

        var result = CreateValidator().ValidateTemplate(dsl);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_UnknownFormat_ReportsInvalidSyntaxError()
    {
        var dsl = """
        {
            "id": "doc-1",
            "version": "1.0",
            "format": "weird",
            "root": { "type": "text", "value": "x" }
        }
        """;

        var result = CreateValidator().ValidateTemplate(dsl);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "format");
    }
}
