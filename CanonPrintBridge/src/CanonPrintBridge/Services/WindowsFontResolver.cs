using System.IO;
using PdfSharp.Fonts;

namespace CanonPrintBridge.Services;

/// <summary>
/// Minimal PdfSharp font resolver that serves a monospace face (Consolas, falling
/// back to Courier New) from the Windows Fonts folder. Enough for rendering plain
/// text to PDF; PdfSharp 6.1 has no built-in Windows font resolution.
/// </summary>
public sealed class WindowsFontResolver : IFontResolver
{
    public const string Family = "Consolas";

    private static readonly string FontsDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    // faceName -> candidate files (first that exists wins).
    private static readonly Dictionary<string, string[]> Faces = new()
    {
        ["Consolas"]      = new[] { "consola.ttf",  "cour.ttf" },
        ["Consolas#b"]    = new[] { "consolab.ttf", "courbd.ttf" },
        ["Consolas#i"]    = new[] { "consolai.ttf", "couri.ttf" },
        ["Consolas#bi"]   = new[] { "consolaz.ttf", "courbi.ttf" },
    };

    public byte[]? GetFont(string faceName)
    {
        var candidates = Faces.TryGetValue(faceName, out var f) ? f : Faces["Consolas"];
        foreach (var file in candidates)
        {
            var path = Path.Combine(FontsDir, file);
            if (File.Exists(path)) return File.ReadAllBytes(path);
        }
        return null;
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var face = "Consolas" + (isBold && isItalic ? "#bi" : isBold ? "#b" : isItalic ? "#i" : "");
        return new FontResolverInfo(face);
    }
}
