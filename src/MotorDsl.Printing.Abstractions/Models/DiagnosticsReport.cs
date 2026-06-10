namespace MotorDsl.Printing;

/// <summary>
/// Información de una librería detectada en runtime (nombre + versión semántica
/// y opcionalmente la versión informacional con sufijo de commit/build).
/// </summary>
public record LibraryInfo(
    string Name,
    string? Version,
    string? InformationalVersion);

/// <summary>
/// Snapshot de la aplicación host (nombre comercial, versión semántica,
/// build number y package name del paquete instalado).
/// </summary>
public record AppInfoSnapshot(
    string Name,
    string Version,
    string Build,
    string PackageName);

/// <summary>
/// Snapshot del dispositivo móvil (fabricante, modelo, plataforma, versión OS,
/// idiom — Phone/Tablet/etc — y tipo de dispositivo — Physical/Virtual).
/// </summary>
public record DeviceInfoSnapshot(
    string Manufacturer,
    string Model,
    string OsPlatform,
    string OsVersion,
    string Idiom,
    string DeviceType);

/// <summary>
/// Snapshot de la impresora actualmente vinculada al servicio térmico
/// (kind, identificador del transport, nombre, estado de conexión, perfil de
/// impresión activo, ancho del papel y capabilities resueltas en el render).
/// </summary>
public record PrinterInfoSnapshot(
    string Kind,
    string Name,
    string Id,
    string ConnectionState,
    string ProfileName,
    int PaperWidthChars,
    IReadOnlyDictionary<string, object?> Capabilities);

/// <summary>
/// Snapshot del estado de los permisos de plataforma relevantes (Bluetooth,
/// ubicación). Solo aplicable en plataformas que los exponen (Android).
/// </summary>
public record PermissionsSnapshot(
    IReadOnlyDictionary<string, string> Statuses);

/// <summary>
/// Reporte de diagnóstico capturado en un instante dado: librerías cargadas,
/// info de app y dispositivo, impresora vinculada (si la hay), permisos, fecha
/// de captura y notas del usuario. Pensado para mostrar, imprimir y compartir
/// vía Share API en flujos de reporte de fallos.
/// </summary>
public record DiagnosticsReport(
    IReadOnlyList<LibraryInfo> Libraries,
    AppInfoSnapshot App,
    DeviceInfoSnapshot Device,
    PrinterInfoSnapshot? Printer,
    PermissionsSnapshot? Permissions,
    DateTimeOffset CapturedAt,
    string? Notes = null,
    // Historial acotado de fallos de impresion recientes. Opcional para no romper
    // llamadores existentes; null cuando no hay fallos registrados.
    IReadOnlyList<PrintFailureEntry>? Failures = null);
