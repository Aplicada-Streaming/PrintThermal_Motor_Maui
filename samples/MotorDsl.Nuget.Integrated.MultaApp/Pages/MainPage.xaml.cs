using Microsoft.Maui.ApplicationModel.DataTransfer;
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;
using MotorDsl.Nuget.Integrated.MultaApp.Templates;
using MotorDsl.Printing;
using MotorDsl.Rendering;

#if ANDROID
using Android;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
#endif

namespace MotorDsl.Nuget.Integrated.MultaApp.Pages;

public partial class MainPage : ContentPage
{
    private readonly IDocumentEngine _engine;
    private readonly IThermalPrinterService _printer;
    private readonly IDiagnosticsReportProvider _diagnostics;
    private readonly IBitmapRasterizer _rasterizer;

    // Keycode del logo en NV. Configurable: una const por default. El recall lo usa por keycode
    // (familia GS ( L); para FS el define crea la imagen #1 y el recall usa ese indice.
    private const int LOGO_KEYCODE = 32;

    // La app recuerda si aprovisiono el logo en ESTA sesion (la libreria solo da define/clear/
    // query + recall; la politica de cuando re-aprovisionar es de la app).
    private bool _logoProvisioned;

    // Documentos disponibles: cada entrada es un JSON integrado completo (formato "integrated").
    // No hay diccionario de datos — todos los valores ya están resueltos en el JSON.
    private readonly (string Name, string IntegratedJson)[] _documents;

    public MainPage(IDocumentEngine engine, IThermalPrinterService printer, IDiagnosticsReportProvider diagnostics, IBitmapRasterizer rasterizer)
    {
        InitializeComponent();
        _engine = engine;
        _printer = printer;
        _diagnostics = diagnostics;
        _rasterizer = rasterizer;

        _documents = new[]
        {
            ("Acta de Infracción (integrada)", MultaIntegratedDsl.Document),
            ("Ticket Simple de Multa (integrado)", TicketSimpleIntegratedDsl.Document),
            ("Comprobante de Pago (integrado)", ComprobanteIntegratedDsl.Document),
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        StatusBadge.Service = _printer;
        DevicePicker.Service = _printer;
        DevicePicker.DeviceSelected += (_, device) => ShowMessage($"Conectado a {device.Name}.");
        DevicePicker.ScanError += (_, ex) => ShowMessage($"BT Error: {ex.Message}");

#if ANDROID
        var granted = await RequestBluetoothPermissions();
        if (granted)
            await DevicePicker.ScanAsync();
        else
            ShowMessage("Permisos BT denegados.");
#elif IOS
        await Task.CompletedTask;
#else
        await DevicePicker.ScanAsync();
#endif
    }

    // ─── Bluetooth Permissions (Android 12+) ───

#if ANDROID
    private async Task<bool> RequestBluetoothPermissions()
    {
        try
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
            {
                var activity = Platform.CurrentActivity!;
                string[] btPermissions = new[]
                {
                    Manifest.Permission.BluetoothScan,
                    Manifest.Permission.BluetoothConnect
                };

                bool allGranted = btPermissions.All(p =>
                    ContextCompat.CheckSelfPermission(activity, p) == (int)Android.Content.PM.Permission.Granted);

                if (!allGranted)
                {
                    ActivityCompat.RequestPermissions(activity, btPermissions, 1);
                    await Task.Delay(3000);

                    allGranted = btPermissions.All(p =>
                        ContextCompat.CheckSelfPermission(activity, p) == (int)Android.Content.PM.Permission.Granted);
                }

                if (!allGranted)
                {
                    ShowMessage("Permisos BT denegados. Aceptá los permisos y presioná Reescanear.");
                    return false;
                }
            }
            else
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    ShowMessage("Permisos de ubicación denegados (necesarios para BT en Android < 12).");
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MULTA] BT Permissions error: {ex.Message}");
            ShowMessage($"Error permisos BT: {ex.Message}");
            return false;
        }
    }
