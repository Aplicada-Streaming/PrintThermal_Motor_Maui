using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using MotorDsl.Printing;

namespace MotorDsl.Maui.Diagnostics;

/// <summary>
/// Implementación MAUI de <see cref="IDiagnosticsReportProvider"/>.
/// Usa <see cref="AppInfo"/> y <see cref="DeviceInfo"/> de Microsoft.Maui.Essentials,
/// y opcionalmente recibe un <see cref="IThermalPrinterService"/> por DI para
/// snapshotear la impresora vinculada.
/// </summary>
public class MauiDiagnosticsReportProvider : IDiagnosticsReportProvider
{
    private readonly IThermalPrinterService? _printer;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public MauiDiagnosticsReportProvider(IThermalPrinterService? printer = null)
    {
        _printer = printer;
    }

    /// <inheritdoc />
    public DiagnosticsReport Build(string? notes = null, bool includePii = false)
    {
        // El snapshot de impresora (con capabilities) y el historial de fallos (con el DeviceId
        // enmascarado segun includePii) se arman en DiagnosticsBuilder, codigo puro y testeable.
        return new DiagnosticsReport(
            Libraries: ScanLibraries(),
            App: SnapshotApp(),
            Device: SnapshotDevice(),
            Printer: DiagnosticsBuilder.BuildPrinterSnapshot(_printer, includePii),
            Permissions: SnapshotPermissions(),
            CapturedAt: DateTimeOffset.Now,
            Notes: notes,
            Failures: DiagnosticsBuilder.BuildFailures(_printer, includePii));
    }

    private static IReadOnlyList<LibraryInfo> ScanLibraries()
    {
        var list = new List<LibraryInfo>();
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName().Name;
                if (name == null || !name.StartsWith("MotorDsl.", StringComparison.Ordinal))
                    continue;

                string? version = asm.GetName().Version?.ToString();
                string? infoVersion = null;
                try
                {
                    infoVersion = asm
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                        ?.InformationalVersion;
                }
                catch
                {
                    // best-effort
                }

                list.Add(new LibraryInfo(name, version, infoVersion));
            }
        }
        catch
        {
            // si falla todo, devolvemos lista parcial / vacía sin propagar
        }

        return list
            .GroupBy(l => l.Name)
            .Select(g => g.First())
            .OrderBy(l => l.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static AppInfoSnapshot SnapshotApp()
    {
        try
        {
            var app = AppInfo.Current;
            return new AppInfoSnapshot(
                Name: app.Name ?? "(no disponible)",
                Version: app.VersionString ?? "(no disponible)",
                Build: app.BuildString ?? "(no disponible)",
                PackageName: app.PackageName ?? "(no disponible)");
        }
        catch
        {
            return new AppInfoSnapshot(
                "(no disponible)",
                "(no disponible)",
                "(no disponible)",
                "(no disponible)");
        }
    }

    private static DeviceInfoSnapshot SnapshotDevice()
    {
        try
        {
            var d = DeviceInfo.Current;
            return new DeviceInfoSnapshot(
                Manufacturer: d.Manufacturer ?? "(no disponible)",
                Model: d.Model ?? "(no disponible)",
                OsPlatform: d.Platform.ToString(),
                OsVersion: d.VersionString ?? "(no disponible)",
                Idiom: d.Idiom.ToString(),
                DeviceType: d.DeviceType.ToString());
        }
        catch
        {
            return new DeviceInfoSnapshot(
                "(no disponible)",
                "(no disponible)",
                "(no disponible)",
                "(no disponible)",
                "(no disponible)",
                "(no disponible)");
        }
    }

    private static PermissionsSnapshot? SnapshotPermissions()
    {
#if ANDROID
        try
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);

            void Probe<T>(string key) where T : Permissions.BasePermission, new()
            {
                try
                {
                    var status = Permissions.CheckStatusAsync<T>()
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                    dict[key] = status.ToString();
                }
                catch (Exception ex)
                {
                    dict[key] = $"Error: {ex.GetType().Name}";
                }
            }

            Probe<Permissions.Bluetooth>("Bluetooth");
            Probe<Permissions.LocationWhenInUse>("LocationWhenInUse");

            return new PermissionsSnapshot(dict);
        }
        catch
        {
            return new PermissionsSnapshot(new Dictionary<string, string>());
        }
#else
        return null;
