using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace CanonPrintBridge.Services;

/// <summary>Reads page counts and writes temp PDFs containing a chosen subset of pages.</summary>
public static class PdfPageExtractor
{
    /// <summary>Number of pages in the PDF at <paramref name="path"/>.</summary>
    public static int PageCount(string path)
    {
        using var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        return doc.PageCount;
    }

    /// <summary>
    /// Writes a new PDF containing exactly <paramref name="pages"/> (1-based, in the
    /// given order) to <paramref name="destPath"/>. Returns false if none were valid.
    /// </summary>
    public static bool ExtractTo(string srcPath, IReadOnlyList<int> pages, string destPath)
    {
        using var src = PdfReader.Open(srcPath, PdfDocumentOpenMode.Import);
        using var dst = new PdfDocument();
        var added = 0;
        foreach (var p in pages)
        {
            if (p < 1 || p > src.PageCount) continue;
            dst.AddPage(src.Pages[p - 1]);
            added++;
        }
        if (added == 0) return false;
        dst.Save(destPath);
        return true;
    }
}
