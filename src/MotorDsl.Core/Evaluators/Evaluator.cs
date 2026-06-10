using System.Dynamic;
using System.Text.RegularExpressions;
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;

namespace MotorDsl.Core.Evaluators;

/// <summary>
/// Implements IEvaluator to resolve AST nodes against data contexts.
/// Handles: variable resolution, conditional evaluation, loop expansion.
/// 
/// Sprint 2 | TK-10 through TK-12
/// Supports: CU-02, CU-15, CU-16, CU-17
/// </summary>
public class Evaluator : IEvaluator
{
    private readonly IDataResolver _dataResolver;

    /// <summary>
    /// Constructor with dependency injection of IDataResolver.
    /// </summary>
    public Evaluator(IDataResolver? dataResolver = null)
    {
        _dataResolver = dataResolver ?? new DataResolver();
    }

    /// <summary>
    /// Evaluates an AST node against data and returns an EvaluatedDocument.
    /// </summary>
    public EvaluatedDocument Evaluate(DocumentNode ast, object? data)
    {
        if (ast == null)
        {
            return new EvaluatedDocument
            {
                Id = "unknown",
                Version = "1.0",
                Root = null,
                Errors = new() { "AST node is null" }
            };
        }

        var warnings = new List<string>();
        var evaluated = new EvaluatedDocument
        {
            Id = "evaluated-doc",
            Version = "1.0",
            Root = EvaluateNode(ast, data, warnings),
            Warnings = warnings
        };

        return evaluated;
    }

    /// <summary>
    /// Evaluates a DocumentTemplate and returns an EvaluatedDocument with full metadata.
    /// </summary>
    public EvaluatedDocument EvaluateTemplate(DocumentTemplate template, object? data)
    {
        if (template == null)
        {
            return new EvaluatedDocument
            {
                Id = "unknown",
                Version = "1.0",
                Root = null,
                Errors = new() { "Template is null" }
            };
        }

        var warnings = new List<string>();
        var evaluated = new EvaluatedDocument
        {
            Id = template.Id,
            Version = template.Version,
            Root = EvaluateNode(template.Root, data, warnings),
            Warnings = warnings
        };

        return evaluated;
    }

    public object? ResolveVariable(object? data, string path)
    {
        return _dataResolver.Resolve(data, path);
    }

    public bool EvaluateCondition(string expression, object? data)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Expression cannot be null or empty", nameof(expression));

        // Reject invalid operators like === or !==
        if (expression.Contains("===") || expression.Contains("!=="))
            throw new ArgumentException($"Invalid operator in expression: {expression}");