#endif

    // ─── Reescanear manual ───

    private async void OnReescanearClicked(object? sender, EventArgs e)
    {
#if ANDROID
        var granted = await RequestBluetoothPermissions();
        if (granted)
            await DevicePicker.ScanAsync();
#else
        await DevicePicker.ScanAsync();
#endif
    }

    // ─── Selector de documento ───

    private void OnDocPickerChanged(object? sender, EventArgs e)
    {
        PreviewLabel.Text = "Presioná 'Vista Previa' para ver el documento.";
    }

    private string? GetSelectedDocument()
    {
        var idx = DocPicker.SelectedIndex;
        if (idx < 0 || idx >= _documents.Length)
        {
            ShowMessage("Seleccioná un documento primero.");
            return null;
        }
        return _documents[idx].IntegratedJson;
    }

    // ─── Vista Previa (texto) ───

    private void OnVistaPreviewClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("[MULTA-INT] OnVistaPrevia iniciado");
        var doc = GetSelectedDocument();
        if (doc == null) return;

        try
        {
            var profile = new DeviceProfile("thermal_58mm", 32, "text");
            Console.WriteLine("[MULTA-INT] Llamando engine.Render(json, profile)...");
            var result = _engine.Render(doc, profile);
            Console.WriteLine($"[MULTA-INT] Render OK. IsSuccessful={result.IsSuccessful}");

            if (result.IsSuccessful)
            {
                PreviewLabel.Text = result.Output?.ToString() ?? "(vacío)";
                ShowMessage("Vista previa generada.");
            }
            else
            {
                PreviewLabel.Text = "ERRORES:\n" + string.Join("\n", result.Errors);
                ShowMessage("Error al generar preview.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MULTA] Error: {ex.Message}\n{ex.StackTrace}");
            PreviewLabel.Text = $"Error: {ex.Message}";
            ShowMessage("Excepción al generar preview.");
        }
    }

    // ─── Vista Pixelada (raster preview) ───

    private void OnVistaPixeladaClicked(object? sender, EventArgs e)
    {
        var doc = GetSelectedDocument();
        if (doc == null) return;

        try
        {
            var profile = new DeviceProfile("preview", 32, "raster-preview");
            profile.SetCapability("bitmap_max_width_px", 384);
            var result = _engine.Render(doc, profile);
            if (result.IsSuccessful && result.Output is byte[] bytes)
            {
                RasterPreview.ImageBytes = bytes;
                RasterFrame.IsVisible = true;
                ShowMessage($"Vista pixelada generada — {bytes.Length} bytes PNG");
            }
            else
            {
                RasterFrame.IsVisible = false;
                ShowMessage("Error vista pixelada: " + string.Join("; ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            RasterFrame.IsVisible = false;
            ShowMessage($"Excepción vista pixelada: {ex.Message}");
        }
    }

    // ─── Imprimir ESC/POS ───

    private async void OnImprimirClicked(object? sender, EventArgs e)
    {
        MessageLabel.Text = "Iniciando impresión...";
        var doc = GetSelectedDocument();
        if (doc == null) return;

        try
        {
            // ── 1. Render SIEMPRE primero (diagnóstico independiente de impresora) ──
            // Conectar -> leer capacidades -> elegir estrategia (la libreria recomienda, la app
            // decide). Con NV soportado Y logo aprovisionado: "escpos" (logo por recall, firmas
            // inline). Si no: "escpos-bitmap" (rasteriza todo). Sin conexion, caps es null ->
            // bitmap, por eso el render sigue funcionando offline.
            var caps = _printer.CurrentCapabilities;
            var target = PrintStrategySelector.RecommendTarget(caps, docNeedsGraphics: true);

            DeviceProfile profile;
            if (target == "escpos" && _logoProvisioned && caps?.NvGraphicsKind != null)
            {
                profile = new DeviceProfile("58HB6", 32, "escpos");
                // Para FS el define crea la imagen #1; el recall usa ese indice. Para GS ( L, el keycode.
                int recallKey = caps.NvGraphicsKind == "fs" ? 1 : LOGO_KEYCODE;
                profile.SetCapability("nv_logo_keycode", recallKey);
                profile.SetCapability("nv_logo_kind", caps.NvGraphicsKind);
                profile.SetCapability("supports_bitmap", true); // firmas inline (signature)
                profile.SetCapability("bitmap_max_width_px", 320);
                profile.SetCapability("bitmap_binarization_threshold", 128);
                ShowMessage($"Generando ESC/POS nativo (logo NV {caps.NvGraphicsKind})...");
            }
            else
            {
                profile = new DeviceProfile("58HB6", 32, "escpos-bitmap");
                profile.SetCapability("supports_bitmap", true);
                profile.SetCapability("bitmap_max_width_px", 320);
                profile.SetCapability("bitmap_binarization_threshold", 128);
                ShowMessage("Generando ESC/POS bitmap completo...");
            }
            var result = _engine.Render(doc, profile);

            if (!result.IsSuccessful || result.Output is not byte[] bytes)
            {
                var firstErr = result.Errors.FirstOrDefault() ?? "sin errores";
                var snippet = firstErr.Length > 200 ? firstErr[..200] : firstErr;
                MessageLabel.Text = $"{DateTime.Now:HH:mm:ss} RENDER FALLÓ:\n{snippet}";
                System.Console.WriteLine($"[MULTA-RENDER-ERROR] {firstErr}");
                foreach (var w in result.Warnings)
                    System.Console.WriteLine($"[MULTA-WARN] {w}");
                return;
            }

            ShowMessage($"Render OK — {bytes.Length} bytes");
            System.Console.WriteLine($"[MULTA] Render OK: {bytes.Length} bytes");

            // ── 2. Verificar impresora antes de enviar ──
            if (!_printer.IsConnected)
            {
                ShowMessage($"Render OK ({bytes.Length} bytes) pero no hay impresora conectada.");
                return;
            }

            await Task.Delay(500);
            await _printer.SendBytesAsync(bytes);
            ShowMessage($"Impreso OK — {bytes.Length} bytes enviados.");
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException;
            var msg = $"TIPO: {ex.GetType().Name}\n" +
                      $"MSG: {ex.Message}\n" +
                      $"INNER: {inner?.GetType().Name}: {inner?.Message}\n" +
                      $"INNER2: {inner?.InnerException?.Message}\n" +
                      $"STACK: {ex.StackTrace?.Replace("\n", " | ")?.Substring(0, Math.Min(200, ex.StackTrace?.Length ?? 0))}";
            MessageLabel.Text = $"{DateTime.Now:HH:mm:ss} — {msg}";
            System.Console.WriteLine($"[MULTA-ERROR] {msg}");
        }
    }

    // ─── Logo NV: aprovisionar / limpiar ───

    private async void OnConfigurarLogoClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!_printer.IsConnected)
            {
                ShowMessage("Conectá una impresora antes de configurar el logo.");
                return;
            }

            // Rasteriza el logo de ejemplo (via el IBitmapRasterizer que ya inyecta la app) y lo
            // arma como GS v 0 (el formato inline que el transport acepta para aprovisionar NV).
            var rast = _rasterizer.Rasterize("data:image/bmp;base64," + MultaIntegratedDsl.LogoBase64, 320);
            var gsV0 = EscPosCommands.BuildRasterImageGsV0(rast.Bits, rast.WidthBytes, rast.HeightDots);

            var result = await _printer.ProvisionLogoAsync(gsV0, LOGO_KEYCODE);
            _logoProvisioned = result.Success;
            ShowMessage($"Logo NV: Success={result.Success} Kind={result.Kind ?? "-"} — {result.Message}");
        }
        catch (Exception ex)
        {
            _logoProvisioned = false;
            ShowMessage($"Error configurando logo: {ex.Message}");
        }
    }

    private async void OnLimpiarLogoClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!_printer.IsConnected)
            {
                ShowMessage("Conectá una impresora antes de limpiar el logo.");
                return;
            }
            await _printer.ClearLogoAsync(LOGO_KEYCODE);
            _logoProvisioned = false;
            ShowMessage("Logo NV limpiado.");
        }
        catch (Exception ex)
        {
            ShowMessage($"Error limpiando logo: {ex.Message}");
        }
    }

    // ─── Ver PDF ───

