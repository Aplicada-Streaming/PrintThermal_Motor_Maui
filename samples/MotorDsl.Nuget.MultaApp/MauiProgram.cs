using Microsoft.Extensions.Logging;
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;
using MotorDsl.Core.Printing;
using MotorDsl.Extensions;
using MotorDsl.Nuget.MultaApp.Pages;
using MotorDsl.Nuget.MultaApp.Renderers;
using MotorDsl.Nuget.MultaApp.Services;
using MotorDsl.Nuget.MultaApp.Templates;

// using QuestPDF.Infrastructure;

namespace MotorDsl.Nuget.MultaApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Motor DSL: core pipeline + templates + profiles + custom renderers
        builder.Services.AddMotorDslEngine()
            .AddTemplates(t =>
            {
                t.Add("acta-infraccion", MultaDsl.Template);
                t.Add("ticket-simple", TicketSimpleDsl.Template);
                t.Add("comprobante-pago", ComprobanteDsl.Template);
            })
            .AddProfiles(p =>
            {
                p.Add(new DeviceProfile("thermal_58mm", 32, "escpos-bitmap"));
                p.Add(new DeviceProfile("a4-pdf", 80, "pdf"));
                p.Add(new DeviceProfile("pdf", 48, "pdf"));
            })
            .AddRenderer<PdfRenderer>()
            .AddRenderer<BitmapEscPosRenderer>();

        // Bitmap rasterizer (SkiaSharp)
        builder.Services.AddSingleton<IBitmapRasterizer, SkiaSharpRasterizer>();

        // Servicios de la app
        builder.Services.AddSingleton<IPrintErrorHandler, DefaultPrintErrorHandler>();
        builder.Services.AddSingleton<IThermalPrinterService, ThermalPrinterService>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
