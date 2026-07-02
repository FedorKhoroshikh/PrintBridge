using System.IO;
using System.Text.Json;
using System.ComponentModel;

namespace CanonPrintBridge.Services;

/// <summary>
/// Runtime string localization. Holds the active language's key-&gt;text map (loaded
/// from <c>Resources/Strings.&lt;lang&gt;.json</c> next to the exe) and exposes it via an
/// indexer, so XAML <c>{loc:Loc key}</c> bindings and code refresh live on language change.
/// </summary>
public sealed class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new();

    /// <summary>Supported language codes, in display order.</summary>
    public static readonly string[] Languages = { "ru", "en" };

    private Dictionary<string, string> _map = new();
    private string _language = "ru";

    private LocalizationManager() => Load("ru");

    public string Language => _language;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised after the active language changes (for imperatively-rendered UI).</summary>
    public event Action? LanguageChanged;

    /// <summary>Localized text for <paramref name="key"/>, or the key itself if missing.</summary>
    public string this[string key] => _map.TryGetValue(key, out var v) ? v : key;

    /// <summary>Code-behind convenience: same as the indexer, with optional formatting.</summary>
    public static string T(string key) => Instance[key];
    public static string T(string key, params object[] args) => string.Format(Instance[key], args);

    public void SetLanguage(string? lang)
    {
        lang = string.IsNullOrWhiteSpace(lang) ? "ru" : lang.ToLowerInvariant();
        if (lang == _language && _map.Count > 0) return;
        Load(lang);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]")); // refresh all indexer bindings
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        LanguageChanged?.Invoke();
    }

    private void Load(string lang)
    {
        _map = LoadFromDisk(lang) ?? LoadFromDisk("ru") ?? new Dictionary<string, string>();
        _language = lang;
    }

    private static Dictionary<string, string>? LoadFromDisk(string lang)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Resources", $"Strings.{lang}.json");
            if (!File.Exists(path)) return null;
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }
}
