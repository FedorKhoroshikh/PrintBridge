using System.IO;

namespace CanonPrintBridge.Services;

/// <summary>
/// Routes an input file to a PDF: PDFs pass through untouched; other supported
/// formats are converted to a temp PDF by the first matching <see cref="IPdfConverter"/>.
/// The print queue stays PDF-only — conversion happens here, host-side, before submit.
/// </summary>
public sealed class PdfConversionService
{
    private readonly IReadOnlyList<IPdfConverter> _converters;

    public PdfConversionService(IReadOnlyList<IPdfConverter> converters) => _converters = converters;

    public static bool IsPdf(string path) =>
        Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    /// <summary>All accepted extensions (".pdf" plus every converter's), lowercased.</summary>
    public IReadOnlyList<string> SupportedExtensions =>
        _converters.SelectMany(c => c.Extensions).Prepend(".pdf").Distinct().ToList();

    public bool IsSupported(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".pdf" || _converters.Any(c => c.Extensions.Contains(ext));
    }

    /// <summary>
    /// Returns a PDF path for <paramref name="src"/>: the source itself when already a PDF,
    /// otherwise a freshly converted temp PDF. Throws <see cref="NotSupportedException"/>
    /// for unknown types, or the converter's exception on failure.
    /// </summary>
    public async Task<string> ToPdfAsync(string src)
    {
        if (IsPdf(src)) return src;

        var ext = Path.GetExtension(src).ToLowerInvariant();
        var conv = _converters.FirstOrDefault(c => c.Extensions.Contains(ext))
                   ?? throw new NotSupportedException(ext);

        var dir = Path.Combine(Path.GetTempPath(), "CanonPrintBridge");
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, $"conv-{Guid.NewGuid():N}.pdf");
        return await conv.ConvertAsync(src, dest);
    }
}
