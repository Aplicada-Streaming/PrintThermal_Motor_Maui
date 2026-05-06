namespace MotorDsl.Printing;

public record PrinterDevice(string Id, string Name, string Kind, bool IsPaired = false)
{
    public override string ToString() => $"{Name} ({Id})";
}
