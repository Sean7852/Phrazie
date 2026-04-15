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

    public async Task<string?> PickImageAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title         = "Select Cover Image",
            AllowMultiple = false,
            FileTypeFilter = [ImageTypes]
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    private static TopLevel? GetTopLevel() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } w }
            ? TopLevel.GetTopLevel(w)
            : null;
}
