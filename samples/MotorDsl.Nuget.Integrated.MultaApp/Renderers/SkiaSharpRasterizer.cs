using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;
using SkiaSharp;

namespace MotorDsl.Nuget.Integrated.MultaApp.Renderers;

/// <summary>
/// Implementa IBitmapRasterizer usando SkiaSharp.
/// Decodifica imágenes data:image/png;base64,... a formato raster 1-bit
/// apto para ESC/POS GS v 0.
/// Sprint 08 | TK-64
/// </summary>
public class SkiaSharpRasterizer : IBitmapRasterizer
{
    public RasterizedImage Rasterize(string source, int widthPixels)
    {
        System.Console.WriteLine($"[SKIA] rasterize source_len={source?.Length ?? 0}");
        System.Console.WriteLine($"[SKIA] rasterize source={source?.Substring(0, Math.Min(100, source?.Length ?? 0))}");

        var bitmap = DecodeSource(source);

        // Scale to target width maintaining aspect ratio
        if (bitmap.Width != widthPixels)
        {
            int targetHeight = (int)((float)bitmap.Height / bitmap.Width * widthPixels);
            var scaled = bitmap.Resize(new SKImageInfo(widthPixels, targetHeight), SKSamplingOptions.Default);
            bitmap.Dispose();
            bitmap = scaled;
        }

        // WidthBytes must be a multiple of 8 pixels → ceil(widthPixels/8)
        int widthBytes = (bitmap.Width + 7) / 8;
        int heightDots = bitmap.Height;
        var bits = new byte[widthBytes * heightDots];

        for (int y = 0; y < heightDots; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                // Convert to grayscale, threshold at 128 → black = bit on
                int gray = (pixel.Red * 299 + pixel.Green * 587 + pixel.Blue * 114) / 1000;
                bool isBlack = gray < 128 && pixel.Alpha > 128;

                if (isBlack)
                {
                    int byteIndex = y * widthBytes + (x / 8);
                    int bitIndex = 7 - (x % 8);
                    bits[byteIndex] |= (byte)(1 << bitIndex);
                }
            }
        }

        bitmap.Dispose();
        return new RasterizedImage(bits, widthBytes, heightDots);
    }

    private static SKBitmap DecodeSource(string source)
    {
        // Remover prefijo data:image/...;base64, si existe
        var base64Clean = source.Contains(",")
            ? source[(source.IndexOf(',') + 1)..]
            : source;

        // Remover saltos de línea, espacios y caracteres extra que invalidan base64
        base64Clean = base64Clean
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace(" ", "")
            .Trim();

        if (string.IsNullOrEmpty(base64Clean))
            throw new ArgumentException("Fuente de imagen vacía.", nameof(source));

        var bytes = Convert.FromBase64String(base64Clean);
        return SKBitmap.Decode(bytes)
            ?? throw new InvalidOperationException("SkiaSharp no pudo decodificar la imagen desde base64.");
    }
}
