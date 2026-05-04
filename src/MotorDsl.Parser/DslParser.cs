using System.Text.Json;
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;

namespace MotorDsl.Parser;

/// <summary>
/// Implementation of IDslParser using System.Text.Json.
/// Parses DSL templates in JSON format into DocumentTemplate objects.
///
/// Source: contratos-del-motor_v1.0.md (Section 7)
/// Supports element types: text, container, conditional, loop, table, image
/// Supports document formats: "template" (default, classic) and "integrated" (pre-resolved AST).
/// </summary>
public class DslParser : IDslParser
{
    private static readonly HashSet<string> SupportedTypes = new()
    {
        "text",
        "container",
        "conditional",
        "loop",
        "table",
        "image"
    };

    private static readonly HashSet<string> SupportedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        DocumentTemplate.FormatTemplate,
        DocumentTemplate.FormatIntegrated
    };

    public DocumentTemplate Parse(string dsl)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(dsl))
            throw new ArgumentException("DSL cannot be null or empty");

        try
        {
            // Parse JSON
            var doc = JsonDocument.Parse(dsl);
            var root = doc.RootElement;

            // Extract id (required)
            if (!root.TryGetProperty("id", out var idToken))
                throw new ArgumentException("DSL must contain 'id' property");

            var id = idToken.GetString();
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("'id' cannot be null or empty");

            // Extract version (required)
            if (!root.TryGetProperty("version", out var versionToken))
                throw new ArgumentException("DSL must contain 'version' property");

            var version = versionToken.GetString();
            if (string.IsNullOrEmpty(version))
                throw new ArgumentException("'version' cannot be null or empty");

            // Extract format (optional, defaults to "template")
            var format = DocumentTemplate.FormatTemplate;
            if (root.TryGetProperty("format", out var formatToken))
            {
                var formatValue = formatToken.ValueKind == JsonValueKind.String
                    ? formatToken.GetString() ?? ""
                    : "";

                if (!SupportedFormats.Contains(formatValue))
                    throw new ArgumentException(
                        $"Unsupported 'format' value: '{formatValue}'. Expected '{DocumentTemplate.FormatTemplate}' or '{DocumentTemplate.FormatIntegrated}'.");

                format = formatValue;
            }

            // Extract root node (required)
            if (!root.TryGetProperty("root", out var rootNodeToken))
                throw new ArgumentException("DSL must contain 'root' property");

            var isIntegrated = string.Equals(format, DocumentTemplate.FormatIntegrated, StringComparison.OrdinalIgnoreCase);
            var parsedRoot = ParseNode(rootNodeToken, isIntegrated);

            // Extract metadata (optional)
            var metadata = new Dictionary<string, object>();
            if (root.TryGetProperty("metadata", out var metadataToken) && metadataToken.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in metadataToken.EnumerateObject())
                {
                    object value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => (object)(prop.Value.GetString() ?? ""),
                        JsonValueKind.Number => (object)prop.Value.GetDouble(),
                        JsonValueKind.True => (object)true,
                        JsonValueKind.False => (object)false,
                        _ => (object)prop.Value.ToString()
                    };
                    metadata[prop.Name] = value;
                }
            }

            return new DocumentTemplate(id, version, parsedRoot)
            {
                Format = format,
                Metadata = metadata
            };
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON format: " + ex.Message, ex);
        }
    }

    private DocumentNode ParseNode(JsonElement nodeToken, bool isIntegrated)
    {
        if (nodeToken.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Node must be a JSON object");

        if (!nodeToken.TryGetProperty("type", out var typeToken))
            throw new ArgumentException("Node must have a 'type' property");

        var type = typeToken.GetString();
        if (string.IsNullOrEmpty(type))
            throw new ArgumentException("Node type cannot be empty");

        if (!SupportedTypes.Contains(type))
            throw new ArgumentException($"Unsupported element type: '{type}'. Supported types are: {string.Join(", ", SupportedTypes)}");

        if (isIntegrated && (type == "loop" || type == "conditional"))
            throw new ArgumentException(
                $"Node type '{type}' is not allowed in integrated format. Loops and conditionals must be pre-resolved by the producer.");

        return type switch
        {
            "text" => ParseTextNode(nodeToken, isIntegrated),
            "container" => ParseContainerNode(nodeToken, isIntegrated),
            "conditional" => ParseConditionalNode(nodeToken, isIntegrated),
            "loop" => ParseLoopNode(nodeToken, isIntegrated),
            "table" => ParseTableNode(nodeToken),
            "image" => ParseImageNode(nodeToken),
            _ => throw new ArgumentException($"Unsupported element type: '{type}'")
        };
    }

    private TextNode ParseTextNode(JsonElement nodeToken, bool isIntegrated)
    {
        // Integrated documents use "value" (already-resolved string).
        // Classic templates use "text" (may contain {{placeholders}}).
        string text;
        if (isIntegrated)
        {
            text = nodeToken.TryGetProperty("value", out var valueToken)
                ? valueToken.GetString() ?? ""
                : "";
        }
        else
        {
            text = nodeToken.TryGetProperty("text", out var textToken)
                ? textToken.GetString() ?? ""
                : "";
        }

        var bindPath = nodeToken.TryGetProperty("bindPath", out var bindToken)
            ? bindToken.GetString()
            : null;

        var node = new TextNode(text, bindPath);

        if (nodeToken.TryGetProperty("style", out var styleToken) && styleToken.ValueKind == JsonValueKind.Object)
        {
            node.Style = new StyleDefinition();
            foreach (var prop in styleToken.EnumerateObject())
            {
                object value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => (object)(prop.Value.GetString() ?? ""),
                    JsonValueKind.True => (object)true,
                    JsonValueKind.False => (object)false,
                    JsonValueKind.Number => (object)prop.Value.GetDouble(),
                    _ => (object)prop.Value.ToString()
                };
                node.Style.Attributes[prop.Name] = value;
            }
        }

        return node;
    }

    private ContainerNode ParseContainerNode(JsonElement nodeToken, bool isIntegrated)
    {
        var layout = nodeToken.TryGetProperty("layout", out var layoutToken)
            ? layoutToken.GetString()
            : null;

        var container = new ContainerNode(layout);

        if (nodeToken.TryGetProperty("children", out var childrenToken) && childrenToken.ValueKind == JsonValueKind.Array)
        {
            foreach (var childToken in childrenToken.EnumerateArray())
            {
                var childNode = ParseNode(childToken, isIntegrated);
                container.AddChild(childNode);
            }
        }

        return container;
    }

    private ConditionalNode ParseConditionalNode(JsonElement nodeToken, bool isIntegrated)
    {
        var expression = nodeToken.TryGetProperty("expression", out var expToken)
            ? expToken.GetString() ?? ""
            : "";

        var trueBranch = nodeToken.TryGetProperty("trueBranch", out var trueToken)
            ? ParseNode(trueToken, isIntegrated)
            : null;

        var falseBranch = nodeToken.TryGetProperty("falseBranch", out var falseToken)
            ? ParseNode(falseToken, isIntegrated)
            : null;

        return new ConditionalNode(expression, trueBranch, falseBranch);
    }

    private LoopNode ParseLoopNode(JsonElement nodeToken, bool isIntegrated)
    {
        var source = nodeToken.TryGetProperty("source", out var srcToken)
            ? srcToken.GetString() ?? ""
            : "";

        var itemAlias = nodeToken.TryGetProperty("itemAlias", out var aliasToken)
            ? aliasToken.GetString() ?? ""
            : "";

        var body = nodeToken.TryGetProperty("body", out var bodyToken)
            ? ParseNode(bodyToken, isIntegrated)
            : null;

        return new LoopNode(source, itemAlias, body);
    }

    private TableNode ParseTableNode(JsonElement nodeToken)
    {
        var headers = new List<string>();
        if (nodeToken.TryGetProperty("headers", out var headersToken) && headersToken.ValueKind == JsonValueKind.Array)
        {
            foreach (var headerToken in headersToken.EnumerateArray())
            {
                headers.Add(headerToken.GetString() ?? "");
            }
        }

        var rows = new List<List<string>>();
        if (nodeToken.TryGetProperty("rows", out var rowsToken) && rowsToken.ValueKind == JsonValueKind.Array)
        {
            foreach (var rowToken in rowsToken.EnumerateArray())
            {
                var row = new List<string>();
                if (rowToken.ValueKind == JsonValueKind.Array)
                {
                    foreach (var cellToken in rowToken.EnumerateArray())
                    {
                        row.Add(cellToken.GetString() ?? "");
                    }
                }
                rows.Add(row);
            }
        }

        return new TableNode(headers, rows);
    }

    private ImageNode ParseImageNode(JsonElement nodeToken)
    {
        var source = nodeToken.TryGetProperty("source", out var srcToken)
            ? srcToken.GetString() ?? ""
            : "";

        var width = nodeToken.TryGetProperty("width", out var widthToken) && widthToken.ValueKind == JsonValueKind.Number
            ? widthToken.GetInt32() as int?
            : null;

        var height = nodeToken.TryGetProperty("height", out var heightToken) && heightToken.ValueKind == JsonValueKind.Number
            ? heightToken.GetInt32() as int?
            : null;

        var imageType = nodeToken.TryGetProperty("imageType", out var typeToken)
            ? typeToken.GetString()
            : null;

        return new ImageNode(source, width, height, imageType);
    }
}
