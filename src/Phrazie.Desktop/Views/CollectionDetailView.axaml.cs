using System.ComponentModel;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class CollectionDetailView : UserControl
{
    // Tracks the state card being reordered via pointer-drag.
    private StateItemViewModel? _dragging;

    public CollectionDetailView()
    {
        InitializeComponent();
        // File-drop from OS file manager (still uses the DragDrop system)
        AddHandler(DragDrop.DragOverEvent, FileDragOver);
        AddHandler(DragDrop.DropEvent,     FileDrop);
        // State reorder — pointer-capture approach (bypasses AllowDrop routing issues)
        AddHandler(PointerMovedEvent,   OnPointerMoved,   handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnPointerReleased, handledEventsToo: true);
    }

    // ── auto-focus inline editors when editing mode activates ─────────────

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is CollectionDetailViewModel vm)
            vm.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not CollectionDetailViewModel vm) return;

        if (e.PropertyName == nameof(CollectionDetailViewModel.IsEditingName) && vm.IsEditingName)
        {
            // Defer until after layout so the TextBox is visible
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                NameTextBox?.Focus();
                if (NameTextBox is not null)
                {
                    NameTextBox.SelectionStart = NameTextBox.Text?.Length ?? 0;
                    NameTextBox.SelectionEnd   = NameTextBox.Text?.Length ?? 0;
                }
            });
        }

        if (e.PropertyName == nameof(CollectionDetailViewModel.IsEditingDescription) && vm.IsEditingDescription)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                DescriptionTextBox?.Focus();
                if (DescriptionTextBox is not null)
                {
                    DescriptionTextBox.SelectionStart = DescriptionTextBox.Text?.Length ?? 0;
                    DescriptionTextBox.SelectionEnd   = DescriptionTextBox.Text?.Length ?? 0;
                }
            });
        }
    }

    // ── inline name TextBox ────────────────────────────────────────────────

    private void NameTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is CollectionDetailViewModel vm)
            vm.CommitNameCommand.Execute(null);
    }

    private void NameTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not CollectionDetailViewModel vm) return;
        if (e.Key == Key.Enter)  { vm.CommitNameCommand.Execute(null);  e.Handled = true; }
        if (e.Key == Key.Escape) { vm.CancelEditNameCommand.Execute(null); e.Handled = true; }
    }

    // ── inline description TextBox ─────────────────────────────────────────

    private void DescriptionTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is CollectionDetailViewModel vm)
            vm.CommitDescriptionCommand.Execute(null);
    }

    private void DescriptionTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not CollectionDetailViewModel vm) return;
        // Shift+Enter inserts a newline; plain Enter saves
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
        {
            vm.CommitDescriptionCommand.Execute(null);
            e.Handled = true;
        }
        if (e.Key == Key.Escape) { vm.CancelEditDescriptionCommand.Execute(null); e.Handled = true; }
    }

    // ── state-reorder drag (pointer-capture, no OS DragDrop) ─────────────────

    private void DragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;
        if (sender is not StyledElement { DataContext: StateItemViewModel vm }) return;

        _dragging = vm;
        // Capture to this UserControl so PointerMoved/Released always arrive here,
        // even when the cursor leaves the originating element.
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragging is null) return;
        e.Handled = true;   // suppress other hover effects while dragging
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragging is null) return;

        var source = _dragging;
        _dragging = null;
        e.Pointer.Capture(null);

        var target = HitTestStateItem(e.GetPosition(this));
        if (target is not null && !ReferenceEquals(target, source))
            if (DataContext is CollectionDetailViewModel detailVm)
                detailVm.MoveState(source, target);

        e.Handled = true;
    }

    // ── file drop from OS file manager ────────────────────────────────────────

    private void FileDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void FileDrop(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Contains(DataFormat.File)) return;

        var targetState = HitTestStateItem(e.GetPosition(this));
        if (targetState is null) return;

        var files = e.DataTransfer.TryGetFiles();
        if (files is null) return;

        var paths = files
            .OfType<IStorageFile>()
            .Select(f => f.Path.LocalPath)
            .Where(IsVideoFile)
            .ToList();

        if (paths.Count > 0)
            _ = targetState.AddClipsFromPathsAsync(paths);

        e.Handled = true;
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private StateItemViewModel? HitTestStateItem(Point position)
    {
        var hit = this.InputHitTest(position);
        return hit is Visual v ? FindStateItem(v) : null;
    }

    private static bool IsVideoFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".mp4" or ".mov" or ".avi" or ".mkv"
                   or ".wmv" or ".webm" or ".m4v" or ".flv";
    }

    private static StateItemViewModel? FindStateItem(object? element)
    {
        var current = element as Visual;
        while (current is not null)
        {
            if (current is StyledElement { DataContext: StateItemViewModel svm }) return svm;
            current = current.GetVisualParent();
        }
        return null;
    }
}
