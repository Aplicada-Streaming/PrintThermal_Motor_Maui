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
/// Tests for <see cref="DocumentEngine.Render(string, DeviceProfile)"/> — the integrated overload.
/// Verifies the simplified pipeline (Parse → Validate → Layout → Render) and that the Evaluator is bypassed.
/// </summary>
public class IntegratedFormatEngineTests
{
    private static IDocumentEngine CreateEngine(IEvaluator? evaluator = null)
    {
        var registry = new RendererRegistry();
        registry.Register(new TextRenderer());
        registry.Register(new EscPosRenderer());
        return new DocumentEngine(
            new DslParser(),
            evaluator ?? new Evaluator(),
            new LayoutEngine(),
            registry,
            new DataValidator(),
            new TemplateValidator(),
            new ProfileValidator()
        );
    }

    private static DeviceProfile TextProfile() => new("thermal-58mm", 32, "text");

    [Fact]
    public void Render_IntegratedJson_ProducesExpectedOutput()
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
                    { "type": "text", "value": "MUNICIPALIDAD DE EJEMPLO" },
                    { "type": "text", "value": "ACTA N° 2026-00123" }
                ]
            }
        }
        """;

        var result = engine.Render(json, TextProfile());

        Assert.True(result.IsSuccessful);
        var output = result.Output!.ToString()!;
        Assert.Contains("MUNICIPALIDAD DE EJEMPLO", output);
        Assert.Contains("ACTA N° 2026-00123", output);
    }

    [Fact]
    public void Render_IntegratedJson_DoesNotCallEvaluator()
    {
        var spyEvaluator = new SpyEvaluator();
        var engine = CreateEngine(spyEvaluator);
        var json = """
        {
            "id": "acta-001",
            "version": "1.0",
            "format": "integrated",
            "root": { "type": "text", "value": "hola" }
        }
        """;

        var result = engine.Render(json, TextProfile());

        Assert.True(result.IsSuccessful);
        Assert.Equal(0, spyEvaluator.EvaluateCallCount);
    }

    [Fact]
    public void Render_IntegratedJsonMalformed_ReturnsError()
    {
        var engine = CreateEngine();
        var json = "{ this is not valid json";

        var result = engine.Render(json, TextProfile());

        Assert.False(result.IsSuccessful);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Render_IntegratedJsonWithLoopNode_ReturnsValidationError()
    {
        var engine = CreateEngine();
        var json = """
        {
            "id": "acta-001",
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

        var result = engine.Render(json, TextProfile());

        Assert.False(result.IsSuccessful);
        Assert.Contains(result.Errors, e => e.Contains("UnsupportedInIntegratedFormat"));
    }

    [Fact]
    public void Render_ClassicJsonViaIntegratedOverload_ReturnsError()
    {
        // Defensive guard: the integrated overload only accepts integrated documents.
        var engine = CreateEngine();
        var json = """
        {
            "id": "doc-1",
            "version": "1.0",
            "root": { "type": "text", "text": "hola {{nombre}}" }
        }
        """;

        var result = engine.Render(json, TextProfile());

        Assert.False(result.IsSuccessful);
        Assert.Contains(result.Errors, e => e.Contains("integrated"));
    }

    /// <summary>
    /// Test double that records how many times <see cref="IEvaluator.Evaluate"/> is invoked.
    /// Used to assert the integrated pipeline skips the Evaluate stage.
    /// </summary>
    private sealed class SpyEvaluator : IEvaluator
    {
        public int EvaluateCallCount { get; private set; }

        public EvaluatedDocument Evaluate(DocumentNode ast, object? data)
        {
            EvaluateCallCount++;
            return new EvaluatedDocument { Id = "spy", Version = "1.0", Root = ast };
        }

        public object? ResolveVariable(object? data, string path) => null;
        public bool EvaluateCondition(string expression, object? data) => false;
        public IEnumerable<object> ResolveCollection(object? data, string path) => Enumerable.Empty<object>();
    }
}
