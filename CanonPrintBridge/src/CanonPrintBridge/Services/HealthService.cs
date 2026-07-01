using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CanonPrintBridge.Services;

/// <summary>Per-indicator readiness state used to drive the status dots and gating.</summary>
public enum IndicatorState { Ok, Booting, Off, Lost }

/// <summary>A snapshot of the three readiness indicators plus derived labels.</summary>
public sealed class HealthSnapshot
{
    public IndicatorState Vm { get; init; }
    public IndicatorState Os { get; init; }
    public IndicatorState Printer { get; init; }

    public string VmLabel { get; init; } = "";
    public string OsLabel { get; init; } = "";
    public string PrinterLabel { get; init; } = "";

    public string? PrinterName { get; init; }

    public bool AllOk => Vm == IndicatorState.Ok && Os == IndicatorState.Ok && Printer == IndicatorState.Ok;
}

/// <summary>
/// Probes VM + guest + printer readiness. VM state comes from
/// <c>VBoxManage list runningvms</c>; the guest heartbeat / printer come from
/// <c>&lt;QueueRoot&gt;\status\bridge.health.json</c> written by the XP watcher.
/// </summary>
public sealed class HealthService
{
    private readonly AppConfig _cfg;

    // A heartbeat file older than this (by host mtime) is treated as stale.
    private static readonly TimeSpan FreshWindow = TimeSpan.FromSeconds(20);

    public HealthService(AppConfig cfg) => _cfg = cfg;

    public bool IsVmRunning()
    {
        try
        {
            if (!File.Exists(_cfg.VBoxManagePath)) return false;
            var psi = new ProcessStartInfo
            {
                FileName = _cfg.VBoxManagePath,
                Arguments = "list runningvms",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(4000);
            return output.Contains('"' + _cfg.VmName + '"', StringComparison.OrdinalIgnoreCase)
                || output.Contains(_cfg.VmName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private sealed class HealthFile
    {
        [JsonPropertyName("printerPresent")] public bool PrinterPresent { get; set; }
        [JsonPropertyName("printerName")] public string PrinterName { get; set; } = "";
    }

    /// <summary>Reads bridge.health.json + its freshness, or (null, false) if absent/unreadable.</summary>
    private (HealthFile? health, bool fresh) ReadHealth()
    {
        try
        {
            var path = Path.Combine(_cfg.QueueRoot, "status", "bridge.health.json");
            if (!File.Exists(path)) return (null, false);
            var fresh = DateTime.Now - File.GetLastWriteTime(path) <= FreshWindow;
            var health = JsonSerializer.Deserialize<HealthFile>(File.ReadAllText(path));
            return (health, fresh);
        }
        catch
        {
            return (null, false);
        }
    }

    /// <summary>
    /// Builds the current snapshot. <paramref name="wasVmRunning"/> lets us tell
    /// "never started" (Off) apart from "was running, now gone" (Lost).
    /// </summary>
    public HealthSnapshot Evaluate(bool wasVmRunning)
    {
        var vmRunning = IsVmRunning();
        var (health, fresh) = ReadHealth();

        // --- VM ---
        IndicatorState vm;
        string vmLabel;
        if (vmRunning) { vm = IndicatorState.Ok; vmLabel = "Запущена"; }
        else if (wasVmRunning) { vm = IndicatorState.Lost; vmLabel = "Потеряна"; }
        else { vm = IndicatorState.Off; vmLabel = "Остановлена"; }

        // --- OS (guest heartbeat) ---
        IndicatorState os;
        string osLabel;
        if (!vmRunning) { os = IndicatorState.Off; osLabel = "Не запущен"; }
        else if (fresh) { os = IndicatorState.Ok; osLabel = "Готов"; }
        else if (health is not null) { os = IndicatorState.Lost; osLabel = "Связь потеряна"; }
        else { os = IndicatorState.Booting; osLabel = "Загрузка гостя…"; }

        // --- Printer ---
        IndicatorState printer;
        string printerLabel;
        if (fresh && health is { PrinterPresent: true }) { printer = IndicatorState.Ok; printerLabel = "В сети"; }
        else if (fresh && health is not null) { printer = IndicatorState.Lost; printerLabel = "Не найден"; }
        else { printer = IndicatorState.Off; printerLabel = "Неизвестно"; }

        return new HealthSnapshot
        {
            Vm = vm, VmLabel = vmLabel,
            Os = os, OsLabel = osLabel,
            Printer = printer, PrinterLabel = printerLabel,
            PrinterName = health?.PrinterName,
        };
    }
}
