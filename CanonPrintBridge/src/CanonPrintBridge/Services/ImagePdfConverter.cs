using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace CanonPrintBridge.Services;

/// <summary>Wraps an image into a single-page A4 PDF (fit, centered) via PdfSharp.</summary>
public sealed class ImagePdfConverter : IPdfConverter
{
    public IReadOnlyCollection<string> Extensions { get; } =
        new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff" };

    public Task<string> ConvertAsync(string sourcePath, string destPdfPath)
    {
        using var img = XImage.FromFile(sourcePath);
        using var doc = new PdfDocument();
        var page = doc.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        if (img.PixelWidth > img.PixelHeight)
            page.Orientation = PdfSharp.PageOrientation.Landscape;

        using (var gfx = XGraphics.FromPdfPage(page))
        {
            const double margin = 24;
            var availW = page.Width.Point - 2 * margin;
            var availH = page.Height.Point - 2 * margin;
            var scale = Math.Min(availW / img.PixelWidth, availH / img.PixelHeight);
            var w = img.PixelWidth * scale;
            var h = img.PixelHeight * scale;
            var x = (page.Width.Point - w) / 2;
            var y = (page.Height.Point - h) / 2;
            gfx.DrawImage(img, x, y, w, h);
        }

        doc.Save(destPdfPath);
        return Task.FromResult(destPdfPath);
    }
}
