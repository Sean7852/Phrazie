using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Phrazie.Desktop.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
