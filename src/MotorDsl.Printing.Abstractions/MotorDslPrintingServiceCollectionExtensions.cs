using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Printing;

namespace MotorDsl.Printing;

public static class MotorDslPrintingServiceCollectionExtensions
{
    public static IServiceCollection AddMotorDslPrinting(this IServiceCollection services)
    {
        services.TryAddSingleton<IPrintErrorHandler, DefaultPrintErrorHandler>();
        services.TryAddSingleton<IThermalPrinterService, ThermalPrinterService>();
        return services;
    }
}
