using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Interfaces;

namespace Phrazie.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ICollectionRepository _collections;
    private readonly ISessionService _session;
    private readonly ITriggerService _trigger;
    private readonly IPlaybackService _playback;

    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private bool _isCollectionsActive = true;

    [ObservableProperty]
    private bool _isLiveActive = false;

    public MainWindowViewModel(
        ICollectionRepository collections,
        ISessionService session,
        ITriggerService trigger,
        IPlaybackService playback)
    {
        _collections = collections;
        _session     = session;
        _trigger     = trigger;
        _playback    = playback;

        _currentPage = new CollectionsViewModel(_collections, _session);
    }

    [RelayCommand]
    private void GoToCollections()
    {
        CurrentPage          = new CollectionsViewModel(_collections, _session);
        IsCollectionsActive  = true;
        IsLiveActive         = false;
    }

    [RelayCommand]
    private void GoToLive()
    {
        CurrentPage         = new LivePerformanceViewModel(_session, _trigger, _playback);
        IsCollectionsActive = false;
        IsLiveActive        = true;
    }
}
