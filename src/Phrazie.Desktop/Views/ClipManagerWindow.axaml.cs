using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class ClipManagerWindow : Window
{
    public ClipManagerWindow()
    {
        InitializeComponent();

        DoneButton.Click += (_, _) => Close();

        DropTarget.AddHandler(DragDrop.DropEvent,    OnDrop);
        DropTarget.AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
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

    private static bool IsVideoFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".mp4" or ".mov" or ".avi" or ".mkv" or ".webm" or ".wmv" or ".m4v" or ".flv";
    }
}
