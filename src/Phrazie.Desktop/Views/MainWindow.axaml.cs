using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class MainWindow : Window
{
    private const int ResizeBorder = 6;

    public MainWindow()
    {
        InitializeComponent();
    }

    private WindowEdge? GetResizeEdge(Point p)
    {
        if (WindowState != WindowState.Normal) return null;
        double w = Bounds.Width, h = Bounds.Height;
        bool l = p.X <= ResizeBorder, r = p.X >= w - ResizeBorder;
        bool t = p.Y <= ResizeBorder, b = p.Y >= h - ResizeBorder;
        return (l, r, t, b) switch
        {
            (true,  false, true,  false) => WindowEdge.NorthWest,
            (false, true,  true,  false) => WindowEdge.NorthEast,
            (true,  false, false, true)  => WindowEdge.SouthWest,
            (false, true,  false, true)  => WindowEdge.SouthEast,
            (true,  false, false, false) => WindowEdge.West,
            (false, true,  false, false) => WindowEdge.East,
            (false, false, true,  false) => WindowEdge.North,
            (false, false, false, true)  => WindowEdge.South,
            _                            => null
        };
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        Cursor = GetResizeEdge(e.GetPosition(this)) switch
        {
            WindowEdge.NorthWest or WindowEdge.SouthEast or
            WindowEdge.NorthEast or WindowEdge.SouthWest => new Cursor(StandardCursorType.SizeAll),
            WindowEdge.West      or WindowEdge.East      => new Cursor(StandardCursorType.SizeWestEast),
            WindowEdge.North     or WindowEdge.South     => new Cursor(StandardCursorType.SizeNorthSouth),
            _                                            => Cursor.Default
        };
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var edge = GetResizeEdge(e.GetPosition(this));
        if (edge.HasValue)
        {
            BeginResizeDrag(edge.Value, e);
            e.Handled = true;
        }
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