#endif
    }

    /// <inheritdoc />
    public string ToJson(DiagnosticsReport report)
    {
        return JsonSerializer.Serialize(report, s_jsonOptions);
    }

    /// <inheritdoc />
    public string ToPlainText(DiagnosticsReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== REPORTE DE DIAGNOSTICO ===");
        sb.AppendLine($"Fecha: {report.CapturedAt:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine();

        sb.AppendLine("[LIBRERIA]");
        if (report.Libraries.Count == 0)
        {
            sb.AppendLine("- (sin librerias detectadas)");
        }
        else
        {
            foreach (var lib in report.Libraries)
            {
                var ver = lib.InformationalVersion ?? lib.Version ?? "(sin version)";
                sb.AppendLine($"- {lib.Name} {ver}");
            }
        }
        sb.AppendLine();

        sb.AppendLine("[APLICACION]");
        sb.AppendLine($"Nombre: {report.App.Name}");
        sb.AppendLine($"Version: {report.App.Version}");
        sb.AppendLine($"Build: {report.App.Build}");
        sb.AppendLine($"Paquete: {report.App.PackageName}");
        sb.AppendLine();

        sb.AppendLine("[DISPOSITIVO]");
        sb.AppendLine($"Fabricante: {report.Device.Manufacturer}");
        sb.AppendLine($"Modelo: {report.Device.Model}");
        sb.AppendLine($"Plataforma: {report.Device.OsPlatform} {report.Device.OsVersion}");
        sb.AppendLine($"Idiom: {report.Device.Idiom}");
        sb.AppendLine($"Tipo: {report.Device.DeviceType}");
        sb.AppendLine();

        if (report.Printer != null)
        {
            sb.AppendLine("[IMPRESORA]");
            sb.AppendLine($"Kind: {report.Printer.Kind}");
            sb.AppendLine($"Nombre: {report.Printer.Name}");
            sb.AppendLine($"Id: {report.Printer.Id}");
            sb.AppendLine($"Estado: {report.Printer.ConnectionState}");
            sb.AppendLine($"Profile: {report.Printer.ProfileName}");
            if (report.Printer.PaperWidthChars > 0)
                sb.AppendLine($"PaperWidthChars: {report.Printer.PaperWidthChars}");
            if (report.Printer.Capabilities.Count > 0)
            {
                sb.AppendLine("Capabilities:");
                foreach (var kv in report.Printer.Capabilities)
                    sb.AppendLine($"  {kv.Key}: {kv.Value}");
            }
            sb.AppendLine();
        }

        if (report.Permissions != null && report.Permissions.Statuses.Count > 0)
        {
            sb.AppendLine("[PERMISOS]");
            foreach (var kv in report.Permissions.Statuses)
                sb.AppendLine($"{kv.Key}: {kv.Value}");
            sb.AppendLine();
        }

        if (report.Failures != null && report.Failures.Count > 0)
        {
            sb.AppendLine("[FALLOS RECIENTES]");
            foreach (var f in report.Failures)
            {
                // Formato compacto por fallo: fecha, modelo/kind, tipo de error, intentos.
                sb.AppendLine(
                    $"- {f.Timestamp:yyyy-MM-dd HH:mm:ss} {f.DeviceName}/{f.DeviceKind} " +
                    $"{f.ErrorType} (intentos: {f.Attempts}, bytes: {f.BytesLength})");
            }
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(report.Notes))
        {
            sb.AppendLine("[NOTAS]");
            sb.AppendLine(report.Notes);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public string ToDslJson(DiagnosticsReport report, int paperWidthChars = 32)
    {
        // Genera un documento "integrated" (mismo schema usado por
        // MultaIntegratedDsl.Document): un root container con children
        // de tipo text e image (qrcode). El motor lo procesa via
        // IDocumentEngine.Render(json, profile) sin etapa de Evaluate.
        int width = paperWidthChars > 0 ? paperWidthChars : 32;
        var separator = new string('=', width);

        var children = new List<object>();

        children.Add(NewText("REPORTE DE DIAGNOSTICO", align: "center", bold: true));
        children.Add(NewText(report.CapturedAt.ToString("yyyy-MM-dd HH:mm:ss"), align: "center"));
        children.Add(NewText(separator));

        // ── Libreria ──
        children.Add(NewText("LIBRERIA", bold: true));
        if (report.Libraries.Count == 0)
        {
            children.Add(NewText("(sin librerias detectadas)"));
        }
        else
        {
            foreach (var lib in report.Libraries)
            {
                var ver = lib.InformationalVersion ?? lib.Version ?? "?";
                children.Add(NewText($"{lib.Name} {ver}"));
            }
        }
        children.Add(NewText(separator));

        // ── Aplicacion ──
        children.Add(NewText("APLICACION", bold: true));
        children.Add(NewText($"Nombre: {report.App.Name}"));
        children.Add(NewText($"Version: {report.App.Version}"));
        children.Add(NewText($"Build: {report.App.Build}"));
        children.Add(NewText($"Paquete: {report.App.PackageName}"));
        children.Add(NewText(separator));

        // ── Dispositivo ──
        children.Add(NewText("DISPOSITIVO", bold: true));
        children.Add(NewText($"Fabric.: {report.Device.Manufacturer}"));
        children.Add(NewText($"Modelo: {report.Device.Model}"));
        children.Add(NewText($"OS: {report.Device.OsPlatform} {report.Device.OsVersion}"));
        children.Add(NewText($"Idiom: {report.Device.Idiom}"));
        children.Add(NewText($"Tipo: {report.Device.DeviceType}"));
        children.Add(NewText(separator));

        // ── Impresora (opcional) ──
        if (report.Printer != null)
        {
            children.Add(NewText("IMPRESORA", bold: true));
            children.Add(NewText($"Kind: {report.Printer.Kind}"));
            children.Add(NewText($"Nombre: {report.Printer.Name}"));
            children.Add(NewText($"Id: {report.Printer.Id}"));
            children.Add(NewText($"Estado: {report.Printer.ConnectionState}"));
            children.Add(NewText(separator));
        }

        // ── Fallos recientes: solo el resumen para no inflar el ticket ──
        int failureCount = report.Failures?.Count ?? 0;
        if (failureCount > 0)
        {
            children.Add(NewText($"Fallos recientes: {failureCount}", bold: true));
            children.Add(NewText(separator));
        }

        // ── Permisos (opcional) ──
        if (report.Permissions != null && report.Permissions.Statuses.Count > 0)
        {
            children.Add(NewText("PERMISOS", bold: true));
            foreach (var kv in report.Permissions.Statuses)
                children.Add(NewText($"{kv.Key}: {kv.Value}"));
            children.Add(NewText(separator));
        }

        // ── Notas (opcional) ──
        if (!string.IsNullOrWhiteSpace(report.Notes))
        {
            children.Add(NewText("NOTAS", bold: true));
            children.Add(NewText(report.Notes!));
            children.Add(NewText(separator));
        }

        // ── QR con payload compacto ──
        var qrPayload = BuildQrPayload(report);
        children.Add(NewText("QR diagnóstico:", align: "center"));
        children.Add(new Dictionary<string, object?>
        {
            ["type"] = "image",
            ["source"] = qrPayload,
            ["imageType"] = "qrcode"
        });

        var doc = new Dictionary<string, object?>
        {
            ["id"] = "diagnostics-report",
            ["version"] = "1.0",
            ["format"] = "integrated",
            ["root"] = new Dictionary<string, object?>
            {
                ["type"] = "container",
                ["layout"] = "vertical",
                ["children"] = children
            }
        };

        return JsonSerializer.Serialize(doc, s_jsonOptions);
    }

    private static Dictionary<string, object?> NewText(string value, string? align = null, bool bold = false)
    {
        var node = new Dictionary<string, object?>
        {
            ["type"] = "text",
            ["value"] = value
        };

        if (align != null || bold)
        {
            var style = new Dictionary<string, object?>();
            if (align != null) style["align"] = align;
            if (bold) style["bold"] = true;
            node["style"] = style;
        }

        return node;
    }

    private static string BuildQrPayload(DiagnosticsReport report)
    {
        // Payload compacto ~80-100 chars con identificadores claves para
        // correlacionar el reporte con la versión del binario y el dispositivo.
        var mauiLib = report.Libraries.FirstOrDefault(l =>
            string.Equals(l.Name, "MotorDsl.Maui", StringComparison.Ordinal));
        var ver = mauiLib?.InformationalVersion ?? mauiLib?.Version ?? "?";
        var unix = report.CapturedAt.ToUnixTimeSeconds();

        return $"ver={ver}|app={report.App.Version}|dev={report.Device.Model}" +
               $"|os={report.Device.OsPlatform}_{report.Device.OsVersion}|t={unix}";
    }
}
