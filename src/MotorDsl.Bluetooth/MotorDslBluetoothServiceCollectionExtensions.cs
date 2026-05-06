using Microsoft.Extensions.DependencyInjection;
using MotorDsl.Printing;

namespace MotorDsl.Bluetooth;

public static class MotorDslBluetoothServiceCollectionExtensions
{
    public static IServiceCollection AddBluetoothPrinterTransport(this IServiceCollection services)
    {
        services.AddSingleton<IThermalPrinterTransport, BluetoothPrinterTransport>();
        return services;
    }
}
