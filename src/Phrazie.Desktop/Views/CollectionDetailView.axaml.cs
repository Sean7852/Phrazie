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

    private async void DragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;
        if (sender is not Control { DataContext: StateItemViewModel vm }) return;
        e.Handled = true;

        _dragging = vm;

        // DataTransfer is the Avalonia 12 replacement for DataObject.
        // We just need any payload to initiate the drag session.
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
