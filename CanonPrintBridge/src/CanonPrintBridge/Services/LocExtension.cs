using System.Windows.Data;
using System.Windows.Markup;

namespace CanonPrintBridge.Services;

/// <summary>
/// XAML markup extension: <c>{loc:Loc key}</c> binds to the live localized string,
/// so text updates automatically when the language changes.
/// </summary>
public sealed class LocExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public LocExtension() { }
    public LocExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay,
        };
        return binding.ProvideValue(serviceProvider);
    }
}
