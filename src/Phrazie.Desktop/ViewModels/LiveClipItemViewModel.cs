using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;

namespace Phrazie.Desktop.ViewModels;

public partial class LiveClipItemViewModel : ViewModelBase
{
    public Clip Model { get; }

    private readonly Action<LiveClipItemViewModel> _requestRemove;

    [ObservableProperty] private bool     _isActive;
    [ObservableProperty] private double   _opacity = 1.0;
    [ObservableProperty] private Bitmap?  _thumbnail;

    public string Name           => Model.DisplayName;
    public string CollectionName { get; }
    public string StateName      { get; }
    public string StateColor     { get; }

    // 25% opacity fill for the badge background, full color for the text
    public string StateColorBg   => StateColor.Length == 7
        ? $"#40{StateColor[1..]}"   // #RRGGBB → #40RRGGBB
        : StateColor;
    public string StateColorFg   => StateColor;

    public string Duration       => Model.Duration > TimeSpan.Zero
        ? Model.Duration.ToString(@"m\:ss")
        : "—";

    public LiveClipItemViewModel(Clip model, Action<LiveClipItemViewModel> requestRemove,
                                  string collectionName = "—", string stateName = "—",
                                  string stateColor = "#443366")
    {
        Model          = model;
        _requestRemove = requestRemove;
        CollectionName = collectionName;
        StateName      = stateName;
        StateColor     = stateColor;

        _ = LoadThumbnailAsync();
    }

    private async Task LoadThumbnailAsync()
    {
        var bmp = await VideoThumbnailService.GetThumbnailAsync(Model.FilePath, 160, 90);
        if (bmp is not null)
            await Dispatcher.UIThread.InvokeAsync(() => Thumbnail = bmp);
    }

    [RelayCommand]
    private void Remove() => _requestRemove(this);
}
