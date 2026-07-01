using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CanonPrintBridge.Models;
using CanonPrintBridge.Services;
using Microsoft.Win32;

namespace CanonPrintBridge;

public partial class MainWindow : Window
{
    private readonly AppConfig _cfg;
    private readonly QueueService _queue;
    private string? _pdfPath;

    private readonly DispatcherTimer _tick;
    private DateTime _opStart;
    private string _opLabel = "";

    public MainWindow()
    {
        InitializeComponent();
        _cfg = AppConfig.Load();
        _queue = new QueueService(_cfg.QueueRoot);
        Log($"Очередь: {_cfg.QueueRoot}");

        _tick = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tick.Tick += (_, _) => StatusText.Text = $"{_opLabel}… {Elapsed():m\\:ss}";
    }

    private TimeSpan Elapsed() => DateTime.Now - _opStart;

    // Включить «бегущий» индикатор и тикающий таймер для текущей операции.
    private void StartBusy(string label)
    {
        _opLabel = label;
        _opStart = DateTime.Now;
        Busy.IsIndeterminate = true;
        StatusText.Text = $"{_opLabel}… 0:00";
        _tick.Start();
    }

    // Остановить индикатор; finalText остаётся в статусе (например, «Готово за 0:07»).
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
    }

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
        Log($"Выбран файл: {Path.GetFileName(path)}");
    }

    private async void Launcher_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(_cfg.LauncherPath))
        {
            MessageBox.Show($"Лаунчер не найден:\n{_cfg.LauncherPath}", "Canon Print Bridge",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LauncherButton.IsEnabled = false;
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
            LauncherButton.IsEnabled = true;
        }
    }

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
            Duplex = DuplexBox.IsChecked == true ? "manual" : "none",
            CreatedAt = DateTime.Now.ToString("s"),
        };

        PrintButton.IsEnabled = false;
        StartBusy("Печать");
        var result = "timeout";
        try
        {
            Log($"Задание {job.Id}: {job.Paper}, копий {job.Copies}, " +
                $"дуплекс {(job.Duplex == "manual" ? "ручной" : "нет")}");
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
            PrintButton.IsEnabled = true;
        }
    }

    // Возвращает финальное состояние: "done" / "error" / "timeout".
    private async Task<string> WaitForCompletionAsync(string id)
    {
        var deadline = DateTime.Now.AddMinutes(10);
        var flipHandled = false;
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

            if (st?.State == "awaiting-flip" && !flipHandled)
            {
                flipHandled = true;
                MessageBox.Show(
                    "Нечётные страницы напечатаны.\n\n" +
                    "Переверни стопку, вставь обратно в лоток и нажми OK — продолжу чётными.",
                    "Ручная двусторонняя печать", MessageBoxButton.OK, MessageBoxImage.Information);
                _queue.SignalContinue(id);
                Log("Сигнал «продолжить» отправлен.");
            }

            switch (st?.State)
            {
                case "done":
                    Log("Готово ✅");
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
}
