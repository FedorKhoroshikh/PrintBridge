using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CanonPrintBridge.Services;

/// <summary>
/// Process-wide diagnostic log. Writes timestamped lines to a per-day file under
/// %LocalAppData%\CanonPrintBridge\logs and, when the app was launched from a
/// terminal, to that console too. The UI log panel is fed separately by
/// <c>MainWindow.Log</c>, which also forwards here so the two stay in sync.
/// Every method swallows its own errors — logging must never crash the app.
/// </summary>
public static class Logger
{
    private static readonly object _gate = new();
    private static string _path = "";

    public static string FilePath => _path;

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);
    private const int ATTACH_PARENT_PROCESS = -1;

    /// <summary>Opens the log file for the day and attaches to the parent console (if any).</summary>
    public static void Init()
    {
        try
        {
            AttachConsole(ATTACH_PARENT_PROCESS); // no-op when started from Explorer
            Console.OutputEncoding = Encoding.UTF8; // keep Cyrillic readable in the terminal
        }
        catch { /* ignore */ }

        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CanonPrintBridge", "logs");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, $"bridge-{DateTime.Now:yyyyMMdd}.log");
            Write($"=== session start · v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version} ===");
        }
        catch { /* logging disabled if the file can't be opened */ }
    }

    /// <summary>Appends one timestamped line to console + file. Safe to call before Init.</summary>
    public static void Write(string msg)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {msg}";
        try { Console.WriteLine(line); } catch { }
        try { System.Diagnostics.Debug.WriteLine(line); } catch { }

        if (string.IsNullOrEmpty(_path)) return;
        try
        {
            lock (_gate)
                File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
        }
        catch { /* ignore */ }
    }
}
