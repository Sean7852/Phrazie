using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;

namespace Phrazie.Desktop.ViewModels;

public partial class ClipQueueViewModel : ViewModelBase,
    IRecipient<ClipsChangedMessage>
{
    private readonly ISessionService       _session;
    private readonly IPlaybackService      _playback;
    private readonly TransitionPoolService _transitionPool;

    private readonly Dictionary<Guid, (string Collection, string State, string Color)> _clipMeta = new();

    public ObservableCollection<LiveClipItemViewModel> Clips { get; } = new();

    public ObservableCollection<TransitionOptionViewModel> OutTransitionOptions { get; } = new()
    {
        new(TransitionType.Fade,   "Fade",   isArmed: true),
        new(TransitionType.Cut,    "Cut",    isArmed: false),
        new(TransitionType.Strobe, "Strobe", isArmed: false),
        new(TransitionType.Glitch, "Glitch", isArmed: false),
        new(TransitionType.Blur,   "Blur",   isArmed: false),
        new(TransitionType.Invert, "Invert", isArmed: false),
    };

    public ObservableCollection<TransitionOptionViewModel> InTransitionOptions { get; } = new()
    {
        new(TransitionType.Fade,   "Fade",   isArmed: false),
        new(TransitionType.Cut,    "Cut",    isArmed: true),
        new(TransitionType.Strobe, "Strobe", isArmed: false),
        new(TransitionType.Glitch, "Glitch", isArmed: false),
        new(TransitionType.Blur,   "Blur",   isArmed: false),
        new(TransitionType.Invert, "Invert", isArmed: false),
    };

    // ── Pool state ────────────────────────────────────────────────────────
    [ObservableProperty] private bool     _isGridView      = false;
    [ObservableProperty] private bool     _isFixedDuration = true;
    [ObservableProperty] private double   _fixedDuration   = 1.0;
    [ObservableProperty] private decimal? _minDuration     = 0.5m;
    [ObservableProperty] private decimal? _maxDuration     = 2.0m;
    [ObservableProperty] private string   _nowPlayingName  = "—";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhraseBeatsDisplay))]
    private int _clipDurationPhrases = 4;

    /// <summary>e.g. "16 bars · 64 beats"</summary>
    public string PhraseBeatsDisplay =>
        $"{ClipDurationPhrases * 4} bars · {ClipDurationPhrases * 16} beats";

    [RelayCommand] private void SetListView()       => IsGridView      = false;
    [RelayCommand] private void SetGridView()       => IsGridView      = true;
    [RelayCommand] private void SetFixedDuration()  => IsFixedDuration = true;
    [RelayCommand] private void SetRandomDuration() => IsFixedDuration = false;

    // Push duration settings whenever any relevant property changes
    partial void OnIsFixedDurationChanged(bool _)     => PushDurationToService();
    partial void OnFixedDurationChanged(double _)     => PushDurationToService();
    partial void OnMinDurationChanged(decimal? _)     => PushDurationToService();
    partial void OnMaxDurationChanged(decimal? _)     => PushDurationToService();
    partial void OnClipDurationPhrasesChanged(int _)  => PushClipDurationToService();

    public ClipQueueViewModel(
        ISessionService       session,
        IPlaybackService      playback,
        TransitionPoolService transitionPool)
    {
        _session        = session;
        _playback       = playback;
        _transitionPool = transitionPool;

        // Subscribe to armed-state changes on each toggle
        foreach (var opt in OutTransitionOptions)
            opt.PropertyChanged += (_, _) => PushOutArmedToService();
        foreach (var opt in InTransitionOptions)
            opt.PropertyChanged += (_, _) => PushInArmedToService();

        // Push initial state
        PushOutArmedToService();
        PushInArmedToService();
        PushDurationToService();
        PushClipDurationToService();

        WeakReferenceMessenger.Default.Register(this);

        LoadClips();
        UpdateActiveClip(_playback.CurrentClip);

        _session.SessionChanged += _ =>
            Dispatcher.UIThread.Post(LoadClips);

        _playback.ClipChanged += clip =>
            Dispatcher.UIThread.Post(() => UpdateActiveClip(clip));
    }

    // ── IRecipient ─────────────────────────────────────────────────────────

    public void Receive(ClipsChangedMessage message)
    {
        var currentStateId = _session.Current.CurrentState?.Id;
        if (message.Value.Id == currentStateId)
            Dispatcher.UIThread.Post(LoadClips);
    }

    // ── Add clip ───────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddClip()
    {
        var state = _session.Current.CurrentState;

        WeakReferenceMessenger.Default.Send(new OpenClipBrowserMessage(selected =>
        {
            if (state is null) return;
            foreach (var info in selected)
            {
                _clipMeta[info.Clip.Id] = (info.CollectionName, info.StateName, info.StateColor);
                state.Clips.Add(info.Clip);
                Clips.Add(new LiveClipItemViewModel(info.Clip, RequestRemove,
                              info.CollectionName, info.StateName, info.StateColor));
            }
            WeakReferenceMessenger.Default.Send(new ClipsChangedMessage(state));
        }));
    }

    // ── Internals ──────────────────────────────────────────────────────────

    private void PushOutArmedToService() =>
        _transitionPool.SetArmedOut(OutTransitionOptions.Where(o => o.IsArmed).Select(o => o.Type));

    private void PushInArmedToService() =>
        _transitionPool.SetArmedIn(InTransitionOptions.Where(o => o.IsArmed).Select(o => o.Type));

    private void PushDurationToService() =>
        _transitionPool.SetDuration(
            IsFixedDuration,
            FixedDuration,
            (double)(MinDuration ?? 0.5m),
            (double)(MaxDuration ?? 2.0m));

    private void PushClipDurationToService() =>
        _transitionPool.ClipDurationPhrases = ClipDurationPhrases;

    private void LoadClips()
    {
        Clips.Clear();

        var defaultCollection = _session.Current.ActiveCollection?.Name ?? "—";
        var defaultState      = _session.Current.CurrentState?.Name      ?? "—";
        var defaultColor      = _session.Current.CurrentState?.Color     ?? "#443366";

        foreach (var clip in _session.Current.CurrentState?.Clips ?? [])
        {
            var (col, st, color) = _clipMeta.TryGetValue(clip.Id, out var meta)
                ? meta
                : (defaultCollection, defaultState, defaultColor);
            Clips.Add(new LiveClipItemViewModel(clip, RequestRemove, col, st, color));
        }
    }

    private void UpdateActiveClip(Clip? clip)
    {
        foreach (var item in Clips)
            item.IsActive = item.Model.Id == clip?.Id;
        NowPlayingName = clip?.DisplayName ?? "—";
    }

    private async void RequestRemove(LiveClipItemViewModel item)
    {
        item.Opacity = 0;
        await Task.Delay(220);
        _clipMeta.Remove(item.Model.Id);
        _session.Current.CurrentState?.Clips.Remove(item.Model);
        Clips.Remove(item);
        WeakReferenceMessenger.Default.Send(
            new ClipsChangedMessage(_session.Current.CurrentState ?? new State()));
    }
}
