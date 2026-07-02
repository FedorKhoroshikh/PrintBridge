using System.IO;
using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace CanonPrintBridge.Services;

/// <summary>
/// Renders a plain-text file into a paginated A4 PDF (monospace, wrapped) via PdfSharp.
/// Detects UTF-8/UTF-16 by BOM, then strict UTF-8, then falls back to Windows-1251.
/// </summary>
public sealed class TextPdfConverter : IPdfConverter
{
    public IReadOnlyCollection<string> Extensions { get; } = new[] { ".txt", ".log", ".csv", ".md" };

    public Task<string> ConvertAsync(string sourcePath, string destPdfPath)
    {
        var text = ReadTextDetectEncoding(sourcePath);

        const double fontSize = 10;
        const double margin = 40;
        var font = new XFont(WindowsFontResolver.Family, fontSize);
        var lineHeight = fontSize * 1.4;

        using var doc = new PdfDocument();
        XGraphics? gfx = null;
        PdfPage page = null!;
        double y = 0, usableWidth = 0, bottom = 0;

        void NewPage()
        {
            gfx?.Dispose();
            page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            usableWidth = page.Width.Point - 2 * margin;
            bottom = page.Height.Point - margin;
            y = margin;
        }
        NewPage();

        foreach (var raw in text.Replace("\t", "    ").Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            foreach (var chunk in Wrap(gfx!, line, font, usableWidth))
            {
                if (y + lineHeight > bottom) NewPage();
                gfx!.DrawString(chunk.Length == 0 ? " " : chunk, font, XBrushes.Black,
                    new XRect(margin, y, usableWidth, lineHeight), XStringFormats.TopLeft);
                y += lineHeight;
            }
        }

        gfx?.Dispose();
        doc.Save(destPdfPath);
        return Task.FromResult(destPdfPath);
    }

    // Word-aware wrap; a single word wider than the line is hard-split by character.
    private static IEnumerable<string> Wrap(XGraphics gfx, string line, XFont font, double maxWidth)
    {
        if (line.Length == 0) { yield return ""; yield break; }

        var words = line.Split(' ');
        var current = new StringBuilder();
        foreach (var word in words)
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (gfx.MeasureString(candidate, font).Width <= maxWidth)
            {
                current.Clear().Append(candidate);
                continue;
            }
            if (current.Length > 0) { yield return current.ToString(); current.Clear(); }

            // word itself may still overflow -> hard-split by character
            var piece = new StringBuilder();
            foreach (var ch in word)
            {
                if (gfx.MeasureString(piece.ToString() + ch, font).Width > maxWidth && piece.Length > 0)
                {
                    yield return piece.ToString();
                    piece.Clear();
                }
                piece.Append(ch);
            }
            if (piece.Length > 0) current.Append(piece);
        }
        if (current.Length > 0) yield return current.ToString();
    }

    private static string ReadTextDetectEncoding(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        try
        {
            return new UTF8Encoding(false, true).GetString(bytes); // strict: throws on invalid
        }
        catch (DecoderFallbackException)
        {
            try { return Encoding.GetEncoding(1251).GetString(bytes); } // Cyrillic ANSI
            catch { return Encoding.Latin1.GetString(bytes); }
        }
    }
}
