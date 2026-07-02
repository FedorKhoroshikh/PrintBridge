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

    /// <summary>Name of the VirtualBox VM that hosts the Windows XP print guest.</summary>
    public string VmName { get; set; } = "Microelectronics";

    /// <summary>Full path to VBoxManage.exe (used for status polling and shutdown).</summary>
    public string VBoxManagePath { get; set; } = @"C:\Program Files\Oracle\VirtualBox\VBoxManage.exe";

    /// <summary>UI language code: "ru" or "en".</summary>
    public string Language { get; set; } = "ru";

    /// <summary>
    /// Path to OfficeToPDF.exe (converts Word/Office docs to PDF via installed Office).
    /// Relative values resolve against the exe directory, where the build copies it.
    /// </summary>
    public string OfficeToPdfPath { get; set; } = "OfficeToPDF.exe";

    public static AppConfig Load()
    {
        var cfg = LoadRaw();
        if (!Path.IsPathRooted(cfg.LauncherPath))
            cfg.LauncherPath = Path.Combine(AppContext.BaseDirectory, cfg.LauncherPath);
        if (!Path.IsPathRooted(cfg.OfficeToPdfPath))
            cfg.OfficeToPdfPath = Path.Combine(AppContext.BaseDirectory, cfg.OfficeToPdfPath);
        return cfg;
    }

    private static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    private static AppConfig LoadRaw()
    {
        try
        {
            var path = ConfigPath;
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

    /// <summary>Writes the current settings to appsettings.json next to the exe.</summary>
    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
