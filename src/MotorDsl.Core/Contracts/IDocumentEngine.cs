using MotorDsl.Core.Models;

namespace MotorDsl.Core.Contracts;

/// <summary>
/// Interface for the main document rendering engine.
/// Orchestrates the entire pipeline: DSL parsing → AST construction → layout → rendering.
/// 
/// Source: contratos-del-motor_v1.0.md (Section 3)
/// </summary>
public interface IDocumentEngine
{
    /// <summary>
    /// Renders a document from a raw DSL string.
    /// </summary>
    /// <param name="templateDsl">Raw DSL template as string</param>
    /// <param name="data">Document data to be bound</param>
    /// <param name="profile">Device profile defining rendering constraints</param>
    /// <returns>Render result with output and any warnings/errors</returns>
    RenderResult Render(string templateDsl, object data, DeviceProfile profile);

    /// <summary>
    /// Renders a document from an already-parsed template.
    /// </summary>
    /// <param name="template">Pre-parsed document template</param>
    /// <param name="data">Document data to be bound</param>
    /// <param name="profile">Device profile defining rendering constraints</param>
    /// <returns>Render result with output and any warnings/errors</returns>
    RenderResult Render(DocumentTemplate template, object data, DeviceProfile profile);

    /// <summary>
    /// Renders a document from an integrated DSL JSON string.
    /// The JSON must declare <c>"format": "integrated"</c> and contain all values
    /// already resolved (no placeholders, no loops, no conditionals).
    /// Pipeline is simplified: Parse → Validate → Layout → Render (Evaluate is skipped).
    /// </summary>
    /// <param name="integratedJson">Integrated DSL document as JSON string</param>
    /// <param name="profile">Device profile defining rendering constraints</param>
    /// <returns>Render result with output and any warnings/errors</returns>
    RenderResult Render(string integratedJson, DeviceProfile profile);

    /// <summary>
    /// Executes Parse → Evaluate → Layout without rendering.
    /// Returns the LayoutedDocument for UI preview.
    /// Sprint 06 | TK-45
    /// </summary>
    LayoutedDocument RenderLayout(string templateDsl, object data, DeviceProfile profile);
}
