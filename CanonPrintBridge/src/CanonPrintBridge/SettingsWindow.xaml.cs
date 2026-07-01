using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace CanonPrintBridge;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _cfg;

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
    }

    private void BrowseShared_Click(object sender, RoutedEventArgs e) => BrowseFolder(SharedBox);
    private void BrowseQueue_Click(object sender, RoutedEventArgs e) => BrowseFolder(QueueBox);

    private void BrowseFolder(System.Windows.Controls.TextBox target)
    {
        var dlg = new OpenFolderDialog { Title = "Выберите папку" };
        if (Directory.Exists(target.Text)) dlg.InitialDirectory = target.Text;
        if (dlg.ShowDialog() == true) target.Text = dlg.FolderName;
    }

    private void BrowseVBox_Click(object sender, RoutedEventArgs e) =>
        BrowseFile(VBoxBox, "VBoxManage.exe|VBoxManage.exe|Программы (*.exe)|*.exe");

    private void BrowseLauncher_Click(object sender, RoutedEventArgs e) =>
        BrowseFile(LauncherBox, "Скрипты (*.ps1;*.cmd;*.bat)|*.ps1;*.cmd;*.bat|Все файлы (*.*)|*.*");

    private void BrowseFile(System.Windows.Controls.TextBox target, string filter)
    {
        var dlg = new OpenFileDialog { Filter = filter, Title = "Выберите файл" };
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
        sb.AppendLine($"Общая папка:      {Check(Directory.Exists(SharedBox.Text))}  {SharedBox.Text}");
        sb.AppendLine($"Папка очереди:    {Check(Directory.Exists(QueueBox.Text))}  {QueueBox.Text}");
        sb.AppendLine($"VBoxManage:       {Check(File.Exists(VBoxBox.Text))}  {VBoxBox.Text}");
        sb.AppendLine($"Скрипт запуска:   {Check(File.Exists(LauncherBox.Text))}  {LauncherBox.Text}");
        MessageBox.Show(sb.ToString(), "Проверка путей", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string Check(bool ok) => ok ? "✓" : "✗";

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _cfg.QueueRoot = QueueBox.Text.Trim();
        _cfg.VmName = VmNameBox.Text.Trim();
        _cfg.VBoxManagePath = VBoxBox.Text.Trim();
        _cfg.LauncherPath = LauncherBox.Text.Trim();
        try
        {
            _cfg.Save();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось сохранить настройки:\n{ex.Message}", "Настройки",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
