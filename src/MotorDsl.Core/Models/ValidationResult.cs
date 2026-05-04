namespace MotorDsl.Core.Models;

/// <summary>
/// Severity level for validation errors.
/// Sprint 07 | TK-48
/// </summary>
public enum ValidationSeverity
{
    /// <summary>Critical error — stops the pipeline.</summary>
    Error,

    /// <summary>Non-critical warning — pipeline continues.</summary>
    Warning
}

/// <summary>
/// Types of validation errors detected by validators.
/// Sprint 06 | TK-38, Sprint 07 | TK-48
/// </summary>
public enum ValidationErrorType
{
    /// <summary>A binding references a field that does not exist in the data.</summary>
    MissingField,

    /// <summary>A field exists but its type is incompatible (e.g., loop source is not a collection).</summary>
    TypeMismatch,

    /// <summary>A structural problem in the AST (e.g., loop without body, conditional without branches).</summary>
    InvalidStructure,

    /// <summary>The DSL JSON is syntactically invalid.</summary>
    InvalidSyntax,

    /// <summary>A required field is missing from the template schema (e.g., id, version, root, source).</summary>
    MissingRequiredField,

    /// <summary>A node type is not recognized by the engine.</summary>
    UnknownNodeType,

    /// <summary>A node type that is not supported in the integrated format (e.g., loop or conditional).</summary>
    UnsupportedInIntegratedFormat,

    /// <summary>An unresolved <c>{{placeholder}}</c> was found in an integrated document.</summary>
    UnresolvedPlaceholder
}

/// <summary>
/// Represents a single validation error found during validation.
/// Immutable after construction (Severity and Location settable via init).
/// Sprint 06 | TK-38, Sprint 07 | TK-48
/// </summary>
public class ValidationError
{
    public string Field { get; }
    public ValidationErrorType Type { get; }
    public string Message { get; }
    public string NodeType { get; }

    /// <summary>Severity level. Default = Error (backward compatible).</summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;

    /// <summary>Path/location in the template where the error was found. Default = null.</summary>
    public string? Location { get; init; } = null;

    public ValidationError(string field, ValidationErrorType type, string message, string nodeType)
    {
        Field = field;
        Type = type;
        Message = message;
        NodeType = nodeType;
    }
}

/// <summary>
/// Result of validating data against a DSL template AST.
/// IsValid is true when there are no errors.
/// Sprint 06 | TK-38
/// </summary>
public class ValidationResult
{
    public bool IsValid => !Errors.Any(e => e.Severity == ValidationSeverity.Error);
    public List<ValidationError> Errors { get; } = new();

    public void AddError(string field, ValidationErrorType type, string message, string nodeType)
    {
        Errors.Add(new ValidationError(field, type, message, nodeType));
    }
}
