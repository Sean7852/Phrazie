using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Phrazie.Desktop.Services;

public sealed class AvaloniaFilePickerService : IFilePickerService
{
    private static readonly FilePickerFileType ImageTypes = new("Images")
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif", "*.bmp"]
    };

    private static readonly FilePickerFileType VideoTypes = new("Video Files")
    {
        Patterns = ["*.mp4", "*.mov", "*.avi", "*.mkv", "*.webm", "*.wmv", "*.flv", "*.m4v"]
    };

    public async Task<string?> PickImageAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title          = "Select Cover Image",
            AllowMultiple  = false,
            FileTypeFilter = [ImageTypes]
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    public async Task<IReadOnlyList<string>> PickVideoFilesAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return [];

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title          = "Import Video Clips",
            AllowMultiple  = true,
            FileTypeFilter = [VideoTypes]
        });

        return files.Select(f => f.Path.LocalPath).ToList();
    }

    private static TopLevel? GetTopLevel() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } w }
            ? TopLevel.GetTopLevel(w)
            : null;
}
