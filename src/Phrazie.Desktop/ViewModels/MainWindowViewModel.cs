using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;

namespace Phrazie.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ICollectionRepository _collections;
    private readonly ISessionService       _session;
    private readonly ITriggerService       _trigger;
    private readonly IPlaybackService      _playback;
    private readonly IBeatClock            _beatClock;
    private readonly ISessionStore         _sessionStore;
    private readonly IAuthService          _auth;

    [ObservableProperty] private bool           _isAuthenticated;
    [ObservableProperty] private LoginViewModel _loginPage;

    private readonly IFilePickerService    _filePicker;
    private readonly IHotkeyService        _hotkeys;

    [ObservableProperty] private ViewModelBase          _currentPage;
    [ObservableProperty] private ClipBrowserViewModel? _activeBrowser;
    [ObservableProperty] private bool _isCollectionsActive  = true;
    [ObservableProperty] private bool _isLiveActive         = false;
    [ObservableProperty] private bool _isClipQueueActive    = false;
    [ObservableProperty] private bool _isSettingsActive     = false;
    [ObservableProperty] private bool _isHelpActive         = false;

    public MainWindowViewModel(
        ICollectionRepository collections,
        ISessionService       session,
        ITriggerService       trigger,
        IPlaybackService      playback,
        IBeatClock            beatClock,
        IFilePickerService    filePicker,
        IHotkeyService        hotkeys,
        ISessionStore         sessionStore,
        IAuthService          auth,
        LoginViewModel        loginPage)
    {
        _collections  = collections;
        _session      = session;
        _trigger      = trigger;
        _playback     = playback;
        _beatClock    = beatClock;
        _filePicker   = filePicker;
        _hotkeys      = hotkeys;
        _sessionStore = sessionStore;
        _auth         = auth;
        _loginPage    = loginPage;
        _currentPage  = BuildCollectionsPage();

        _isAuthenticated = sessionStore.IsAuthenticated;

        WeakReferenceMessenger.Default.Register<OpenClipBrowserMessage>(this, (_, msg) =>
        {
            ActiveBrowser = new ClipBrowserViewModel(
                _collections,
                clip  => { msg.Value(clip); ActiveBrowser = null; },
                ()    => ActiveBrowser = null);
        });

        loginPage.LoginSucceeded += () =>
        {
            IsAuthenticated = true;
            CurrentPage     = BuildCollectionsPage();
        };

        sessionStore.AuthStateChanged += () =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                IsAuthenticated = sessionStore.IsAuthenticated);
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        await _auth.SignOutAsync();
        IsAuthenticated = false;
        CurrentPage     = BuildCollectionsPage();
    }

    // ── nav commands ───────────────────────────────────────────────────────

    [RelayCommand]
    private void GoToCollections()
    {
        CurrentPage         = BuildCollectionsPage();
        IsCollectionsActive = true;
        IsLiveActive        = false;
        IsClipQueueActive   = false;
        IsSettingsActive    = false;
        IsHelpActive        = false;
    }

    [RelayCommand]
    private void GoToLive()
    {
        CurrentPage         = new LivePerformanceViewModel(_session, _trigger, _playback, _beatClock);
        IsCollectionsActive = false;
        IsLiveActive        = true;
        IsClipQueueActive   = false;
        IsSettingsActive    = false;
        IsHelpActive        = false;
    }

    [RelayCommand]
    private void GoToClipQueue()
    {
        CurrentPage         = new ClipQueueViewModel(_session, _playback);
        IsCollectionsActive = false;
        IsLiveActive        = false;
        IsClipQueueActive   = true;
        IsSettingsActive    = false;
        IsHelpActive        = false;
    }

    [RelayCommand]
    private void GoToSettings()
    {
        CurrentPage         = new SettingsViewModel(_hotkeys, _sessionStore, SignOutAsync);
        IsCollectionsActive = false;
        IsLiveActive        = false;
        IsClipQueueActive   = false;
        IsSettingsActive    = true;
        IsHelpActive        = false;
    }

    [RelayCommand]
    private void GoToHelp()
    {
        CurrentPage         = new HelpViewModel();
        IsCollectionsActive = false;
        IsLiveActive        = false;
        IsClipQueueActive   = false;
        IsSettingsActive    = false;
        IsHelpActive        = true;
    }

    // ── global hotkey routing ──────────────────────────────────────────────

    /// <summary>Called by MainWindow.OnKeyDown with the Avalonia Key name.</summary>
    public void HandleKeyDown(string keyName)
    {
        var action = _hotkeys.HandleKeyPress(keyName);
        if (action is null) return;

        switch (action.Value)
        {
            case HotkeyAction.GoToCollections: GoToCollections(); break;
            case HotkeyAction.GoToLive:        GoToLive();        break;
            // EmergencySwitch / ScheduleTrigger / CancelTrigger delegate to LivePerformanceViewModel
            case HotkeyAction.EmergencySwitch or
                 HotkeyAction.ScheduleTrigger  or
                 HotkeyAction.CancelTrigger
                when CurrentPage is LivePerformanceViewModel live:
                live.HandleHotkeyAction(action.Value);
                break;
        }
    }

    // ── collection detail navigation ───────────────────────────────────────

    private void GoToCollectionDetail(Collection collection)
    {
        CurrentPage         = new CollectionDetailViewModel(collection, _collections, GoToCollections);
        IsCollectionsActive = false;
        IsLiveActive        = false;
        IsSettingsActive    = false;
        IsHelpActive        = false;
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private CollectionsViewModel BuildCollectionsPage() =>
        new(_collections, _session, GoToCollectionDetail);
}
