using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Ellipse = System.Windows.Shapes.Ellipse;
using CanonPrintBridge.Models;
using CanonPrintBridge.Services;
using Microsoft.Win32;

namespace CanonPrintBridge;

public partial class MainWindow : Window
{
    private readonly AppConfig _cfg;
    private readonly QueueService _queue;
    private readonly HealthService _health;
    private string? _pdfPath;

    private readonly DispatcherTimer _tick;      // print operation elapsed timer
    private readonly DispatcherTimer _healthTimer; // readiness poller
    private DateTime _opStart;
    private string _opLabel = "";

    private bool _wasVmRunning;   // VM has been seen running (=> disappearance is "Lost")
    private bool _printing;       // a print job is in flight (keeps the gate honest)
    private int _logCount;

    private double _normalWidth;
    private bool _previewOpen;

    public MainWindow()
    {
        InitializeComponent();
        _cfg = AppConfig.Load();
        _queue = new QueueService(_cfg.QueueRoot);
        _health = new HealthService(_cfg);

        FooterText.Text = $"Готово · очередь: {_cfg.QueueRoot}";
        var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        if (ver is not null) VersionText.Text = $"v{ver.Major}.{ver.Minor}";
        Log($"Очередь: {_cfg.QueueRoot}");

        _tick = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tick.Tick += (_, _) => StatusText.Text = $"{_opLabel}… {Elapsed():m\\:ss}";

        _healthTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2500) };
        _healthTimer.Tick += (_, _) => RefreshHealth();
        Loaded += (_, _) => { RefreshHealth(); _healthTimer.Start(); };
    }

    private TimeSpan Elapsed() => DateTime.Now - _opStart;

    // ---- Busy indicator + ticking timer for the current operation ----
    private void StartBusy(string label)
    {
        _opLabel = label;
        _opStart = DateTime.Now;
        Busy.IsIndeterminate = true;
        StatusText.Text = $"{_opLabel}… 0:00";
        _tick.Start();
    }

    private void StopBusy(string finalText = "")
    {
        _tick.Stop();
        Busy.IsIndeterminate = false;
        StatusText.Text = finalText;
    }

    private void Log(string msg)
    {
        LogBox.AppendText($"{DateTime.Now:HH:mm:ss}  {msg}{Environment.NewLine}");
        LogBox.ScrollToEnd();
        _logCount++;
        LogCount.Text = $"{_logCount} " + (_logCount == 1 ? "запись" : _logCount is >= 2 and <= 4 ? "записи" : "записей");
    }

    // ================= File selection =================

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "PDF (*.pdf)|*.pdf", Title = "Выберите PDF для печати" };
        if (dlg.ShowDialog() == true) SetPdf(dlg.FileName);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = IsPdfDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (IsPdfDrop(e) && e.Data.GetData(DataFormats.FileDrop) is string[] files)
            SetPdf(files[0]);
    }

    private static bool IsPdfDrop(DragEventArgs e) =>
        e.Data.GetDataPresent(DataFormats.FileDrop)
        && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } f
        && f[0].EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    private void SetPdf(string path)
    {
        if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
        {
            MessageBox.Show("Нужен существующий PDF-файл.", "Canon Print Bridge",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _pdfPath = path;
        PdfPathBox.Text = path;

        FileName.Text = Path.GetFileName(path);
        FileMeta.Text = $"{FormatSize(new FileInfo(path).Length)} · {Path.GetDirectoryName(path)}";

        EmptyState.Visibility = Visibility.Collapsed;
        FileCard.Visibility = Visibility.Visible;

        Log($"Выбран файл: {Path.GetFileName(path)}");
        if (_previewOpen) LoadPreview();
        RefreshHealth();
    }

    private void ClearFile_Click(object sender, RoutedEventArgs e)
    {
        _pdfPath = null;
        PdfPathBox.Text = "";
        FileCard.Visibility = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Visible;
        if (_previewOpen) LoadPreview();
        RefreshHealth();
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} Б";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} КБ";
        return $"{bytes / (1024.0 * 1024):0.#} МБ";
    }

    private void Integer_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !int.TryParse(e.Text, out _);

    // ================= Launcher (start VM) =================

    private async void Launcher_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(_cfg.LauncherPath))
        {
            MessageBox.Show($"Лаунчер не найден:\n{_cfg.LauncherPath}", "Canon Print Bridge",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var btn = sender as Button;
        if (btn is not null) btn.IsEnabled = false;
        StartBusy("Запуск VM");
        try
        {
            Log("Запуск принтера (VM)…");
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{_cfg.LauncherPath}\"",
                UseShellExecute = true,
            });
            if (p is not null) await p.WaitForExitAsync();
            Log("Команда отправлена. XP грузится (~30–60 c); принтер прицепится сам.");
            Log("Можно сразу ставить «Печать» — задание подождёт в очереди, пока сторож не поднимется.");
            StopBusy("VM запускается");
        }
        catch (Exception ex)
        {
            Log($"Ошибка запуска лаунчера: {ex.Message}");
            StopBusy();
        }
        finally
        {
            if (btn is not null) btn.IsEnabled = true;
        }
    }

    // ================= Print =================

    private async void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_pdfPath is null || !File.Exists(_pdfPath))
        {
            MessageBox.Show("Сначала выберите PDF.", "Canon Print Bridge",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(CopiesBox.Text.Trim(), out var copies) || copies < 1)
            copies = 1;

        var job = new PrintJob
        {
            Id = QueueService.NewId(DateTime.Now),
            Copies = copies,
            Paper = (PaperBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "A4",
            Scale = (ScaleBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "fit",
            Pages = PagesBox.Text.Trim(),
            Duplex = "none",
            CreatedAt = DateTime.Now.ToString("s"),
        };

        _printing = true;
        PrintButton.IsEnabled = false;
        StartBusy("Печать");
        var result = "timeout";
        try
        {
            Log($"Задание {job.Id}: {job.Paper}, копий {job.Copies}, дуплекс нет");
            await _queue.SubmitAsync(_pdfPath, job);
            Log("Отправлено в очередь, жду печать…");
            result = await WaitForCompletionAsync(job.Id);
        }
        catch (Exception ex)
        {
            result = "error";
            Log($"Ошибка: {ex.Message}");
            MessageBox.Show(ex.Message, "Canon Print Bridge", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            var total = Elapsed();
            StopBusy(result switch
            {
                "done" => $"Готово за {total:m\\:ss}",
                "error" => "Ошибка",
                "timeout" => "Таймаут",
                _ => "",
            });
            _printing = false;
            RefreshHealth();
        }
    }

    // Returns final state: "done" / "error" / "timeout".
    private async Task<string> WaitForCompletionAsync(string id)
    {
        var deadline = DateTime.Now.AddMinutes(10);
        var lastState = "";

        while (DateTime.Now < deadline)
        {
            var st = _queue.ReadStatus(id);

            if (st is not null && st.State != lastState)
            {
                lastState = st.State;
                _opLabel = Translate(st.State);
                var suffix = string.IsNullOrEmpty(st.Message) ? "" : " — " + st.Message;
                Log($"Статус: {Translate(st.State)}{suffix}");
            }

            switch (st?.State)
            {
                case "done":
                    Log("Готово ✓");
                    return "done";
                case "error":
                    MessageBox.Show($"Печать не удалась: {st.Message}", "Canon Print Bridge",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return "error";
            }

            await Task.Delay(800);
        }

        Log("Таймаут ожидания статуса (10 мин). Проверь, запущена ли VM и сторож в XP.");
        return "timeout";
    }

    private static string Translate(string state) => state switch
    {
        "queued" => "в очереди",
        "printing" => "печать",
        "awaiting-flip" => "жду переворот стопки",
        "done" => "готово",
        "error" => "ошибка",
        _ => state,
    };

    // ================= Readiness / gating =================

    private void RefreshHealth()
    {
        var snap = _health.Evaluate(_wasVmRunning);
        if (snap.Vm == IndicatorState.Ok) _wasVmRunning = true;

        SetDot(VmDot, VmState, snap.Vm, snap.VmLabel);
        SetDot(OsDot, OsState, snap.Os, snap.OsLabel);
        SetDot(PrDot, PrState, snap.Printer, snap.PrinterLabel);

        ReadyBadge.Visibility = snap.AllOk ? Visibility.Visible : Visibility.Collapsed;
        LauncherPanel.Visibility = snap.Vm == IndicatorState.Ok ? Visibility.Collapsed : Visibility.Visible;

        FooterDot.Fill = (Brush)FindRes(snap.AllOk ? "Ok" : "Off");

        UpdatePrintGate(snap);
    }

    private void SetDot(Ellipse dot, TextBlock label, IndicatorState state, string text)
    {
        var (styleKey, brushKey) = state switch
        {
            IndicatorState.Ok => ("DotOk", "Ok"),
            IndicatorState.Booting => ("DotBooting", "Warn"),
            IndicatorState.Lost => ("DotLost", "Lost"),
            _ => ("DotOff", "Off"),
        };
        dot.Style = (Style)FindRes(styleKey);
        label.Text = text;
        label.Foreground = (Brush)FindRes(brushKey);
    }

    private void UpdatePrintGate(HealthSnapshot snap)
    {
        if (_printing)
        {
            PrintButton.IsEnabled = false;
            return;
        }

        var missing = new List<string>();
        if (_pdfPath is null) missing.Add("выберите PDF");
        if (snap.Vm != IndicatorState.Ok) missing.Add("запустите виртуальную машину");
        else if (snap.Os != IndicatorState.Ok) missing.Add("дождитесь готовности Windows XP");
        else if (snap.Printer != IndicatorState.Ok) missing.Add("принтер не найден");

        if (missing.Count == 0)
        {
            PrintButton.IsEnabled = true;
            PrintButton.ToolTip = "Отправить задание в очередь печати";
        }
        else
        {
            PrintButton.IsEnabled = false;
            PrintButton.ToolTip = "Недоступно: " + string.Join("; ", missing) + ".";
        }
    }

    private static object FindRes(string key) => Application.Current.FindResource(key);

    // ================= Preview split =================

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        if (_previewOpen) ClosePreview();
        else OpenPreview();
    }

    private void OpenPreview()
    {
        _previewOpen = true;
        _normalWidth = Width;
        PreviewColumn.Width = new GridLength(660);
        PreviewSplitter.Width = 6;
        PreviewPanel.Visibility = Visibility.Visible;
        MinWidth = 900;
        Width = Math.Min(_normalWidth + 700, SystemParameters.WorkArea.Width - 40);
        PreviewButton.ToolTip = "Скрыть предпросмотр";
        LoadPreview();
    }

    private void ClosePreview()
    {
        _previewOpen = false;
        PreviewColumn.Width = new GridLength(0);
        PreviewSplitter.Width = 0;
        PreviewPanel.Visibility = Visibility.Collapsed;
        MinWidth = 446;
        if (_normalWidth > 0) Width = _normalWidth;
        PreviewButton.ToolTip = "Показать предпросмотр";
    }

    private void LoadPreview()
    {
        if (_pdfPath is null)
        {
            PreviewTitle.Text = "Предпросмотр";
            PreviewFooter.Text = "";
            ShowPreviewPlaceholder("Файл не выбран");
            return;
        }

        PreviewTitle.Text = $"Предпросмотр — {Path.GetFileName(_pdfPath)}";
        PreviewFooter.Text = Path.GetFileName(_pdfPath);
        try
        {
            PreviewPlaceholder.Visibility = Visibility.Collapsed;
            PreviewWeb.Visibility = Visibility.Visible;
            PreviewWeb.CoreWebView2InitializationCompleted -= OnWebViewInit;
            PreviewWeb.CoreWebView2InitializationCompleted += OnWebViewInit;
            PreviewWeb.Source = new Uri(_pdfPath);
        }
        catch (Exception ex)
        {
            // TODO WebView2 — runtime unavailable; fall back to placeholder.
            ShowPreviewPlaceholder($"Предпросмотр недоступен: {ex.Message}");
        }
    }

    private void OnWebViewInit(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
            ShowPreviewPlaceholder("Предпросмотр недоступен (не установлен WebView2 Runtime).");
    }

    private void ShowPreviewPlaceholder(string text)
    {
        PreviewPlaceholder.Text = text;
        PreviewPlaceholder.Visibility = Visibility.Visible;
        PreviewWeb.Visibility = Visibility.Collapsed;
    }

    // ================= Settings =================

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsWindow(_cfg) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            FooterText.Text = $"Готово · очередь: {_cfg.QueueRoot}";
            Log("Настройки сохранены.");
            RefreshHealth();
        }
    }

    // ================= Shutdown VM =================

    private void Shutdown_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ConfirmShutdownWindow { Owner = this };
        if (dlg.ShowDialog() != true) return;

        try
        {
            Log("Завершение работы: выключаю ВМ…");
            if (File.Exists(_cfg.VBoxManagePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _cfg.VBoxManagePath,
                    Arguments = $"controlvm \"{_cfg.VmName}\" acpipowerbutton",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
            }
            else
            {
                Log($"VBoxManage не найден: {_cfg.VBoxManagePath}");
            }
        }
        catch (Exception ex)
        {
            Log($"Ошибка выключения ВМ: {ex.Message}");
        }

        ResetToInitial();
    }

    private void ResetToInitial()
    {
        _wasVmRunning = false;
        _printing = false;
        if (_previewOpen) ClosePreview();
        StopBusy("");
        var off = HealthSnapshotOff();
        SetDot(VmDot, VmState, off.Vm, off.VmLabel);
        SetDot(OsDot, OsState, off.Os, off.OsLabel);
        SetDot(PrDot, PrState, off.Printer, off.PrinterLabel);
        ReadyBadge.Visibility = Visibility.Collapsed;
        LauncherPanel.Visibility = Visibility.Visible;
        FooterDot.Fill = (Brush)FindRes("Off");
        PrintButton.IsEnabled = false;
        PrintButton.ToolTip = "Недоступно: запустите виртуальную машину.";
    }

    private static HealthSnapshot HealthSnapshotOff() => new()
    {
        Vm = IndicatorState.Off, VmLabel = "Остановлена",
        Os = IndicatorState.Off, OsLabel = "Не запущен",
        Printer = IndicatorState.Off, PrinterLabel = "Неизвестно",
    };
}
