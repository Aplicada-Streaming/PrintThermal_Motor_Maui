using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using SkiaSharp;

namespace MotorDsl.Maui.Renderers
{
    public class MotorDslFontResolver : IFontResolver
    {
        public static readonly MotorDslFontResolver Instance = new();

        public string DefaultFontName => "DroidSans";

        public byte[] GetFont(string faceName)
        {
            var assembly = typeof(MotorDslFontResolver).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("DroidSans-Regular.ttf"));

            if (resourceName == null)
                throw new InvalidOperationException(
                    "No se encontró DroidSans-Regular.ttf como EmbeddedResource. " +
                    $"Recursos disponibles: {string.Join(", ", assembly.GetManifestResourceNames())}");

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return new FontResolverInfo("DroidSans");
        }
    }

    /// <summary>
    /// Renderer PDF que soporta texto (con word-wrap por medición real y
    /// page-break automático), QR (vía QrCodeRasterizer) y barcode (texto fallback).
    /// </summary>
    public class PdfRenderer : IRenderer
    {
        private readonly QrCodeRasterizer _qrRasterizer;

        public string Target => "pdf";

        public PdfRenderer(QrCodeRasterizer qrRasterizer)
        {
            _qrRasterizer = qrRasterizer;
        }

        public RenderResult Render(LayoutedDocument document, DeviceProfile profile)
        {
            var result = new RenderResult("pdf");
            try
            {
                GlobalFontSettings.FontResolver = MotorDslFontResolver.Instance;

                using var doc = new PdfDocument();
                using var state = new PdfRenderState(doc, _qrRasterizer);

                var layoutInfos = document.NodeLayoutInfo.Values
                    .OrderBy(n => n.LineNumber)
                    .ThenBy(n => n.ColumnNumber);

                foreach (var node in layoutInfos)
                {
                    // QR
                    if (node.DeviceMetadata.TryGetValue("is_qr", out var qrFlag) && qrFlag is true
                        && node.DeviceMetadata.TryGetValue("qr_data", out var qrDataObj)
                        && qrDataObj is string qrData && !string.IsNullOrEmpty(qrData))
                    {
                        state.RenderQr(qrData, node.Alignment);
                        continue;
                    }

                    // Bitmap base64
                    if (node.DeviceMetadata.TryGetValue("is_bitmap", out var bmpFlag) && bmpFlag is true
                        && node.DeviceMetadata.TryGetValue("bitmap_source", out var bmpSrc)
                        && bmpSrc is string bmpStr && !string.IsNullOrEmpty(bmpStr))
                    {
                        double width = 150;
                        if (node.DeviceMetadata.TryGetValue("bitmap_width", out var bw))
                        {
                            try { width = Math.Min(Convert.ToDouble(bw), 400); } catch { }
                        }
                        state.RenderBitmap(bmpStr, node.Alignment, width);
                        continue;
                    }

                    // Barcode → texto fallback "[Código: data]"
                    if (node.DeviceMetadata.TryGetValue("is_barcode", out var bcFlag) && bcFlag is true
                        && node.DeviceMetadata.TryGetValue("barcode_data", out var bcDataObj))
                    {
                        var bcData = bcDataObj?.ToString() ?? "";
                        state.RenderBarcodeText(bcData, node.Alignment);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(node.WrappedText))
                    {
                        bool bold = node.DeviceMetadata.TryGetValue("bold", out var bv) && bv is true;
                        state.RenderText(node.WrappedText, node.Alignment, bold);
                    }
                }

                using var ms = new MemoryStream();
                doc.Save(ms, false);
                result.Output = ms.ToArray();
            }
            catch (Exception ex)
            {
                result.AddError($"PDF error: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Estado del render PDF: encapsula doc / página actual / gfx / cursor Y
        /// para permitir page-break y word-wrap sin pasar refs por todos lados.
        /// </summary>
        private sealed class PdfRenderState : IDisposable
        {
            private const double Margin = 40;
            private const double LineGap = 4;

            private readonly PdfDocument _doc;
            private readonly QrCodeRasterizer _qr;
            private readonly XFont _font;
            private readonly XFont _fontBold;

            private PdfPage _page;
            private XGraphics _gfx;
            private double _y;

            public PdfRenderState(PdfDocument doc, QrCodeRasterizer qr)
            {
                _doc = doc;
                _qr = qr;
                _page = doc.AddPage();
                _page.Size = PdfSharpCore.PageSize.A4;
                _gfx = XGraphics.FromPdfPage(_page);
                _font = new XFont("DroidSans", 12);
                _fontBold = new XFont("DroidSans", 12, XFontStyle.Bold);
                _y = Margin;
            }

            private double LineAdvance(XFont font) => font.Height + LineGap;

            /// <summary>
            /// Asegura espacio vertical. Si no alcanza, abre nueva página y resetea cursor.
            /// </summary>
            public void EnsureSpace(double needed)
            {
                if (_y + needed > _page.Height.Point - Margin)
                {
                    _gfx.Dispose();
                    _page = _doc.AddPage();
                    _page.Size = PdfSharpCore.PageSize.A4;
                    _gfx = XGraphics.FromPdfPage(_page);
                    _y = Margin;
                }
            }

            public void RenderText(string text, string? alignment, bool bold)
            {
                var font = bold ? _fontBold : _font;
                DrawWrapped(text, font, alignment);
            }

            public void RenderBarcodeText(string data, string? alignment)
            {
                var label = $"[Código: {data}]";
                DrawWrapped(label, _font, alignment);
            }

            public void RenderQr(string data, string? alignment)
            {
                const double imgWidth = 150;
                const double imgHeight = 150;
                EnsureSpace(imgHeight + LineGap);
                try
                {
                    using var qrBitmap = _qr.Rasterize(data, moduleSize: 6);
                    byte[] pngBytes;
                    using (var skImage = SKImage.FromBitmap(qrBitmap))
                    using (var pngData = skImage.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        pngBytes = pngData.ToArray();
                    }
                    var xImage = XImage.FromStream(() => new MemoryStream(pngBytes));
                    double x = alignment?.ToLower() == "center"
                        ? (_page.Width.Point - imgWidth) / 2
                        : alignment?.ToLower() == "right"
                            ? _page.Width.Point - Margin - imgWidth
                            : Margin;
                    _gfx.DrawImage(xImage, x, _y, imgWidth, imgHeight);
                    _y += imgHeight + LineGap;
                }
                catch (Exception ex)
                {
                    DrawWrapped($"[QR: {data}] (error: {ex.Message})", _font, alignment);
                }
            }

            public void RenderBitmap(string base64Source, string? alignment, double width)
            {
                try
                {
                    var base64 = base64Source.Contains(',') ? base64Source[(base64Source.IndexOf(',') + 1)..] : base64Source;
                    base64 = base64.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim();
                    var imageBytes = Convert.FromBase64String(base64);
                    var xImage = XImage.FromStream(() => new MemoryStream(imageBytes));
                    double imgWidth = Math.Min(width, _page.Width.Point - 2 * Margin);
                    double imgHeight = imgWidth * xImage.PixelHeight / Math.Max(xImage.PixelWidth, 1);

                    EnsureSpace(imgHeight + LineGap);
                    double x = alignment?.ToLower() == "center"
                        ? (_page.Width.Point - imgWidth) / 2
                        : alignment?.ToLower() == "right"
                            ? _page.Width.Point - Margin - imgWidth
                            : Margin;
                    _gfx.DrawImage(xImage, x, _y, imgWidth, imgHeight);
                    _y += imgHeight + LineGap;
                }
                catch
                {
                    DrawWrapped("[Imagen]", _font, alignment);
                }
            }

            /// <summary>
            /// Word-wrap por medición real (XGraphics.MeasureString). Maneja page-break
            /// automáticamente si el texto se extiende a varias líneas.
            /// </summary>
            private void DrawWrapped(string text, XFont font, string? alignment)
            {
                double maxWidth = _page.Width.Point - 2 * Margin;
                var paragraphs = text.Split('\n');
                foreach (var paragraph in paragraphs)
                {
                    var words = paragraph.Split(' ');
                    var line = new StringBuilder();
                    foreach (var word in words)
                    {
                        var trial = line.Length == 0 ? word : line + " " + word;
                        var size = _gfx.MeasureString(trial, font);
                        if (size.Width > maxWidth && line.Length > 0)
                        {
                            FlushLine(line.ToString(), font, alignment, maxWidth);
                            line.Clear();
                            line.Append(word);
                        }
                        else
                        {
                            line.Length = 0;
                            line.Append(trial);
                        }
                    }
                    if (line.Length > 0)
                        FlushLine(line.ToString(), font, alignment, maxWidth);
                    else if (words.Length == 1 && words[0].Length == 0)
                    {
                        // línea vacía → avanzar Y un line-height
                        EnsureSpace(LineAdvance(font));
                        _y += LineAdvance(font);
                    }
                }
            }

            private void FlushLine(string line, XFont font, string? alignment, double maxWidth)
            {
                EnsureSpace(LineAdvance(font));
                double x = Margin;
                if (alignment?.ToLower() == "center")
                {
                    var size = _gfx.MeasureString(line, font);
                    x = (_page.Width.Point - size.Width) / 2;
                }
                else if (alignment?.ToLower() == "right")
                {
                    var size = _gfx.MeasureString(line, font);
                    x = _page.Width.Point - Margin - size.Width;
                }
                // PdfSharp DrawString usa el baseline del texto desde el punto Y; sumamos font.Height
                // para que no se corte arriba: posicionamos baseline en _y + font.Height.
                _gfx.DrawString(line, font, XBrushes.Black, new XPoint(x, _y + font.Height * 0.8));
                _y += LineAdvance(font);
            }

            public void Dispose()
            {
                _gfx?.Dispose();
            }
        }
    }
}
