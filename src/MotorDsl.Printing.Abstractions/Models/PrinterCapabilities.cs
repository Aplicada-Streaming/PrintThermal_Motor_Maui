namespace MotorDsl.Printing;

/// <summary>
/// Capacidades de la impresora detectadas al conectar. Inmutable. Cada capacidad usa el
/// tri-estado <see cref="CapabilitySupport"/> para distinguir "no soportado" de "no se supo".
/// </summary>
public record PrinterCapabilities(
    CapabilitySupport StatusFeedback,
    CapabilitySupport NvGraphics,
    string? NvGraphicsKind,   // "gsl" | "fs" | null (que familia respondio)
    string? ModelId)          // best-effort desde GS I, o null
{
    /// <summary>Estado inicial: todo en Unknown/null, antes de detectar.</summary>
    public static PrinterCapabilities Unknown() =>
        new(CapabilitySupport.Unknown, CapabilitySupport.Unknown, null, null);
}
