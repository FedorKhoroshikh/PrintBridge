using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using CanonPrintBridge.Services;
using static CanonPrintBridge.Services.LocalizationManager;

namespace CanonPrintBridge;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _cfg;
    private readonly string _originalLanguage;
    private bool _ready;

    public SettingsWindow(AppConfig cfg)
    {
        InitializeComponent();
        _cfg = cfg;

        QueueBox.Text = _cfg.QueueRoot;
        SharedBox.Text = Path.GetDirectoryName(_cfg.QueueRoot.TrimEnd('\\')) ?? _cfg.QueueRoot;
        VmNameBox.Text = _cfg.VmName;
        VmNameBox.IsReadOnly = false;
        VBoxBox.Text = _cfg.VBoxManagePath;
        LauncherBox.Text = _cfg.LauncherPath;
        OfficeToPdfBox.Text = _cfg.OfficeToPdfPath;

        _originalLanguage = Instance.Language;
        foreach (ComboBoxItem it in LanguageBox.Items)
        {
            if (it.Tag as string == _originalLanguage) { LanguageBox.SelectedItem = it; break; }
        }
        _ready = true;
    }

    // Live-switch the UI language as the user picks; persisted on Save, reverted on Cancel.
    private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        if (LanguageBox.SelectedItem is ComboBoxItem { Tag: string code })
            Instance.SetLanguage(code);
    }

    private void BrowseShared_Click(object sender, RoutedEventArgs e) => BrowseFolder(SharedBox);
    private void BrowseQueue_Click(object sender, RoutedEventArgs e) => BrowseFolder(QueueBox);

    private void BrowseFolder(TextBox target)
    {
        var dlg = new OpenFolderDialog { Title = T("settings_choose_folder") };
        if (Directory.Exists(target.Text)) dlg.InitialDirectory = target.Text;
        if (dlg.ShowDialog() == true) target.Text = dlg.FolderName;
    }

    private void BrowseVBox_Click(object sender, RoutedEventArgs e) =>
        BrowseFile(VBoxBox, $"VBoxManage.exe|VBoxManage.exe|{T("filter_programs")}");

    private void BrowseLauncher_Click(object sender, RoutedEventArgs e) =>
        BrowseFile(LauncherBox, T("filter_scripts"));

    private void BrowseOfficeToPdf_Click(object sender, RoutedEventArgs e) =>
        BrowseFile(OfficeToPdfBox, $"OfficeToPDF.exe|OfficeToPDF.exe|{T("filter_programs")}");

    private void BrowseFile(TextBox target, string filter)
    {
        var dlg = new OpenFileDialog { Filter = filter, Title = T("settings_choose_file") };
        try
        {
            var dir = Path.GetDirectoryName(target.Text);
            if (dir is not null && Directory.Exists(dir)) dlg.InitialDirectory = dir;
        }
        catch { /* ignore bad path */ }
        if (dlg.ShowDialog() == true) target.Text = dlg.FileName;
    }

    private void Verify_Click(object sender, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{T("verify_shared"),-18}{Check(Directory.Exists(SharedBox.Text))}  {SharedBox.Text}");
        sb.AppendLine($"{T("verify_queue"),-18}{Check(Directory.Exists(QueueBox.Text))}  {QueueBox.Text}");
        sb.AppendLine($"{T("verify_vbox"),-18}{Check(File.Exists(VBoxBox.Text))}  {VBoxBox.Text}");
        sb.AppendLine($"{T("verify_launcher"),-18}{Check(File.Exists(LauncherBox.Text))}  {LauncherBox.Text}");
        MessageBox.Show(sb.ToString(), T("verify_title"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string Check(bool ok) => ok ? "✓" : "✗";

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _cfg.QueueRoot = QueueBox.Text.Trim();
        _cfg.VmName = VmNameBox.Text.Trim();
        _cfg.VBoxManagePath = VBoxBox.Text.Trim();
        _cfg.LauncherPath = LauncherBox.Text.Trim();
        _cfg.OfficeToPdfPath = OfficeToPdfBox.Text.Trim();
        _cfg.Language = Instance.Language;
        try
        {
            _cfg.Save();
        }
        catch (Exception ex)
        {
            MessageBox.Show(T("settings_save_failed", ex.Message), T("settings_title"),
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Instance.SetLanguage(_originalLanguage); // revert the live preview
        DialogResult = false;
    }
}
