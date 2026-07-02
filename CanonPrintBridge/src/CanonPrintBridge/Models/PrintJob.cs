using System.Text.Json.Serialization;

namespace CanonPrintBridge.Models;

/// <summary>
/// The job manifest written as &lt;id&gt;.job.json into the queue folder.
/// This is the cross-boundary "API" contract consumed by the XP watcher.
/// LBP-1120 is monochrome-only, so there is no colour option.
/// </summary>
public sealed class PrintJob
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>File name (not full path) of the PDF inside the queue folder.</summary>
    [JsonPropertyName("file")]
    public string File { get; set; } = "";

    [JsonPropertyName("copies")]
    public int Copies { get; set; } = 1;

    /// <summary>A4 | A5 | B5 — maps to the XP printer queue "Canon LBP-1120 &lt;paper&gt;".</summary>
    [JsonPropertyName("paper")]
    public string Paper { get; set; } = "A4";

    /// <summary>fit | noscale | shrink — passed straight to SumatraPDF -print-settings.</summary>
    [JsonPropertyName("scale")]
    public string Scale { get; set; } = "fit";

    /// <summary>"" = all pages, otherwise a SumatraPDF range like "1-4,7".</summary>
    [JsonPropertyName("pages")]
    public string Pages { get; set; } = "";

    /// <summary>none | manual (LBP-1120 has no auto-duplex unit).</summary>
    [JsonPropertyName("duplex")]
    public string Duplex { get; set; } = "none";

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = "";
}
