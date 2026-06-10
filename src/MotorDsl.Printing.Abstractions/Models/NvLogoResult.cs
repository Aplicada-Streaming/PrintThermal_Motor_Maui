namespace MotorDsl.Printing;

/// <summary>
/// Resultado de aprovisionar un logo en memoria no volatil (NV) de la impresora.
/// HONESTIDAD: el soporte real de la familia FS se confirma RECIEN al aprovisionar (no hay
/// query limpia); por eso un Success con Kind="fs" lleva un Message advirtiendo que la
/// confirmacion FS es limitada. Si ninguna familia acepta el define, Success=false.
/// </summary>
public record NvLogoResult(bool Success, string? Kind, string? Message);
