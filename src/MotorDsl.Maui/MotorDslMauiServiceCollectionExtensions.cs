using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MotorDsl.Core.Contracts;
using MotorDsl.Extensions;
using MotorDsl.Maui.Diagnostics;
using MotorDsl.Maui.Printing;
using MotorDsl.Maui.Renderers;
using MotorDsl.Printing;

namespace MotorDsl.Maui;

public static class MotorDslMauiServiceCollectionExtensions
{
    /// <summary>
    /// Registra renderers MAUI (PDF + ESC/POS bitmap), bitmap rasterizer (SkiaSharp)
    /// y el orquestador IThermalPrinterService. Llama internamente a AddMotorDslPrinting.
    /// Reemplaza el IPrintErrorHandler por <see cref="MauiPrintErrorHandler"/> y
    /// registra <see cref="MauiDiagnosticsReportProvider"/> como
    /// <see cref="IDiagnosticsReportProvider"/> (vía TryAddSingleton para que el
    /// consumer pueda registrar su propio provider antes y prevalezca).
    /// </summary>
    public static MotorDslBuilder AddMotorDslMaui(this MotorDslBuilder builder)
    {
        builder.Services.AddSingleton<IBitmapRasterizer, SkiaSharpRasterizer>();
        builder.Services.AddSingleton<QrCodeRasterizer>();
        builder.Services.AddMotorDslPrinting();
        builder.Services.Replace(ServiceDescriptor.Singleton<IPrintErrorHandler, MauiPrintErrorHandler>());
        builder.Services.TryAddSingleton<IDiagnosticsReportProvider, MauiDiagnosticsReportProvider>();
        builder.AddRenderer<PdfRenderer>();
        builder.AddRenderer<BitmapEscPosRenderer>();
        builder.AddRenderer<RasterPreviewRenderer>();
        return builder;
    }
}
