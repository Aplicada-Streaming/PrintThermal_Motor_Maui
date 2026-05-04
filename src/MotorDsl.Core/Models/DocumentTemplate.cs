namespace MotorDsl.Core.Models;

/// <summary>
/// Represents a complete, parsed template document.
/// Contains the AST (Abstract Syntax Tree) and metadata about the template.
///
/// Source: modelo-datos-logico_v1.0.md (Section 4)
/// </summary>
public class DocumentTemplate
{
    /// <summary>Classic format: AST contains placeholders, loops and conditionals to be resolved by the Evaluator.</summary>
    public const string FormatTemplate = "template";

    /// <summary>Integrated format: AST is pre-resolved (no placeholders, loops or conditionals); Evaluate stage is skipped.</summary>
    public const string FormatIntegrated = "integrated";

    /// <summary>
    /// Gets or sets the unique identifier for this template.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the version of the template.
    /// Used for compatibility checking and template evolution.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Gets or sets the root node of the document tree.
    /// This is the entry point to the entire AST.
    /// </summary>
    public DocumentNode? Root { get; set; }

    /// <summary>
    /// Gets or sets optional metadata about the template.
    /// Can include author, creation date, description, etc.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Document format discriminator. Defaults to <see cref="FormatTemplate"/> for backward compatibility.
    /// When set to <see cref="FormatIntegrated"/> the engine skips the Evaluate stage because the AST
    /// is already fully resolved (no placeholders, no loop/conditional nodes).
    /// </summary>
    public string Format { get; set; } = FormatTemplate;

    /// <summary>
    /// Constructor for document templates.
    /// </summary>
    /// <param name="id">Template identifier</param>
    /// <param name="version">Template version</param>
    /// <param name="root">Root document node</param>
    public DocumentTemplate(string id, string version, DocumentNode? root = null)
    {
        Id = id;
        Version = version;
        Root = root;
        Metadata = new Dictionary<string, object>();
    }
}
