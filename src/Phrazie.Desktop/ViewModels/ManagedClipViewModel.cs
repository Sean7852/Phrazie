using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;

namespace Phrazie.Desktop.ViewModels;

/// <summary>
/// Represents one clip inside the Clip Manager window.
/// Supports toggle-enabled and delete, with a greyed-out thumbnail when disabled.
/// </summary>
public partial class ManagedClipViewModel : ObservableObject
{
    private readonly Action<ManagedClipViewModel> _onDelete;
    private readonly Action                       _onChanged;

    public Clip   Model       { get; }
    public string DisplayName => Model.DisplayName;

    [ObservableProperty] private Bitmap? _thumbnail;
    [ObservableProperty] private bool    _isEnabled;
    [ObservableProperty] private bool    _isSelected;

    partial void OnIsEnabledChanged(bool value)
    {
        Model.IsEnabled = value;
        _onChanged();
    }

    public ManagedClipViewModel(
        Clip                          model,
        Action<ManagedClipViewModel>  onDelete,
        Action                        onChanged)
    {
        Model      = model;
        _onDelete  = onDelete;
        _onChanged = onChanged;
        _isEnabled = model.IsEnabled;

        _ = LoadThumbnailAsync();
    }

    [RelayCommand]
    private void ToggleEnabled()
    {
        IsEnabled = !IsEnabled;
    }

    [RelayCommand]
    private void Delete() => _onDelete(this);

    private async Task LoadThumbnailAsync()
    {
        var bmp = await VideoThumbnailService.GetThumbnailAsync(Model.FilePath, 160, 90);
        if (bmp is not null)
            await Dispatcher.UIThread.InvokeAsync(() => Thumbnail = bmp);
    }
}
