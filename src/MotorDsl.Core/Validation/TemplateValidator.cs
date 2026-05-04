using System.Text.Json;
using System.Text.RegularExpressions;
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;

namespace MotorDsl.Core.Validation;

/// <summary>
/// Validates DSL template JSON for syntax, schema, node types and required properties.
/// Sprint 07 | TK-50
/// Supports: CU-14
/// Extended to validate the integrated document format (Format = "integrated").
/// </summary>
public class TemplateValidator : ITemplateValidator
{
    private static readonly HashSet<string> ValidNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text", "container", "conditional", "loop", "table", "image"
    };

    private static readonly HashSet<string> ValidFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        DocumentTemplate.FormatTemplate,
        DocumentTemplate.FormatIntegrated
    };

    private static readonly Regex PlaceholderRegex = new(@"\{\{[^}]+\}\}", RegexOptions.Compiled);

    public ValidationResult ValidateTemplate(string dslJson)
    {
        var result = new ValidationResult();

        // 1. JSON syntax
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(dslJson);
        }
        catch (JsonException ex)
        {
            result.Errors.Add(new ValidationError(
                "dslJson", ValidationErrorType.InvalidSyntax, $"Invalid JSON: {ex.Message}", "Template")
            {
                Location = "root"
            });
            return result;
        }

        var root = doc.RootElement;

        // 2. Required root fields: id, version, root
        ValidateRequiredRootField(root, "id", result);
        ValidateRequiredRootField(root, "version", result);

        // 3. Optional 'format' field — defaults to "template"
        var format = DocumentTemplate.FormatTemplate;
        if (root.TryGetProperty("format", out var formatToken))
        {
            var formatValue = formatToken.ValueKind == JsonValueKind.String
                ? formatToken.GetString() ?? ""
                : "";

            if (!ValidFormats.Contains(formatValue))
            {
                result.Errors.Add(new ValidationError(
                    "format", ValidationErrorType.InvalidSyntax,
                    $"Unsupported 'format' value: '{formatValue}'. Expected '{DocumentTemplate.FormatTemplate}' or '{DocumentTemplate.FormatIntegrated}'.",
                    "Template")
                {
                    Location = "root"
                });
                return result;
            }

            format = formatValue;
        }

        if (!root.TryGetProperty("root", out var rootNode))
        {
            result.Errors.Add(new ValidationError(
                "root", ValidationErrorType.MissingRequiredField, "Template must have a 'root' node", "Template")
            {
                Location = "root"
            });
            return result;
        }

        // 4. Validate nodes recursively (rules depend on the format)
        var isIntegrated = string.Equals(format, DocumentTemplate.FormatIntegrated, StringComparison.OrdinalIgnoreCase);
        ValidateNode(rootNode, "root", isIntegrated, result);

        return result;
    }

    private static void ValidateRequiredRootField(JsonElement root, string fieldName, ValidationResult result)
    {
        if (!root.TryGetProperty(fieldName, out _))
        {
            result.Errors.Add(new ValidationError(
                fieldName, ValidationErrorType.MissingRequiredField,
                $"Template must have '{fieldName}'", "Template")
            {
                Location = "root"
            });
        }
    }

    private static void ValidateNode(JsonElement node, string path, bool isIntegrated, ValidationResult result)
    {
        if (node.ValueKind != JsonValueKind.Object)
            return;

        // Check "type" field exists
        if (!node.TryGetProperty("type", out var typeProp))
        {
            result.Errors.Add(new ValidationError(
                "type", ValidationErrorType.MissingRequiredField,
                "Node must have a 'type' property", "Unknown")
            {
                Location = path
            });
            return;
        }

        var nodeType = typeProp.GetString() ?? "";

        // Validate node type is known
        if (!ValidNodeTypes.Contains(nodeType))
        {
            result.Errors.Add(new ValidationError(
                "type", ValidationErrorType.UnknownNodeType,
                $"Unknown node type: '{nodeType}'", nodeType)
            {
                Location = path
            });
            return;
        }

        // In integrated format, loop and conditional nodes are not allowed
        if (isIntegrated && (nodeType.Equals("loop", StringComparison.OrdinalIgnoreCase) ||
                              nodeType.Equals("conditional", StringComparison.OrdinalIgnoreCase)))
        {
            result.Errors.Add(new ValidationError(
                "type", ValidationErrorType.UnsupportedInIntegratedFormat,
                $"Node type '{nodeType}' is not allowed in integrated format. Loops and conditionals must be pre-resolved by the producer.",
                nodeType)
            {
                Location = path
            });
            return;
        }

        // Validate required properties per node type
        switch (nodeType.ToLower())
        {
            case "loop":
                ValidateRequiredNodeField(node, "source", nodeType, path, result);
                ValidateRequiredNodeField(node, "itemAlias", nodeType, path, result);
                ValidateRequiredNodeField(node, "body", nodeType, path, result);
                if (node.TryGetProperty("body", out var loopBody))
                    ValidateNode(loopBody, $"{path}.body", isIntegrated, result);
                break;

            case "conditional":
                ValidateRequiredNodeField(node, "expression", nodeType, path, result);
                if (node.TryGetProperty("trueBranch", out var trueBranch))
                    ValidateNode(trueBranch, $"{path}.trueBranch", isIntegrated, result);
                if (node.TryGetProperty("falseBranch", out var falseBranch))
                    ValidateNode(falseBranch, $"{path}.falseBranch", isIntegrated, result);
                break;

            case "container":
                if (node.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
                {
                    int idx = 0;
                    foreach (var child in children.EnumerateArray())
                    {
                        ValidateNode(child, $"{path}.children[{idx}]", isIntegrated, result);
                        idx++;
                    }
                }
                break;

            case "image":
                ValidateRequiredNodeField(node, "source", nodeType, path, result);
                if (isIntegrated && node.TryGetProperty("source", out var srcToken) &&
                    srcToken.ValueKind == JsonValueKind.String)
                {
                    var src = srcToken.GetString() ?? "";
                    if (PlaceholderRegex.IsMatch(src))
                    {
                        result.Errors.Add(new ValidationError(
                            "source", ValidationErrorType.UnresolvedPlaceholder,
                            "Integrated documents must not contain '{{placeholder}}' tokens in image 'source'.",
                            nodeType)
                        {
                            Location = path
                        });
                    }
                }
                break;

            case "text":
                if (isIntegrated)
                {
                    // Integrated text nodes use 'value' (resolved string), not 'text' (placeholder source)
                    if (node.TryGetProperty("value", out var valueToken) &&
                        valueToken.ValueKind == JsonValueKind.String)
                    {
                        var value = valueToken.GetString() ?? "";
                        if (PlaceholderRegex.IsMatch(value))
                        {
                            result.Errors.Add(new ValidationError(
                                "value", ValidationErrorType.UnresolvedPlaceholder,
                                "Integrated documents must not contain '{{placeholder}}' tokens in text 'value'.",
                                nodeType)
                            {
                                Location = path
                            });
                        }
                    }
                }
                break;

            case "table":
                // No mandatory fields beyond "type"
                break;
        }
    }

    private static void ValidateRequiredNodeField(JsonElement node, string fieldName, string nodeType, string path, ValidationResult result)
    {
        if (!node.TryGetProperty(fieldName, out _))
        {
            result.Errors.Add(new ValidationError(
                fieldName, ValidationErrorType.MissingRequiredField,
                $"'{nodeType}' node must have '{fieldName}'", nodeType)
            {
                Location = path
            });
        }
    }
}
