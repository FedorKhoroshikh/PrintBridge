using System.IO;
using System.Text.Json;

namespace CanonPrintBridge;

/// <summary>
/// Settings loaded from appsettings.json next to the exe (falls back to defaults).
/// QueueRoot is the HOST-side path of the VirtualBox shared folder; the XP guest
/// sees the same folder as \\vboxsvr\Shared\Queue.
/// </summary>
public sealed class AppConfig
{
    public string QueueRoot { get; set; } = @"C:\Virtualization\Shared\Queue";

    /// <summary>
    /// Path to Print-Canon.ps1. Relative values (the default) resolve against the exe
    /// directory, where the build copies the launcher — so the app stays portable.
    /// </summary>
    public string LauncherPath { get; set; } = "Print-Canon.ps1";

    public static AppConfig Load()
    {
        var cfg = LoadRaw();
        if (!Path.IsPathRooted(cfg.LauncherPath))
            cfg.LauncherPath = Path.Combine(AppContext.BaseDirectory, cfg.LauncherPath);
        return cfg;
    }

    private static AppConfig LoadRaw()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(path))
            {
                var cfg = JsonSerializer.Deserialize<AppConfig>(
                    File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (cfg is not null) return cfg;
            }
        }
        catch
        {
            // fall through to defaults
        }
        return new AppConfig();
    }
}
