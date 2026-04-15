using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class CollectionDetailView : UserControl
{
    // Static field holds the item being dragged for in-process transfers.
    private static StateItemViewModel? _dragging;

    public CollectionDetailView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, StateBorder_DragOver);
        AddHandler(DragDrop.DropEvent,     StateBorder_Drop);
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

    // ── drag-drop ─────────────────────────────────────────────────────────

    private async void DragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;
        if (sender is not Control { DataContext: StateItemViewModel vm }) return;
        e.Handled = true;

        _dragging = vm;

        var dt = new DataTransfer();
        dt.Add(DataTransferItem.CreateText("phrazie/state"));
        await DragDrop.DoDragDropAsync(e, dt, DragDropEffects.Move);

        _dragging = null;
    }

    private void StateBorder_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = _dragging is not null ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void StateBorder_Drop(object? sender, DragEventArgs e)
    {
        if (_dragging is null) return;
        var target = FindStateItem(e.Source);
        if (target is null || ReferenceEquals(target, _dragging)) return;
        if (DataContext is CollectionDetailViewModel vm) vm.MoveState(_dragging, target);
        e.Handled = true;
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
