using System.Text.RegularExpressions;

namespace MotorDsl.Printing;

/// <summary>
/// Helpers PUROS (sin dependencias de plataforma) para armar las partes de impresora y de
/// fallos de un <see cref="DiagnosticsReport"/> a partir de un <see cref="IThermalPrinterService"/>.
/// Viven aca para poder testearse sin MAUI; el provider de MotorDsl.Maui delega en ellos.
/// </summary>
public static class DiagnosticsBuilder
{
    private static readonly Regex s_macRegex = new(
        @"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$",
        RegexOptions.Compiled);

    /// <summary>
    /// Oculta los ultimos 3 octetos si <paramref name="id"/> parece una MAC
    /// (xx:xx:xx:xx:xx:xx). Si no, mantiene los primeros 4 caracteres y agrega "...".
    /// Evita publicar MACs completas en reportes compartidos.
    /// </summary>
    public static string MaskMac(string id)
    {
        if (string.IsNullOrEmpty(id)) return string.Empty;
        if (s_macRegex.IsMatch(id))
            return id.Substring(0, 8) + "**:**:**";

        return id.Length > 4 ? id.Substring(0, 4) + "..." : id;
    }

    /// <summary>
    /// Mapea las capabilities a claves string legibles para el snapshot del reporte. Si
    /// <paramref name="caps"/> es null, devuelve todo en unknown/(none)/(unknown).
    /// </summary>
    public static IReadOnlyDictionary<string, object?> CapabilitiesToDictionary(PrinterCapabilities? caps)
    {
        caps ??= PrinterCapabilities.Unknown();
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["supports_status_feedback"] = SupportToString(caps.StatusFeedback),
            ["supports_nv_graphics"] = SupportToString(caps.NvGraphics),
            ["nv_graphics_kind"] = caps.NvGraphicsKind ?? "(none)",
            ["model_id"] = caps.ModelId ?? "(unknown)"
        };
    }

    private static string SupportToString(CapabilitySupport support) => support switch
    {
        CapabilitySupport.Supported => "supported",
        CapabilitySupport.NotDetected => "not_detected",
        _ => "unknown"
    };

    /// <summary>
    /// Arma el snapshot de la impresora vinculada (o null si no hay device). Enmascara el
    /// DeviceId cuando includePii=false y puebla las capabilities desde el servicio.
    /// </summary>
    public static PrinterInfoSnapshot? BuildPrinterSnapshot(IThermalPrinterService? printer, bool includePii)
    {
        try
        {
            var device = printer?.CurrentDevice;
            if (printer == null || device == null) return null;

            var rawId = device.Id ?? string.Empty;
            var displayId = includePii ? rawId : MaskMac(rawId);

            return new PrinterInfoSnapshot(
                Kind: device.Kind ?? "(unknown)",
                Name: device.Name ?? "(unknown)",
                Id: displayId,
                ConnectionState: printer.ConnectionState.ToString(),
                ProfileName: "(unknown)",
                PaperWidthChars: 0,
                Capabilities: CapabilitiesToDictionary(printer.CurrentCapabilities));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Arma la lista de fallos recientes (o null si no hay). Cuando includePii=false enmascara
    /// el DeviceId de cada entrada; el resto de los campos se mantienen intactos.
    /// </summary>
    public static IReadOnlyList<PrintFailureEntry>? BuildFailures(IThermalPrinterService? printer, bool includePii)
    {
        try
        {
            var failures = printer?.RecentFailures;
            if (failures == null || failures.Count == 0) return null;

            if (includePii)
                return failures.ToList();

            return failures
                .Select(f => f with { DeviceId = MaskMac(f.DeviceId) })
                .ToList();
        }
        catch
        {
            return null;
        }
    }
}
