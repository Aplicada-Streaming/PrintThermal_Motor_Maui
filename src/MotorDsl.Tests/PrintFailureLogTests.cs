using MotorDsl.Core.Models;
using MotorDsl.Core.Printing;
using MotorDsl.Printing;

namespace MotorDsl.Tests;

/// <summary>
/// Cubre el ring buffer de fallos de ThermalPrinterService: acotamiento a MAX_FAILURES, orden,
/// ClearFailures, concurrencia, y el registro de UNA entrada (sin payload) al agotar el retry.
/// </summary>
public class PrintFailureLogTests
{
    private static ThermalPrinterService NewService()
        => new(new[] { new FakeTransport() }, new DefaultPrintErrorHandler());

    private static PrintFailureEntry MakeEntry(int i)
        => new(DateTimeOffset.Now, $"dev-{i}", $"name-{i}", "fake",
               PrinterCapabilities.Unknown(), "Connection", $"err-{i}", 1, 10, null);

    [Fact]
    public void RingBuffer_Bounded_To_Max_DiscardsOldest_InOrder()
    {
        var svc = NewService();
        for (int i = 0; i < 25; i++)
            svc.RecordFailure(MakeEntry(i));

        var failures = svc.RecentFailures;
        Assert.Equal(20, failures.Count);
        Assert.Equal("dev-5", failures[0].DeviceId);    // i=0..4 descartados (mas viejos)
        Assert.Equal("dev-24", failures[^1].DeviceId);  // mas nuevo al final
    }

    [Fact]
    public void ClearFailures_Empties_TheBuffer()
    {
        var svc = NewService();
        svc.RecordFailure(MakeEntry(1));
        svc.RecordFailure(MakeEntry(2));
        Assert.NotEmpty(svc.RecentFailures);

        svc.ClearFailures();
        Assert.Empty(svc.RecentFailures);
    }

    [Fact]
    public void RingBuffer_Concurrent_Adds_DoNotCorrupt_AndStayBounded()
    {
        var svc = NewService();
        Parallel.For(0, 500, i => svc.RecordFailure(MakeEntry(i)));
        Assert.Equal(20, svc.RecentFailures.Count);
    }

    [Fact]
    public async Task SendBytes_Failure_Adds_Exactly_One_Entry_WithoutPayload()
    {
        var transport = new FakeTransport { ThrowOnWrite = true };
        var svc = new ThermalPrinterService(new[] { transport }, new DefaultPrintErrorHandler());
        await svc.ConnectAsync(new PrinterDevice("00:11:22:33:44:55", "Fake", "fake"));

        // MaxRetries=1 + delay 0: falla rapido en un solo intento.
        var retry = new PrintRetryOptions { MaxRetries = 1, InitialDelayMs = 0 };
        await Assert.ThrowsAsync<Exception>(
            () => svc.SendBytesAsync(new byte[] { 1, 2, 3, 4 }, retry: retry));

        Assert.Single(svc.RecentFailures);
        var entry = svc.RecentFailures[0];
        Assert.Equal(4, entry.BytesLength);   // longitud, NUNCA el payload
        Assert.Equal("fake", entry.DeviceKind);
        Assert.Equal(1, entry.Attempts);
        Assert.Null(entry.RenderTarget);
    }
}