        try
        {
            // Simple evaluator for conditions like: "status == 'activo'", "cantidad > 50", "a && b || c"
            return EvaluateExpression(expression, data);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid condition expression: {expression}", ex);
        }
    }

    public IEnumerable<object> ResolveCollection(object? data, string path)
    {
        return _dataResolver.ResolveCollection(data, path);
    }

    #region Private Evaluation Methods

    /// <summary>
    /// Recursively evaluates a DocumentNode and returns the evaluated result.
    /// </summary>
    private DocumentNode? EvaluateNode(DocumentNode? node, object? data, List<string> warnings)
    {
        if (node == null)
            return null;

        return node switch
        {
            TextNode textNode => EvaluateTextNode(textNode, data, warnings),
            ConditionalNode conditionalNode => EvaluateConditionalNode(conditionalNode, data, warnings),
            LoopNode loopNode => EvaluateLoopNode(loopNode, data, warnings),
            ContainerNode containerNode => EvaluateContainerNode(containerNode, data, warnings),
            ImageNode imageNode => EvaluateImageNode(imageNode, data, warnings),
            _ => node // Return other node types as-is
        };
    }

    /// <summary>
    /// Recursively evaluates a DocumentNode (overload without warnings tracking for backward compatibility).
    /// </summary>
    private DocumentNode? EvaluateNode(DocumentNode? node, object? data)
    {
        return EvaluateNode(node, data, new List<string>());
    }

    private TextNode? EvaluateTextNode(TextNode node, object? data, List<string> warnings)
    {
        if (node.BindPath == null && string.IsNullOrEmpty(node.Text))
            return node;

        string resolvedText = node.Text ?? "";

        // Replace {{variable}} patterns with resolved values
        resolvedText = Regex.Replace(resolvedText, @"\{\{([^}]+)\}\}", (match) =>
        {
            string varPath = match.Groups[1].Value;
            var value = ResolveVariable(data, varPath);
            if (value == null)
            {
                warnings.Add($"Unresolved variable: {varPath}");
                return $"{{{{UNRESOLVED:{varPath}}}}}";
            }
            return value.ToString() ?? "";
        });

        return new TextNode(resolvedText, node.BindPath);
    }

    private TextNode? EvaluateTextNode(TextNode node, object? data)
    {
        return EvaluateTextNode(node, data, new List<string>());
    }

    private ImageNode EvaluateImageNode(ImageNode node, object? data, List<string> warnings)
    {
        // Interpolate {{variables}} in the image source URI
        var resolvedSource = Regex.Replace(node.Source ?? "", @"\{\{([^}]+)\}\}", (match) =>
        {
            string varPath = match.Groups[1].Value;
            var value = ResolveVariable(data, varPath);
            if (value == null)
            {
                warnings.Add($"Unresolved variable in image source: {varPath}");
                return match.Value; // keep placeholder
            }
            return value.ToString() ?? "";
        });

        return new ImageNode(resolvedSource, node.Width, node.Height, node.ImageType, node.Role);
    }

    private DocumentNode? EvaluateConditionalNode(ConditionalNode node, object? data, List<string> warnings)
    {
        bool conditionResult = EvaluateCondition(node.Expression, data);

        if (conditionResult)
        {
            return EvaluateNode(node.TrueBranch, data, warnings);
        }
        else
        {
            if (node.FalseBranch != null)
                return EvaluateNode(node.FalseBranch, data, warnings);
            else
                return null; // No false branch, exclude this node
        }
    }

    private DocumentNode? EvaluateConditionalNode(ConditionalNode node, object? data)
    {
        return EvaluateConditionalNode(node, data, new List<string>());
    }

    private DocumentNode? EvaluateLoopNode(LoopNode node, object? data, List<string> warnings)
    {
        var items = ResolveCollection(data, node.Source);
        var children = new List<DocumentNode>();

        foreach (var item in items)
        {
            // Create a data context with the loop variable
            var loopData = new ExpandoObject() as IDictionary<string, object?>;
            
            // Copy original data properties (dictionary-first for AOT safety)
            if (data is IDictionary<string, object> dictData)
            {
                foreach (var kvp in dictData)
                    loopData[kvp.Key] = kvp.Value;
            }
            else if (data is IDictionary<string, object?> dictDataNullable)
            {
                foreach (var kvp in dictDataNullable)
                    loopData[kvp.Key] = kvp.Value;
            }
            else if (data != null)
            {
                var dataProps = data.GetType().GetProperties();
                foreach (var prop in dataProps)
                {
                    loopData[prop.Name] = prop.GetValue(data);
                }
            }

            // Add loop variable
            loopData[node.ItemAlias] = item;

            // Evaluate body with loop data context
            var evaluatedBody = EvaluateNode(node.Body, loopData, warnings);
            if (evaluatedBody != null)
                children.Add(evaluatedBody);
        }

        if (children.Count == 0)
        {
            // Return empty container
            return new ContainerNode("vertical", new List<DocumentNode>());
        }

        return new ContainerNode("vertical", children);
    }

    private DocumentNode? EvaluateLoopNode(LoopNode node, object? data)
    {
        return EvaluateLoopNode(node, data, new List<string>());
    }

    private ContainerNode? EvaluateContainerNode(ContainerNode node, object? data, List<string> warnings)
    {
        if (node.Children == null || node.Children.Count == 0)
            return node;

        var evaluatedChildren = new List<DocumentNode>();

        foreach (var child in node.Children)
        {
            var evaluated = EvaluateNode(child, data, warnings);
            if (evaluated != null)
                evaluatedChildren.Add(evaluated);
        }

        return new ContainerNode(node.Layout, evaluatedChildren);
    }

    private ContainerNode? EvaluateContainerNode(ContainerNode node, object? data)
    {
        return EvaluateContainerNode(node, data, new List<string>());
    }

    /// <summary>
    /// Evaluates a condition expression with simple expression parsing.
    /// Supports: ==, !=, >, <, >=, <=, &&, ||
    /// Examples: "status == 'activo'", "cantidad > 50", "a == 1 && b == 2"
    /// </summary>
    private bool EvaluateExpression(string expression, object? data)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        expression = expression.Trim();

        // Handle OR (||) - lowest precedence
        if (expression.Contains("||"))
        {
            var parts = SplitExpression(expression, "||");
            foreach (var part in parts)
            {
                if (EvaluateExpression(part.Trim(), data))
                    return true;
            }
            return false;
        }

        // Handle AND (&&) - higher precedence
        if (expression.Contains("&&"))
        {
            var parts = SplitExpression(expression, "&&");
            foreach (var part in parts)
            {
                if (!EvaluateExpression(part.Trim(), data))
                    return false;
            }
            return true;
        }

        // Handle comparison operators
        var operators = new[] { "==", "!=", ">=", "<=", ">", "<" };
        foreach (var op in operators)
        {
            int opIndex = FindOperatorIndex(expression, op);
            if (opIndex >= 0)
            {
                var left = expression.Substring(0, opIndex).Trim();
                var right = expression.Substring(opIndex + op.Length).Trim();
                
                var leftValue = EvaluateOperand(left, data);
                var rightValue = EvaluateOperand(right, data);
                return CompareValues(leftValue, rightValue, op);
            }
        }

        throw new ArgumentException($"Cannot parse expression: {expression}");
    }

    /// <summary>
    /// Finds the index of an operator, ignoring operators inside string literals.
    /// </summary>
    private int FindOperatorIndex(string expression, string op)
    {
        int index = 0;
        bool inSingleQuote = false;
        bool inDoubleQuote = false;

        while (index <= expression.Length - op.Length)
        {
            char c = expression[index];

            if (c == '\'' && !inDoubleQuote)
                inSingleQuote = !inSingleQuote;
            else if (c == '"' && !inSingleQuote)
                inDoubleQuote = !inDoubleQuote;
            else if (!inSingleQuote && !inDoubleQuote && expression.Substring(index, op.Length) == op)
                return index;

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Splits an expression by an operator, respecting string literals.
    /// </summary>
    private string[] SplitExpression(string expression, string separator)
    {
        var parts = new List<string>();
        int lastIndex = 0;
        int index = 0;
        bool inSingleQuote = false;
        bool inDoubleQuote = false;

        while (index <= expression.Length - separator.Length)
        {
            char c = expression[index];

            if (c == '\'' && !inDoubleQuote)
                inSingleQuote = !inSingleQuote;
            else if (c == '"' && !inSingleQuote)
                inDoubleQuote = !inDoubleQuote;
            else if (!inSingleQuote && !inDoubleQuote && expression.Substring(index, separator.Length) == separator)
            {
                parts.Add(expression.Substring(lastIndex, index - lastIndex));
                lastIndex = index + separator.Length;
                index = lastIndex - 1; // Will be incremented at the end of loop
            }

            index++;
        }

        parts.Add(expression.Substring(lastIndex));
        return parts.ToArray();
    }

    private object? EvaluateOperand(string operand, object? data)
    {
        operand = operand.Trim();

        // String literal: 'activo' or "activo"
        if ((operand.StartsWith("'") && operand.EndsWith("'")) ||
            (operand.StartsWith("\"") && operand.EndsWith("\"")))
        {
            return operand.Substring(1, operand.Length - 2);
        }

        // Numeric literal
        if (int.TryParse(operand, out int intValue))
            return intValue;
        if (double.TryParse(operand, out double doubleValue))
            return doubleValue;

        // Boolean literal
        if (operand == "true")
            return true;
        if (operand == "false")
            return false;

        // Variable reference
        return ResolveVariable(data, operand);
    }

    private bool CompareValues(object? left, object? right, string op)
    {
        return op switch
        {
            "==" => Equals(left, right),
            "!=" => !Equals(left, right),
            ">" => Compare(left, right) > 0,
            "<" => Compare(left, right) < 0,
            ">=" => Compare(left, right) >= 0,
            "<=" => Compare(left, right) <= 0,
            _ => false
        };
    }

    private int Compare(object? left, object? right)
    {
        if (left == null && right == null)
            return 0;
        if (left == null)
            return -1;
        if (right == null)
            return 1;

        if (left is IComparable leftComparable)
            return leftComparable.CompareTo(right);

        return left.ToString()?.CompareTo(right.ToString()) ?? 0;
    }

    #endregion
}