#if ANDROID
    private async void OnVerPdfClicked(object? sender, EventArgs e)
    {
        try
        {
            var doc = GetSelectedDocument();
            if (doc == null)
            {
                ShowMessage("Seleccioná un documento primero.");
                return;
            }
            var profile = new DeviceProfile("pdf", 48, "pdf");
            var result = _engine.Render(doc, profile);
            if (result.IsSuccessful && result.Output is byte[] pdfBytes)
            {
                var path = Path.Combine(FileSystem.CacheDirectory, "multa-integrada.pdf");
                File.WriteAllBytes(path, pdfBytes);
                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(path)
                });
                ShowMessage($"PDF generado y abierto: {path}");
            }
            else
            {
                ShowMessage("Error PDF: " + string.Join("; ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Error PDF: {ex.Message}");
        }
    }
#endif

    // ─── Diagnóstico ───

    private void OnDiagnosticoVerClicked(object? sender, EventArgs e)
    {
        try
        {
            var report = _diagnostics.Build(notes: "captura manual desde sample");
            var dsl = _diagnostics.ToDslJson(report, paperWidthChars: 32);
            var profile = new DeviceProfile("preview", 32, "raster-preview");
            profile.SetCapability("bitmap_max_width_px", 384);
            var result = _engine.Render(dsl, profile);
            if (result.IsSuccessful && result.Output is byte[] bytes)
            {
                RasterPreview.ImageBytes = bytes;
                RasterFrame.IsVisible = true;
                ShowMessage($"Diagnóstico generado — {bytes.Length} bytes PNG");
            }
            else
            {
                ShowMessage("Error diagnóstico: " + string.Join("; ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Excepción diagnóstico: {ex.Message}");
        }
    }

    private async void OnDiagnosticoImprimirClicked(object? sender, EventArgs e)
    {
        try
        {
            var report = _diagnostics.Build(notes: "captura manual — botón Imprimir Diag");
            var dsl = _diagnostics.ToDslJson(report, paperWidthChars: 32);
            var profile = new DeviceProfile("58HB6", 32, "escpos-bitmap");
            profile.SetCapability("supports_bitmap", true);
            profile.SetCapability("bitmap_max_width_px", 320);
            var result = _engine.Render(dsl, profile);
            if (!result.IsSuccessful || result.Output is not byte[] bytes)
            {
                ShowMessage("Render diag falló: " + string.Join("; ", result.Errors));
                return;
            }
            if (!_printer.IsConnected)
            {
                ShowMessage($"Render OK ({bytes.Length} bytes) pero no hay impresora conectada.");
                return;
            }
            await _printer.SendBytesAsync(bytes);
            ShowMessage($"Diagnóstico impreso — {bytes.Length} bytes");
        }
        catch (Exception ex)
        {
            ShowMessage($"Error imprimir diag: {ex.Message}");
        }
    }

    private async void OnDiagnosticoReportarClicked(object? sender, EventArgs e)
    {
        try
        {
            var report = _diagnostics.Build(notes: "Reporte de fallo — usuario inició desde botón");
            var text = _diagnostics.ToPlainText(report);
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = "Reporte de fallo MotorDsl",
                Text = text
            });
        }
        catch (Exception ex)
        {
            ShowMessage($"Error reporte: {ex.Message}");
        }
    }

    // ─── Helper ───

    private void ShowMessage(string msg)
    {
        MessageLabel.Text = $"{DateTime.Now:HH:mm:ss} — {msg}";
    }
}
