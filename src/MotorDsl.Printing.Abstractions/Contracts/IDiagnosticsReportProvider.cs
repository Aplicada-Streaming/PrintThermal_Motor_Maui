namespace MotorDsl.Printing;

/// <summary>
/// Captura información de diagnóstico (librería, app, dispositivo, impresora) y
/// la serializa para mostrar, imprimir o compartir.
/// </summary>
public interface IDiagnosticsReportProvider
{
    /// <summary>Construye el snapshot actual de diagnóstico.</summary>
    /// <param name="notes">Texto libre que el usuario adjunta (opcional).</param>
    /// <param name="includePii">Si true, incluye datos como MAC completa. Default false.</param>
    DiagnosticsReport Build(string? notes = null, bool includePii = false);

    /// <summary>Serializa a JSON estructurado (para email/share/log).</summary>
    string ToJson(DiagnosticsReport report);

    /// <summary>Serializa a texto plano formateado (para clipboard).</summary>
    string ToPlainText(DiagnosticsReport report);

    /// <summary>
    /// Genera un DSL "integrated" (formato JSON del motor) listo para ser
    /// renderizado por <c>IDocumentEngine.Render</c> con cualquier profile.
    /// </summary>
    /// <param name="paperWidthChars">Ancho del papel en chars (32 para 58mm, 48 para 80mm).</param>
    string ToDslJson(DiagnosticsReport report, int paperWidthChars = 32);
}
