namespace MotorDsl.Printing;

/// <summary>
/// Helper PURO de seleccion de estrategia de impresion. La libreria solo RECOMIENDA; la
/// decision final es de la app.
/// </summary>
public static class PrintStrategySelector
{
    /// <summary>
    /// Recomienda el render target segun las capacidades:
    /// NvGraphics == Supported -> "escpos" (nativo, el logo puede salir por recall NV);
    /// si no -> "escpos-bitmap" (bitmap completo).
    /// </summary>
    public static string RecommendTarget(PrinterCapabilities? caps, bool docNeedsGraphics)
    {
        // docNeedsGraphics se reserva para heuristicas futuras (p.ej. un doc sin imagenes
        // podria ir siempre por "escpos"). Hoy la recomendacion depende solo del soporte NV.
        _ = docNeedsGraphics;
        return caps?.NvGraphics == CapabilitySupport.Supported ? "escpos" : "escpos-bitmap";
    }
}
