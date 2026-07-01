using System.Windows;

namespace CanonPrintBridge;

public partial class ConfirmShutdownWindow : Window
{
    public ConfirmShutdownWindow() => InitializeComponent();

    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
