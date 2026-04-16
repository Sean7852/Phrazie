using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty && MaxBtn is not null)
            MaxBtn.Content = WindowState == WindowState.Maximized ? "\u2750" : "\u25A1";
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Don't fire global hotkeys when typing in a text input
        if (e.Source is TextBox or NumericUpDown) return;

        if (DataContext is MainWindowViewModel vm)
            vm.HandleKeyDown(e.Key.ToString());
    }

    // ── Title-bar chrome handlers ─────────────────────────────────────────────

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void TitleBar_DoubleTapped(object? sender, TappedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void MinBtn_Click(object? sender, RoutedEventArgs e)   => WindowState = WindowState.Minimized;
    private void MaxBtn_Click(object? sender, RoutedEventArgs e)   =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseBtn_Click(object? sender, RoutedEventArgs e) => Close();
}
