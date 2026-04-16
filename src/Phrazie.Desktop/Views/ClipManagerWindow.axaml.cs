using System.ComponentModel;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class ClipManagerWindow : Window
{
    // ── marquee state ─────────────────────────────────────────────────────────
    private bool  _marqueeActive;
    private Point _marqueeStart;
    private Point _lastPointerPos;   // kept for context-menu hit-testing

    public ClipManagerWindow()
    {
        InitializeComponent();

        DoneButton.Click += (_, _) => Close();

        // File drag-drop from OS
        DropTarget.AddHandler(DragDrop.DropEvent,     OnFileDrop);
        DropTarget.AddHandler(DragDrop.DragOverEvent, OnFileDragOver);

        // Marquee / selection pointer events on the clip canvas
        ClipCanvas.AddHandler(PointerPressedEvent,  OnCanvasPointerPressed,  handledEventsToo: true);
        ClipCanvas.AddHandler(PointerMovedEvent,    OnCanvasPointerMoved,    handledEventsToo: true);
        ClipCanvas.AddHandler(PointerReleasedEvent, OnCanvasPointerReleased, handledEventsToo: true);
    }

    // ── marquee selection ─────────────────────────────────────────────────────

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(ClipCanvas).Properties;
        if (!props.IsLeftButtonPressed) return;   // right-click goes to ContextMenu

        _lastPointerPos  = e.GetPosition(ClipCanvas);
        _marqueeStart    = _lastPointerPos;
        _marqueeActive   = true;

        // Clear existing selection unless Ctrl is held
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            ClearSelection();

        MarqueeRect.IsVisible = false;
        e.Pointer.Capture(ClipCanvas);
        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        _lastPointerPos = e.GetPosition(ClipCanvas);
        if (!_marqueeActive) return;

        var current = _lastPointerPos;
        var x = Math.Min(_marqueeStart.X, current.X);
        var y = Math.Min(_marqueeStart.Y, current.Y);
        var w = Math.Abs(current.X - _marqueeStart.X);
        var h = Math.Abs(current.Y - _marqueeStart.Y);

        // Show marquee only once it's a meaningful size
        if (w > 4 || h > 4)
        {
            Canvas.SetLeft(MarqueeRect, x);
            Canvas.SetTop(MarqueeRect, y);
            MarqueeRect.Width  = w;
            MarqueeRect.Height = h;
            MarqueeRect.IsVisible = true;

            UpdateSelectionFromRect(new Rect(x, y, w, h));
        }

        e.Handled = true;
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_marqueeActive) return;

        _marqueeActive        = false;
        MarqueeRect.IsVisible = false;
        e.Pointer.Capture(null);

        var releasePos = e.GetPosition(ClipCanvas);
        var dx = releasePos.X - _marqueeStart.X;
        var dy = releasePos.Y - _marqueeStart.Y;

        // Tiny move (< 5 px) → treat as a click: toggle the clip under the cursor
        if (dx * dx + dy * dy < 25)
        {
            ClearSelection();
            var hit = FindClipVm(ClipCanvas.InputHitTest(releasePos) as Visual);
            if (hit is not null) hit.IsSelected = true;
        }

        e.Handled = true;
    }

    // ── context menu ─────────────────────────────────────────────────────────

    private void SelectionMenu_Opening(object? sender, CancelEventArgs e)
    {
        if (DataContext is not ClipManagerViewModel vm) return;

        // Auto-select the item under the cursor if nothing is highlighted yet
        if (!vm.Clips.Any(c => c.IsSelected))
        {
            var target = FindClipVm(ClipCanvas.InputHitTest(_lastPointerPos) as Visual);
            if (target is not null) target.IsSelected = true;
        }

        var count = vm.Clips.Count(c => c.IsSelected);
        if (count == 0) { e.Cancel = true; return; }

        DeleteSelectionItem.Header  = count == 1 ? "Delete" : $"Delete {count} clips";
        DeleteSelectionItem.Command = vm.DeleteSelectedCommand;
    }

    // ── file drag-drop from OS ────────────────────────────────────────────────

    private static void OnFileDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnFileDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not ClipManagerViewModel vm) return;
        if (!e.DataTransfer.Contains(DataFormat.File)) return;

        var files = e.DataTransfer.TryGetFiles();
        if (files is null) return;

        var videoPaths = files
            .OfType<IStorageFile>()
            .Select(f => f.Path.LocalPath)
            .Where(IsVideoFile)
            .ToList();

        if (videoPaths.Count > 0)
            await vm.AddClipsFromPathsAsync(videoPaths);

        e.Handled = true;
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks each clip whose container intersects <paramref name="selRect"/> as selected.
    /// Uses the ContentPresenter generated by the ItemsControl as the hit-box.
    /// </summary>
    private void UpdateSelectionFromRect(Rect selRect)
    {
        foreach (var descendant in ClipCanvas.GetVisualDescendants())
        {
            if (descendant is not ContentPresenter { DataContext: ManagedClipViewModel vm })
                continue;

            var matrix = descendant.TransformToVisual(ClipCanvas);
            if (matrix is null) continue;

            var origin = new Point(0, 0).Transform(matrix.Value);
            var bounds = new Rect(origin, descendant.Bounds.Size);
            vm.IsSelected = selRect.Intersects(bounds);
        }
    }

    private void ClearSelection()
    {
        if (DataContext is not ClipManagerViewModel vm) return;
        foreach (var c in vm.Clips) c.IsSelected = false;
    }

    private static ManagedClipViewModel? FindClipVm(Visual? v)
    {
        var current = v;
        while (current is not null)
        {
            if (current is StyledElement { DataContext: ManagedClipViewModel vm }) return vm;
            current = current.GetVisualParent();
        }
        return null;
    }

    private static bool IsVideoFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".mp4" or ".mov" or ".avi" or ".mkv"
                   or ".webm" or ".wmv" or ".m4v" or ".flv";
    }
}
