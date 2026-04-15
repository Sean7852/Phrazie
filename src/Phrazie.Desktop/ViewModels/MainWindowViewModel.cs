using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;

namespace Phrazie.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ICollectionRepository _collections;
    private readonly ISessionService _session;
    private readonly ITriggerService _trigger;
    private readonly IPlaybackService _playback;
    private readonly IFilePickerService _filePicker;

    [ObservableProperty] private ViewModelBase _currentPage;
    [ObservableProperty] private bool _isCollectionsActive = true;
    [ObservableProperty] private bool _isLiveActive        = false;
    [ObservableProperty] private bool _isHelpActive        = false;

    public MainWindowViewModel(
        ICollectionRepository collections,
        ISessionService session,
        ITriggerService trigger,
        IPlaybackService playback,
        IFilePickerService filePicker)
    {
        _collections = collections;
        _session     = session;
        _trigger     = trigger;
        _playback    = playback;
        _filePicker  = filePicker;

        _currentPage = BuildCollectionsPage();
    }

    // ── nav commands ───────────────────────────────────────────────────────

    [RelayCommand]
    private void GoToCollections()
    {
        CurrentPage         = BuildCollectionsPage();
        IsCollectionsActive = true;
        IsLiveActive        = false;
        IsHelpActive        = false;
    }

    [RelayCommand]
    private void GoToLive()
    {
        CurrentPage         = new LivePerformanceViewModel(_session, _trigger, _playback);
        IsCollectionsActive = false;
        IsLiveActive        = true;
        IsHelpActive        = false;
    }

    [RelayCommand]
    private void GoToHelp()
    {
        CurrentPage         = new HelpViewModel();
        IsCollectionsActive = false;
        IsLiveActive        = false;
        IsHelpActive        = true;
    }

    // ── collection detail navigation ───────────────────────────────────────

    private void GoToCollectionDetail(Collection collection)
    {
        CurrentPage         = new CollectionDetailViewModel(collection, _collections, GoToCollections);
        IsCollectionsActive = false;
        IsLiveActive        = false;
        IsHelpActive        = false;
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private CollectionsViewModel BuildCollectionsPage() =>
        new(_collections, _session, _filePicker, GoToCollectionDetail);
}
