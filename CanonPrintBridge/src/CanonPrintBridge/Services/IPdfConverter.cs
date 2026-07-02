namespace CanonPrintBridge.Services;

/// <summary>Converts a single non-PDF input file into a PDF on the Win11 host.</summary>
public interface IPdfConverter
{
    /// <summary>Lowercased extensions (with dot) this converter handles, e.g. ".docx".</summary>
    IReadOnlyCollection<string> Extensions { get; }

    /// <summary>Produces <paramref name="destPdfPath"/> from <paramref name="sourcePath"/>; returns it. Throws on failure.</summary>
    Task<string> ConvertAsync(string sourcePath, string destPdfPath);
}
