namespace MotorDsl.Rendering;

/// <summary>
/// ESC/POS command constants as byte arrays.
/// Sprint 04 | TK-21
/// Reference: ESC/POS Application Programming Guide
/// </summary>
public static class EscPosCommands
{
    /// <summary>ESC @ — Initialize printer</summary>
    public static readonly byte[] Init = { 0x1B, 0x40 };

    /// <summary>ESC a 0 — Align left</summary>
    public static readonly byte[] AlignLeft = { 0x1B, 0x61, 0x00 };

    /// <summary>ESC a 1 — Align center</summary>
    public static readonly byte[] AlignCenter = { 0x1B, 0x61, 0x01 };

    /// <summary>ESC a 2 — Align right</summary>
    public static readonly byte[] AlignRight = { 0x1B, 0x61, 0x02 };

    /// <summary>ESC ! 0 — Normal style (reset)</summary>
    public static readonly byte[] StyleNormal = { 0x1B, 0x21, 0x00 };

    /// <summary>ESC ! 8 — Bold on</summary>
    public static readonly byte[] StyleBold = { 0x1B, 0x21, 0x08 };

    /// <summary>GS V 0 — Full cut</summary>
    public static readonly byte[] CutFull = { 0x1D, 0x56, 0x00 };

    /// <summary>GS V 1 — Partial cut</summary>
    public static readonly byte[] CutPartial = { 0x1D, 0x56, 0x01 };

    /// <summary>LF — Line feed</summary>
    public static readonly byte[] LineFeed = { 0x0A };

    /// <summary>ESC d n — Feed n lines</summary>
    public static byte[] FeedLines(byte n) => new byte[] { 0x1B, 0x64, n };

    // ─── Barcode EAN-13 — GS k ───

    /// <summary>GS k 2 — Print barcode EAN-13 (mode 2, NUL-terminated)</summary>
    public static readonly byte[] BarcodeEan13 = { 0x1D, 0x6B, 0x02 };

    // ─── QR Code — GS ( k ───

    /// <summary>GS ( k — Set QR model 2</summary>
    public static readonly byte[] QrStoreData = { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x50, 0x30 };

    /// <summary>GS ( k — Set QR module size to 3</summary>
    public static readonly byte[] QrSetSize3 = { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x03 };

    /// <summary>GS ( k — Set QR error correction level M</summary>
    public static readonly byte[] QrSetErrorM = { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x33 };

    /// <summary>GS ( k — Print QR code</summary>
    public static readonly byte[] QrPrint = { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 };

    // ─── Raster image — GS v 0 ───

    /// <summary>
    /// GS v 0 (0x1D 0x76 0x30) — Raster bit image. Header fijo de 8 bytes: m=0 (densidad
    /// normal), xL xH = bytes por fila (widthBytes), yL yH = alto en dots, seguido de los bits.
    /// Es el formato inline que la app produce y que el transport acepta para aprovisionar NV.
    /// </summary>
    public static byte[] BuildRasterImageGsV0(byte[] bits, int widthBytes, int heightDots)
    {
        var cmd = new byte[8 + bits.Length];
        cmd[0] = 0x1D; cmd[1] = 0x76; cmd[2] = 0x30; cmd[3] = 0x00; // GS v 0, m=0
        cmd[4] = (byte)(widthBytes & 0xFF);
        cmd[5] = (byte)((widthBytes >> 8) & 0xFF);
        cmd[6] = (byte)(heightDots & 0xFF);
        cmd[7] = (byte)((heightDots >> 8) & 0xFF);
        Array.Copy(bits, 0, cmd, 8, bits.Length);
        return cmd;
    }

    // ─── Recall de logo en NV por keycode (solo recall; define/clear/query viven en el transport) ───

    /// <summary>
    /// GS ( L funcion 69 (0x45) — imprimir NV graphics por keycode, escala x=y=1.
    /// Formato: GS ( L pL pH m fn kc1 kc2 x y.
    /// </summary>
    public static byte[] RecallNvGraphicsGsL(int keycode)
    {
        byte kc1 = (byte)(keycode & 0xFF);
        byte kc2 = (byte)((keycode >> 8) & 0xFF);
        return new byte[] { 0x1D, 0x28, 0x4C, 0x06, 0x00, 0x30, 0x45, kc1, kc2, 0x01, 0x01 };
    }

    /// <summary>
    /// FS p n m (0x1C 0x70) — imprimir la NV bit image clasica numero n con modo m (0=normal).
    /// </summary>
    public static byte[] RecallNvBitImageFsP(int imageNumber, byte mode = 0x00)
        => new byte[] { 0x1C, 0x70, (byte)imageNumber, mode };
}
