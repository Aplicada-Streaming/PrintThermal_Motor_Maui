using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MotorDsl.Core.Contracts;
using MotorDsl.Core.Models;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;

namespace MotorDsl.Integrated.MultaApp.Renderers
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

    public class PdfRenderer : IRenderer
    {
        public string Target => "pdf";

        public RenderResult Render(LayoutedDocument document, DeviceProfile profile)
        {
            var result = new RenderResult("pdf");
            try
            {
                GlobalFontSettings.FontResolver = MotorDslFontResolver.Instance;

                using var doc = new PdfDocument();
                var page = doc.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                using var gfx = XGraphics.FromPdfPage(page);
                var font = new XFont("DroidSans", 12);

                var layoutInfos = document.NodeLayoutInfo.Values.OrderBy(n => n.LineNumber);
                double y = 40;
                foreach (var node in layoutInfos)
                {
                    // Render bitmap image from base64
                    if (node.DeviceMetadata.TryGetValue("is_bitmap", out var bmpFlag) && bmpFlag is true
                        && node.DeviceMetadata.TryGetValue("bitmap_source", out var bmpSrc)
                        && bmpSrc is string bmpStr && !string.IsNullOrEmpty(bmpStr))
                    {
                        try
                        {
                            // Remover prefijo data URI y limpiar caracteres inválidos
                            var base64 = bmpStr.Contains(',') ? bmpStr[(bmpStr.IndexOf(',') + 1)..] : bmpStr;
                            base64 = base64.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim();
                            var imageBytes = Convert.FromBase64String(base64);
                            var xImage = XImage.FromStream(() => new MemoryStream(imageBytes));
                            double imgWidth = Math.Min(node.DeviceMetadata.TryGetValue("bitmap_width", out var bw)
                                ? Convert.ToDouble(bw) : 150, 400);
                            double imgHeight = imgWidth * xImage.PixelHeight / Math.Max(xImage.PixelWidth, 1);
                            double x = node.Alignment?.ToLower() == "center" ? (page.Width.Point - imgWidth) / 2 : 40;
                            gfx.DrawImage(xImage, x, y, imgWidth, imgHeight);
                            y += imgHeight + 4;
                        }
                        catch
                        {
                            gfx.DrawString("[Imagen]", font, XBrushes.Black, new XPoint(40, y));
                            y += font.Height + 4;
                        }
                        continue;
                    }

                    if (!string.IsNullOrEmpty(node.WrappedText))
                    {
                        gfx.DrawString(node.WrappedText, font, XBrushes.Black, new XPoint(40, y));
                        y += font.Height + 4;
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
    }
}

