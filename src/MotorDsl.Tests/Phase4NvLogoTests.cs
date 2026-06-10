using MotorDsl.Bluetooth;
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Layout;
using MotorDsl.Core.Models;
using MotorDsl.Printing;
using MotorDsl.Rendering;

namespace MotorDsl.Tests;

/// <summary>
/// Fase 4: parseo GS v 0, builders de recall NV, propagacion de rol de imagen, ruteo del renderer
/// por rol (logo recall / inline, firmas siempre inline) y seleccion de estrategia.
/// </summary>
public class Phase4NvLogoTests
{
    private readonly ILayoutEngine _layoutEngine = new LayoutEngine();

    private static EvaluatedDocument Evaluated(DocumentNode root)
        => new() { Id = "test", Version = "1.0", Root = root };

    private LayoutedDocument Layout(DocumentNode root, DeviceProfile profile)
        => _layoutEngine.ApplyLayout(Evaluated(root), profile);

    private static DeviceProfile EscPosProfile() => new("p", 32, "escpos");

    private const string Png = "data:image/png;base64,iVBORw0KGgo=";

    // ─── GS v 0 parse round-trip ───

    [Fact]
    public void ParseGsV0_RoundTrips_Dimensions_And_Bits()
    {
        var bits = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 }; // 3 bytes/fila x 2 dots
        var gsv0 = EscPosCommands.BuildRasterImageGsV0(bits, widthBytes: 3, heightDots: 2);

        var parsed = BluetoothPrinterTransport.ParseGsV0(gsv0);

