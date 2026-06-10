namespace MotorDsl.Printing;

/// <summary>
/// Estado tri-estado del soporte de una capacidad de la impresora. Un "no contesto" puede
/// ser "no lo soporta" o "estaba ocupada", asi que NUNCA se modela como bool.
/// </summary>
public enum CapabilitySupport
{
    /// <summary>No se pudo determinar (excepcion al sondear, o aun no sondeado).</summary>
    Unknown,

    /// <summary>Se sondeo y no respondio: puede no soportarlo o estar ocupada.</summary>
    NotDetected,

    /// <summary>Respondio confirmando la capacidad.</summary>
    Supported
}
