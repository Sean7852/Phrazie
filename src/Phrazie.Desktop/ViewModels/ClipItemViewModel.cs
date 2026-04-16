using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;

namespace Phrazie.Desktop.ViewModels;

public partial class ClipItemViewModel : ObservableObject
{
    private readonly Action<ClipItemViewModel> _onUnassign;

    public Clip Model { get; }
    public string DisplayName => Model.DisplayName;
    public string Duration    => Model.Duration == TimeSpan.Zero
        ? "—"
        : Model.Duration.ToString(@"m\:ss");

    [ObservableProperty] private Bitmap? _thumbnail;

    public IRelayCommand UnassignCommand { get; }

    public ClipItemViewModel(Clip model, Action<ClipItemViewModel> onUnassign)
    {
        Model           = model;
        _onUnassign     = onUnassign;
        UnassignCommand = new RelayCommand(() => _onUnassign(this));

        _ = LoadThumbnailAsync();
    }

    private async Task LoadThumbnailAsync()
    {
        var bmp = await VideoThumbnailService.GetThumbnailAsync(Model.FilePath, 160, 90);
        if (bmp is not null)
            await Dispatcher.UIThread.InvokeAsync(() => Thumbnail = bmp);
    }
}
