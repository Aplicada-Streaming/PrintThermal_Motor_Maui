using MotorDsl.Printing;

namespace MotorDsl.Tests;

/// <summary>
/// Cubre DiagnosticsBuilder, que es la parte pura y testeable de lo que arma Build() del
/// provider de Maui: mapeo de capabilities, snapshot de impresora y lista de fallos con
/// enmascarado de MAC segun includePii.
/// </summary>
public class DiagnosticsBuilderTests
{
    private const string Mac = "AA:BB:CC:DD:EE:FF";
    private const string MaskedMac = "AA:BB:CC**:**:**";

    [Fact]
    public void BuildPrinterSnapshot_MapsCapabilities_AndMasksMac_WhenNoPii()
    {
        var svc = new FakePrinterService
        {
            CurrentDevice = new PrinterDevice(Mac, "Printer58", "bluetooth"),
            ConnectionState = PrinterConnectionState.Connected,
            CurrentCapabilities = new PrinterCapabilities(
                CapabilitySupport.Supported, CapabilitySupport.NotDetected, "gsl", "MODEL-X")
        };

        var snap = DiagnosticsBuilder.BuildPrinterSnapshot(svc, includePii: false);

        Assert.NotNull(snap);
        Assert.Equal(MaskedMac, snap!.Id);
        Assert.Equal("supported", snap.Capabilities["supports_status_feedback"]);
        Assert.Equal("not_detected", snap.Capabilities["supports_nv_graphics"]);
        Assert.Equal("gsl", snap.Capabilities["nv_graphics_kind"]);
        Assert.Equal("MODEL-X", snap.Capabilities["model_id"]);
    }

    [Fact]
    public void BuildPrinterSnapshot_KeepsRawId_WhenPii()
    {
        var svc = new FakePrinterService
        {
            CurrentDevice = new PrinterDevice(Mac, "P", "bluetooth")
        };

        var snap = DiagnosticsBuilder.BuildPrinterSnapshot(svc, includePii: true);

        Assert.NotNull(snap);
        Assert.Equal(Mac, snap!.Id);
    }

    [Fact]
    public void BuildPrinterSnapshot_NullCapabilities_AreAllUnknown()
    {
        var svc = new FakePrinterService
        {
            CurrentDevice = new PrinterDevice(Mac, "P", "bluetooth"),
            CurrentCapabilities = null
        };

        var snap = DiagnosticsBuilder.BuildPrinterSnapshot(svc, includePii: false);

        Assert.Equal("unknown", snap!.Capabilities["supports_status_feedback"]);
        Assert.Equal("unknown", snap.Capabilities["supports_nv_graphics"]);
        Assert.Equal("(none)", snap.Capabilities["nv_graphics_kind"]);
        Assert.Equal("(unknown)", snap.Capabilities["model_id"]);
    }

    [Fact]
    public void BuildFailures_MasksDeviceId_WhenNoPii_KeepingRest()
    {
        var svc = new FakePrinterService
        {
            RecentFailures = new[]
            {
                new PrintFailureEntry(DateTimeOffset.Now, Mac, "P", "bluetooth",
                    PrinterCapabilities.Unknown(), "Connection", "boom", 3, 100, null)
            }
        };

        var failures = DiagnosticsBuilder.BuildFailures(svc, includePii: false);

        Assert.NotNull(failures);
        Assert.Single(failures!);
        Assert.Equal(MaskedMac, failures![0].DeviceId);
        Assert.Equal("Connection", failures[0].ErrorType);  // el resto queda intacto
        Assert.Equal(100, failures[0].BytesLength);
    }

    [Fact]
    public void BuildFailures_Null_WhenNoFailures()
    {
        var svc = new FakePrinterService();
        Assert.Null(DiagnosticsBuilder.BuildFailures(svc, includePii: false));
    }
}
