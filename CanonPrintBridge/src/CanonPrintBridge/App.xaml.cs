using System.Text;
using System.Windows;
using System.Windows.Threading;
using CanonPrintBridge.Services;
using PdfSharp.Fonts;

namespace CanonPrintBridge;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Logger.Init();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // enables Windows-1251 for text files
        GlobalFontSettings.FontResolver = new WindowsFontResolver();   // PdfSharp text rendering
        DispatcherUnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Logger.Write($"FATAL (domain): {args.ExceptionObject}");
        base.OnStartup(e);
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Write($"FATAL (dispatcher): {e.Exception}");
        // Let the default handler tear the app down; the log now holds the details.
    }
}
