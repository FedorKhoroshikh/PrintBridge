using System.IO;
using System.Text.Json;
using CanonPrintBridge.Models;

namespace CanonPrintBridge.Services;

/// <summary>
/// Writes jobs into the shared queue folder and reads status back.
/// All writes are atomic (write *.tmp then rename) so the XP watcher never
/// observes a half-written file.
/// </summary>
public sealed class QueueService
{
    public string QueueRoot { get; }
    public string StatusDir { get; }
    public string PrintedDir { get; }

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public QueueService(string queueRoot)
    {
        QueueRoot = queueRoot;
        StatusDir = Path.Combine(queueRoot, "status");
        PrintedDir = Path.Combine(queueRoot, "printed");
        Directory.CreateDirectory(QueueRoot);
        Directory.CreateDirectory(StatusDir);
        Directory.CreateDirectory(PrintedDir);
    }

    public static string NewId(DateTime now) =>
        now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..4];

    /// <summary>
    /// Copies the PDF into the queue as &lt;id&gt;.pdf, then writes the job manifest.
    /// PDF is written first so the watcher always finds the file once the .job.json appears.
    /// </summary>
    public async Task SubmitAsync(string sourcePdf, PrintJob job)
    {
        var destPdf = Path.Combine(QueueRoot, job.Id + ".pdf");
        await using (var src = File.OpenRead(sourcePdf))
        await using (var dst = File.Create(destPdf))
        {
            await src.CopyToAsync(dst);
        }

        job.File = job.Id + ".pdf";

        var jobPath = Path.Combine(QueueRoot, job.Id + ".job.json");
        var tmp = jobPath + ".tmp";
        await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(job, JsonOpts));
        if (File.Exists(jobPath)) File.Delete(jobPath);
        File.Move(tmp, jobPath);
    }

    /// <summary>Reads the latest status for a job, or null if none/partial.</summary>
    public JobStatus? ReadStatus(string id)
    {
        var path = Path.Combine(StatusDir, id + ".status.json");
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<JobStatus>(File.ReadAllText(path));
        }
        catch
        {
            // file mid-write on the guest side — caller retries on next poll
            return null;
        }
    }

    /// <summary>Drops the &lt;id&gt;.continue flag that releases a manual-duplex job's 2nd pass.</summary>
    public void SignalContinue(string id) =>
        File.WriteAllText(Path.Combine(QueueRoot, id + ".continue"), "");
}
