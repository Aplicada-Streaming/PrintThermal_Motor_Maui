using QRCoder;
using SkiaSharp;

namespace MotorDsl.Maui.Renderers;

/// <summary>
/// Rasteriza datos como código QR a SKBitmap, replicando el aspecto que
/// produciría una impresora térmica (módulos 1-bit, sin antialias).
/// Reutilizable por PdfRenderer y RasterPreviewRenderer.
/// </summary>
public class QrCodeRasterizer
{
    /// <summary>
    /// Genera el QR en píxeles. moduleSize = pixels per QR module
    /// (cuanto más grande, más nítido pero más grande la imagen).
    /// </summary>
    public SKBitmap Rasterize(string data, int moduleSize = 4, QRCodeGenerator.ECCLevel ecc = QRCodeGenerator.ECCLevel.M)
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentException("QR data cannot be empty.", nameof(data));

        using var generator = new QRCodeGenerator();
        var qrCodeData = generator.CreateQrCode(data, ecc);

        // PngByteQRCode + decode con SkiaSharp = 1 línea, sin platform deps
        var pngQr = new PngByteQRCode(qrCodeData);
        byte[] pngBytes = pngQr.GetGraphic(moduleSize);
        var bitmap = SKBitmap.Decode(pngBytes)
            ?? throw new InvalidOperationException("Failed to decode generated QR PNG.");
        return bitmap;
    }

    /// <summary>
    /// Calcula el tamaño total en píxeles para un QR dado (útil para layout).
    /// </summary>
    public SKSizeI Measure(string data, int moduleSize = 4, QRCodeGenerator.ECCLevel ecc = QRCodeGenerator.ECCLevel.M)
    {
        using var generator = new QRCodeGenerator();
        var qrCodeData = generator.CreateQrCode(data, ecc);
        // QRCoder no expone módulos directos; usar el bitmap es la forma robusta.
        var pngQr = new PngByteQRCode(qrCodeData);
        var bytes = pngQr.GetGraphic(moduleSize);
        using var bmp = SKBitmap.Decode(bytes);
        return new SKSizeI(bmp.Width, bmp.Height);
    }
}
