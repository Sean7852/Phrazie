using Avalonia.Controls;
using Avalonia.Input;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Don't fire global hotkeys when typing in a text input
        if (e.Source is TextBox or NumericUpDown) return;

        if (DataContext is MainWindowViewModel vm)
            vm.HandleKeyDown(e.Key.ToString());
    }
}