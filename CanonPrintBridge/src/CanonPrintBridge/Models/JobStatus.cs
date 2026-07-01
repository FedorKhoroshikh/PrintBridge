using System.Text.Json.Serialization;

namespace CanonPrintBridge.Models;

/// <summary>
/// Status written back by the XP watcher as status/&lt;id&gt;.status.json.
/// State machine: queued -> printing -> [awaiting-flip -> printing] -> done | error
/// </summary>
public sealed class JobStatus
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = "";
}
