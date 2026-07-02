using System.Diagnostics;
using System.IO;

namespace CanonPrintBridge.Services;

/// <summary>
/// Converts Office documents to PDF by shelling out to the bundled OfficeToPDF.exe,
/// which drives the installed Microsoft Office via COM. Office must be present;
/// otherwise the tool exits non-zero and this surfaces a clear error.
/// </summary>
public sealed class OfficeToPdfConverter : IPdfConverter
{
    private readonly AppConfig _cfg;

    public OfficeToPdfConverter(AppConfig cfg) => _cfg = cfg;

    public IReadOnlyCollection<string> Extensions { get; } = new[] { ".docx", ".doc", ".rtf" };

    public async Task<string> ConvertAsync(string sourcePath, string destPdfPath)
    {
        var tool = _cfg.OfficeToPdfPath;
        if (!File.Exists(tool))
            throw new FileNotFoundException($"OfficeToPDF not found: {tool}");

        var psi = new ProcessStartInfo
        {
            FileName = tool,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        psi.ArgumentList.Add(sourcePath);
        psi.ArgumentList.Add(destPdfPath);

        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException("Could not start OfficeToPDF.");
        var stdout = await p.StandardOutput.ReadToEndAsync();
        var stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();

        if (p.ExitCode != 0 || !File.Exists(destPdfPath))
        {
            var detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException($"OfficeToPDF rc={p.ExitCode}: {detail.Trim()}");
        }
        return destPdfPath;
    }
}