        Assert.NotNull(parsed);
        Assert.Equal(3, parsed!.Value.widthBytes);
        Assert.Equal(2, parsed.Value.heightDots);
        Assert.Equal(bits, parsed.Value.bits);
    }

    [Fact]
    public void ParseGsV0_BadHeader_ReturnsNull()
    {
        Assert.Null(BluetoothPrinterTransport.ParseGsV0(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 }));
        Assert.Null(BluetoothPrinterTransport.ParseGsV0(new byte[] { 0x1D, 0x76, 0x30 })); // sin header completo
    }

    // ─── Recall builders ───

    [Fact]
    public void RecallNvGraphicsGsL_BuildsExpectedSequence()
    {
        // keycode 0x4142 -> kc1=0x42, kc2=0x41
        var cmd = EscPosCommands.RecallNvGraphicsGsL(0x4142);
        Assert.Equal(new byte[] { 0x1D, 0x28, 0x4C, 0x06, 0x00, 0x30, 0x45, 0x42, 0x41, 0x01, 0x01 }, cmd);
    }

    [Fact]
    public void RecallNvBitImageFsP_BuildsExpectedSequence()
    {
        var cmd = EscPosCommands.RecallNvBitImageFsP(1);
        Assert.Equal(new byte[] { 0x1C, 0x70, 0x01, 0x00 }, cmd);
    }

    // ─── Propagacion de rol a DeviceMetadata ───

    [Theory]
    [InlineData("logo")]
    [InlineData("signature")]
    public void Role_Propagates_To_DeviceMetadata(string role)
    {
        var node = new ImageNode(Png, role: role);
        var layouted = Layout(node, EscPosProfile());

        var info = layouted.NodeLayoutInfo.Values.First();
        Assert.True(info.DeviceMetadata.TryGetValue("image_role", out var r));
        Assert.Equal(role, r);
    }

    [Fact]
    public void NoRole_DoesNotSet_ImageRole()
    {
        var node = new ImageNode(Png); // sin rol
        var layouted = Layout(node, EscPosProfile());

        var info = layouted.NodeLayoutInfo.Values.First();
        Assert.False(info.DeviceMetadata.ContainsKey("image_role"));
    }

    // ─── Ruteo del renderer por rol ───

    private static readonly byte[] GsV0Header = { 0x1D, 0x76, 0x30 };
    private static readonly byte[] GsLRecallHeader = { 0x1D, 0x28, 0x4C };

    [Fact]
    public void Logo_WithNvKeycode_EmitsRecall_NotGsV0()
    {
        var profile = EscPosProfile();
        profile.SetCapability("nv_logo_keycode", 0x4142);
        profile.SetCapability("nv_logo_kind", "gsl");

        var renderer = new EscPosRenderer(new FakeRasterizer());
        var layouted = Layout(new ImageNode(Png, role: "logo"), profile);
        var bytes = (byte[])renderer.Render(layouted, profile).Output!;

        Assert.True(Contains(bytes, GsLRecallHeader), "logo+keycode debe emitir recall GS ( L");
        Assert.False(Contains(bytes, GsV0Header), "logo+keycode NO debe rasterizar (sin GS v 0)");
    }

    [Fact]
    public void Logo_WithFsKind_EmitsFsRecall()
    {
        var profile = EscPosProfile();
        profile.SetCapability("nv_logo_keycode", 1);
        profile.SetCapability("nv_logo_kind", "fs");

        var renderer = new EscPosRenderer(new FakeRasterizer());
        var layouted = Layout(new ImageNode(Png, role: "logo"), profile);
        var bytes = (byte[])renderer.Render(layouted, profile).Output!;

        Assert.True(Contains(bytes, new byte[] { 0x1C, 0x70, 0x01, 0x00 }), "kind fs debe emitir FS p");
        Assert.False(Contains(bytes, GsV0Header));
    }

    [Fact]
    public void Logo_WithoutNvKeycode_EmitsInlineGsV0()
    {
        var profile = EscPosProfile(); // sin nv_logo_keycode -> degradacion a inline

        var renderer = new EscPosRenderer(new FakeRasterizer());
        var layouted = Layout(new ImageNode(Png, role: "logo"), profile);
        var bytes = (byte[])renderer.Render(layouted, profile).Output!;

        Assert.True(Contains(bytes, GsV0Header), "sin keycode el logo va inline (GS v 0)");
        Assert.False(Contains(bytes, GsLRecallHeader));
    }

    [Fact]
    public void Signature_AlwaysEmitsInlineGsV0_EvenWithNvKeycode()
    {
        var profile = EscPosProfile();
        profile.SetCapability("nv_logo_keycode", 0x4142); // aunque haya keycode...
        profile.SetCapability("nv_logo_kind", "gsl");

        var renderer = new EscPosRenderer(new FakeRasterizer());
        var layouted = Layout(new ImageNode(Png, role: "signature"), profile);
        var bytes = (byte[])renderer.Render(layouted, profile).Output!;

        Assert.True(Contains(bytes, GsV0Header), "las firmas SIEMPRE van inline (GS v 0)");
        Assert.False(Contains(bytes, GsLRecallHeader), "una firma no debe salir por recall NV");
    }

    // ─── Seleccion de estrategia ───

    [Fact]
    public void RecommendTarget_NvSupported_IsEscPos()
    {
        var caps = new PrinterCapabilities(CapabilitySupport.Supported, CapabilitySupport.Supported, "gsl", null);
        Assert.Equal("escpos", PrintStrategySelector.RecommendTarget(caps, docNeedsGraphics: true));
    }

    [Theory]
    [InlineData(CapabilitySupport.NotDetected)]
    [InlineData(CapabilitySupport.Unknown)]
    public void RecommendTarget_NvNotSupported_IsEscPosBitmap(CapabilitySupport nv)
    {
        var caps = new PrinterCapabilities(CapabilitySupport.Supported, nv, null, null);
        Assert.Equal("escpos-bitmap", PrintStrategySelector.RecommendTarget(caps, docNeedsGraphics: true));
    }

    [Fact]
    public void RecommendTarget_NullCaps_IsEscPosBitmap()
        => Assert.Equal("escpos-bitmap", PrintStrategySelector.RecommendTarget(null, docNeedsGraphics: true));

    // ─── Helper ───

    private static bool Contains(byte[] source, byte[] pattern)
    {
        for (int i = 0; i <= source.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
                if (source[i + j] != pattern[j]) { match = false; break; }
            if (match) return true;
        }
        return false;
    }
}
