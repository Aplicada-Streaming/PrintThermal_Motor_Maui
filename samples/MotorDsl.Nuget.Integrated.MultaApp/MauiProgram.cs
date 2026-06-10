using Microsoft.Extensions.Logging;
using MotorDsl.Bluetooth;
using MotorDsl.Core.Models;
using MotorDsl.Extensions;
using MotorDsl.Maui;
using MotorDsl.Nuget.Integrated.MultaApp.Pages;
using MotorDsl.Nuget.Integrated.MultaApp.Templates;
using MotorDsl.Printing;

// using QuestPDF.Infrastructure;

namespace MotorDsl.Nuget.Integrated.MultaApp;

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

        // Motor DSL: core pipeline + templates + profiles + renderers MAUI (PDF, ESC/POS bitmap, SkiaSharp).
        // El template registrado es un JSON integrado: ya tiene todos los valores resueltos.
        builder.Services.AddMotorDslEngine()
            .AddTemplates(t =>
            {
                t.Add("acta-infraccion-integrada", MultaIntegratedDsl.Document);
            })
            .AddProfiles(p =>
            {
                p.Add(new DeviceProfile("thermal_58mm", 32, "escpos-bitmap"));
                p.Add(new DeviceProfile("preview", 32, "raster-preview"));
                p.Add(new DeviceProfile("a4-pdf", 80, "pdf"));
                p.Add(new DeviceProfile("pdf", 48, "pdf"));
            })
            .AddMotorDslMaui();

        // Transport Bluetooth (Android Classic SPP)
        builder.Services.AddBluetoothPrinterTransport();

        // Servicios de la app
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
