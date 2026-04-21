using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class ClipBrowserView : UserControl
{
    private const double DragThreshold = 5.0;

    private Control?   _marqueePanel;
    private Canvas?    _marqueeCanvas;
    private Rectangle? _marqueeRect;
    private Point      _marqueeStart;
    private bool       _isDragging;

    public ClipBrowserView()
    {
        InitializeComponent();
    }

    private ClipBrowserViewModel? VM => DataContext as ClipBrowserViewModel;

    // ── Clip card click: shift / ctrl-aware selection ─────────────────────────

    private void ClipCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;
        if (sender is not Control ctrl || ctrl.DataContext is not BrowserClipItem item) return;

        bool shift   = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool ctrlKey = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        VM?.HandleClipClick(item, shift, ctrlKey);

        e.Handled = true; // prevent bubbling so marquee logic on parent doesn't fire
    }

    // ── Rubber-band (marquee) selection ───────────────────────────────────────

    private void ClipsPanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;
        if (sender is not Control panel) return;

        _marqueePanel  = panel;
        _marqueeCanvas = panel.GetVisualDescendants().OfType<Canvas>().FirstOrDefault();
        _marqueeStart  = e.GetPosition(panel);
        _isDragging    = false;
        _marqueeRect   = null;

        e.Pointer.Capture(panel);
    }

    private void ClipsPanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_marqueePanel is null) return;
        if (!e.GetCurrentPoint(_marqueePanel).Properties.IsLeftButtonPressed)
        {
            ClearMarquee();
            return;
        }

        var current = e.GetPosition(_marqueePanel);
        var delta   = current - _marqueeStart;
        if (Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) < DragThreshold) return;

        _isDragging = true;

        if (_marqueeCanvas is not null)
        {
            if (_marqueeRect is null)
            {
                _marqueeRect = new Rectangle
                {
                    Fill            = new SolidColorBrush(Color.FromArgb(40,  100, 140, 255)),
                    Stroke          = new SolidColorBrush(Color.FromArgb(200, 100, 140, 255)),
                    StrokeThickness = 1,
                };
                _marqueeCanvas.Children.Add(_marqueeRect);
            }

            var r = MakeRect(_marqueeStart, current);
            Canvas.SetLeft(_marqueeRect, r.X);
            Canvas.SetTop(_marqueeRect,  r.Y);
            _marqueeRect.Width  = r.Width;
            _marqueeRect.Height = r.Height;
        }
    }

    private void ClipsPanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);

        if (_isDragging && _marqueePanel is not null)
        {
            var selectionRect = MakeRect(_marqueeStart, e.GetPosition(_marqueePanel));
            ApplyMarqueeSelection(selectionRect);
        }

        ClearMarquee();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ApplyMarqueeSelection(Rect selectionRect)
    {
        var vm = VM;
        if (vm?.CurrentContent is not BrowserClipsContent content) return;

        var itemsControl = _marqueePanel?
            .GetVisualDescendants()
            .OfType<ItemsControl>()
            .FirstOrDefault();
        if (itemsControl is null) return;

        bool any = false;
        for (int i = 0; i < content.Items.Count; i++)
        {
            var container = itemsControl.ContainerFromIndex(i);
            if (container is not Visual vis) continue;

            var pos = vis.TranslatePoint(new Point(0, 0), _marqueePanel!);
            if (pos is null) continue;

            var itemRect = new Rect(pos.Value, vis.Bounds.Size);
            if (selectionRect.Intersects(itemRect))
            {
                content.Items[i].IsSelected = true;
                any = true;
            }
        }

        if (any) vm.NotifySelectionChanged();
    }

    private void ClearMarquee()
    {
        if (_marqueeRect is not null)
        {
            _marqueeCanvas?.Children.Remove(_marqueeRect);
            _marqueeRect = null;
        }
        _marqueePanel  = null;
        _marqueeCanvas = null;
        _isDragging    = false;
    }

    private static Rect MakeRect(Point a, Point b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y),
            Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}
