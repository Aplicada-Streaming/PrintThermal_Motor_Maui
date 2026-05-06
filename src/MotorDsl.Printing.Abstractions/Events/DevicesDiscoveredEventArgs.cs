namespace MotorDsl.Printing;

public class DevicesDiscoveredEventArgs : EventArgs
{
    public IReadOnlyList<PrinterDevice> Devices { get; }
    public DevicesDiscoveredEventArgs(IReadOnlyList<PrinterDevice> devices) => Devices = devices;
}
