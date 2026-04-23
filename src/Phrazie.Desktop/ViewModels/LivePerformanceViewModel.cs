using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;
using Avalonia.Threading;

namespace Phrazie.Desktop.ViewModels;

public partial class LivePerformanceViewModel : ViewModelBase
{
    private readonly ISessionService       _session;
    private readonly ITriggerService       _trigger;
    private readonly IPlaybackService      _playback;
    private readonly IBeatClock            _beatClock;
    private readonly TransitionPoolService _transitionPool;

    // Tracks an out-transition that was pre-started at near-end so AdvanceToNextClip can await it
    private Task? _pendingOutTransition;

    // Phrase-based clip duration tracking
    private int  _beatClockPhrasesCounted;
    private bool _phraseAdvancePending;
    private int  _clipGeneration; // increments each ResetPhraseClock call

    public VideoPlaybackService? VideoService => _playback as VideoPlaybackService;

    /// <summary>Awaited before the clip/state switches — plays the outgoing effect.</summary>
    public event Func<TransitionType, double, Task>? OutTransitionRequired;
    /// <summary>Fired immediately after the clip/state switches — plays the incoming effect.</summary>
    public event Action<TransitionType, double>? InTransitionStarted;

    // ── state options (the three buttons: Normal / Break / Drop) ──────────

    public ObservableCollection<StateOptionViewModel> StateOptions { get; } = new();

    // ── current state display ─────────────────────────────────────────────

    [ObservableProperty] private string _currentStateName  = "—";
    [ObservableProperty] private string _currentClipName   = string.Empty;
    [ObservableProperty] private string _collectionName    = "—";

    /// <summary>Phrazie UI language: Waiting · Locked · Triggered</summary>
    [ObservableProperty] private string _statusLabel       = "Waiting";

    /// <summary>e.g. "Drop in 8 bars" or "Break in 4s"</summary>
    [ObservableProperty] private string _triggerDescription = string.Empty;

    [ObservableProperty] private string _countdownDisplay  = "—";

    // ── next state ────────────────────────────────────────────────────────

    public string NextStateName =>
        StateOptions.FirstOrDefault(o => o.IsNext)?.Name ?? "—";

    // ── trigger scheduling controls ───────────────────────────────────────

    [ObservableProperty] private double           _delayValue = 8;
    [ObservableProperty] private TriggerDelayType _delayType  = TriggerDelayType.Bars;

    public IReadOnlyList<TriggerDelayType> DelayTypes { get; } =
        Enum.GetValues<TriggerDelayType>();

    // ── Transport ─────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isPlaying   = true;
    [ObservableProperty] private bool _isRecording = false;

    public string PlayPauseIcon => IsPlaying ? "⏸" : "▶";

    partial void OnIsPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(PlayPauseIcon));
        if (value)
        {
            _beatClock.Start();
            var first = _session.Current.CurrentState?.Clips.FirstOrDefault(c => c.IsEnabled);
            if (first is not null && _playback.CurrentClip is null)
                PlayClip(first);
        }
        else
        {
            _beatClock.Stop();
            _ = _playback.StopAsync();
        }
    }

    [RelayCommand] private void TogglePlay()   => IsPlaying   = !IsPlaying;
    [RelayCommand] private void ToggleRecord() => IsRecording = !IsRecording;

    // ── BPM ───────────────────────────────────────────────────────────────

    [ObservableProperty] private double _bpm = 128;

    partial void OnBpmChanged(double value)
    {
        _ = _session.SetBpmAsync(value);
        _beatClock.Bpm = value;
    }

    [RelayCommand] private void IncreaseBpm() => Bpm = Math.Min(200, Bpm + 1);
    [RelayCommand] private void DecreaseBpm() => Bpm = Math.Max(60,  Bpm - 1);

    // ── Phrase tracker ────────────────────────────────────────────────────

    [ObservableProperty] private double _currentPhase  = 0.0;
    [ObservableProperty] private int    _phraseNumber  = 1;
    public int TotalPhrases { get; } = 8;

    private double _lastPhase = -1.0;

    // ── ctor ──────────────────────────────────────────────────────────────

    public LivePerformanceViewModel(
        ISessionService       session,
        ITriggerService       trigger,
        IPlaybackService      playback,
        IBeatClock            beatClock,
        TransitionPoolService transitionPool)
    {
        _session        = session;
        _trigger        = trigger;
        _playback       = playback;
        _beatClock      = beatClock;
        _transitionPool = transitionPool;

        BuildStateOptions(_session.Current);
        SyncFromSession(_session.Current);

        _session.SessionChanged += s =>
            Dispatcher.UIThread.Post(() =>
            {
                RebuildOptionsIfCollectionChanged(s);
                SyncFromSession(s);
                AutoPlayIfIdle(s);
            });

        _trigger.TriggerFired  += OnTriggerFired;
        _trigger.CountdownTick += OnCountdownTick;

        _playback.ClipChanged += clip =>
            Dispatcher.UIThread.Post(() =>
                CurrentClipName = clip?.DisplayName ?? string.Empty);

        _playback.ClipEnded += () =>
        {
            var gen = _clipGeneration;
            Dispatcher.UIThread.Post(() => AdvanceToNextClip(gen));
        };

        if (_playback is VideoPlaybackService vps)
            vps.ClipNearEnd += () =>
                Dispatcher.UIThread.Post(() =>
                {
                    if (_pendingOutTransition is null)
                        _pendingOutTransition = FireOutTransitionAsync();
                });

        _beatClock.Bpm = Bpm;
        _beatClock.PhaseChanged += phase =>
            Dispatcher.UIThread.Post(() =>
            {
                if (_lastPhase > 0.9 && phase < 0.1)
                {
                    PhraseNumber = (PhraseNumber % TotalPhrases) + 1;
                    _beatClockPhrasesCounted++;
                }

                var phraseDur = _transitionPool.ClipDurationPhrases;
                if (phraseDur > 0 && !_phraseAdvancePending)
                {
                    // Continuous elapsed time in IBeatClock phrases — gives sub-phrase precision
                    var elapsed  = _beatClockPhrasesCounted + phase;
                    var spp      = 60.0 / Bpm * 16.0; // seconds per IBeatClock phrase
                    var outRatio = _transitionPool.GetMaxOutDuration() / spp;

                    // Start the out transition early so it ends right at the phrase boundary
                    if (_pendingOutTransition is null && elapsed >= phraseDur - outRatio)
                        _pendingOutTransition = FireOutTransitionAsync();

                    // Advance the clip once all phrases are complete
                    if (elapsed >= phraseDur)
                    {
                        _phraseAdvancePending = true;
                        _ = AdvanceByPhraseAsync();
                    }
                }

                _lastPhase   = phase;
                CurrentPhase = phase;
            }, DispatcherPriority.Render);
        _beatClock.Start();
    }

    // ── commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ScheduleTriggerAsync()
    {
        var nextOpt = StateOptions.FirstOrDefault(o => o.IsNext);
        if (nextOpt is null) return;

        var trigger = new Trigger
        {
            TargetStateId = nextOpt.Model.Id,
            DelayType     = DelayType,
            DelayValue    = DelayValue,
        };

        _session.Current.NextState      = nextOpt.Model;
        _session.Current.PendingTrigger = trigger;

        StatusLabel       = "Locked";
        TriggerDescription = BuildTriggerDescription(nextOpt.Name);
        CountdownDisplay  = TriggerDescription;

        await _trigger.ScheduleAsync(trigger, _session.Current.Bpm);
    }

    [RelayCommand]
    private async Task CancelTriggerAsync()
    {
        await _trigger.CancelAsync();
        StatusLabel        = "Waiting";
        TriggerDescription = string.Empty;
        CountdownDisplay   = "—";
    }

    [RelayCommand]
    private async Task EmergencySwitchAsync()
    {
        await _trigger.CancelAsync();
        ResetPhraseClock();

        var nextOpt = StateOptions.FirstOrDefault(o => o.IsNext);
        if (nextOpt is not null)
        {
            await FireOutTransitionAsync();
            await _session.TransitionToStateAsync(nextOpt.Model);
            await _playback.TransitionToStateAsync(nextOpt.Model);
            FireInTransition();
        }

        StatusLabel        = "Waiting";
        TriggerDescription = string.Empty;
        CountdownDisplay   = "—";
    }

    // ── hotkey dispatch (called from MainWindowViewModel) ─────────────────

    internal void HandleHotkeyAction(HotkeyAction action)
    {
        switch (action)
        {
            case HotkeyAction.EmergencySwitch: _ = EmergencySwitchAsync(); break;
            case HotkeyAction.ScheduleTrigger: _ = ScheduleTriggerAsync(); break;
            case HotkeyAction.CancelTrigger:   _ = CancelTriggerAsync();   break;
        }
    }

    // ── called by StateOptionViewModel via callback ───────────────────────

    internal void SelectNextState(StateOptionViewModel selected)
    {
        foreach (var opt in StateOptions)
            opt.IsNext = opt == selected;

        OnPropertyChanged(nameof(NextStateName));
    }

    // ── event handlers ────────────────────────────────────────────────────

    private void OnTriggerFired(Trigger trigger)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            ResetPhraseClock();
            var opt = StateOptions.FirstOrDefault(o => o.Model.Id == trigger.TargetStateId);
            if (opt is not null)
            {
                await FireOutTransitionAsync();
                await _session.TransitionToStateAsync(opt.Model);
                await _playback.TransitionToStateAsync(opt.Model);
                FireInTransition();
            }

            StatusLabel        = "Triggered";
            TriggerDescription = string.Empty;
            CountdownDisplay   = "Triggered";
        });
    }

    private void OnCountdownTick(TimeSpan remaining)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusLabel      = "Locked";
            CountdownDisplay = remaining.TotalSeconds < 60
                ? $"{(int)remaining.TotalSeconds}s"
                : remaining.ToString(@"m\:ss");
        });
    }

    // ── helpers ───────────────────────────────────────────────────────────

    /// <summary>Called by LiveVideoView once the native HWND is ready.</summary>
    public void TriggerAutoPlay() => AutoPlayIfIdle(_session.Current);

    private void AutoPlayIfIdle(Session s)
    {
        if (!IsPlaying || _playback.CurrentClip is not null) return;
        var first = s.CurrentState?.Clips.FirstOrDefault(c => c.IsEnabled);
        if (first is not null)
            PlayClip(first);
    }

    private void BuildStateOptions(Session s)
    {
        StateOptions.Clear();
        foreach (var state in s.ActiveCollection?.States ?? [])
            StateOptions.Add(new StateOptionViewModel(state, SelectNextState));
    }

    private void SyncFromSession(Session s)
    {
        CurrentStateName = s.CurrentState?.Name ?? "—";
        CollectionName   = s.ActiveCollection?.Name ?? "—";
        Bpm              = s.Bpm;

        foreach (var opt in StateOptions)
            opt.IsActive = opt.Model.Id == s.CurrentState?.Id;
    }

    private void RebuildOptionsIfCollectionChanged(Session s)
    {
        var currentIds = StateOptions.Select(o => o.Model.Id).ToHashSet();
        var newIds     = (s.ActiveCollection?.States ?? []).Select(st => st.Id).ToHashSet();

        if (!currentIds.SetEquals(newIds))
        {
            BuildStateOptions(s);
            OnPropertyChanged(nameof(NextStateName));
        }
    }

    // Called from ClipEnded — loops if phrase mode is active and target not reached yet
    private async void AdvanceToNextClip(int generation)
    {
        if (!IsPlaying || generation != _clipGeneration) return;

        var phraseDuration = _transitionPool.ClipDurationPhrases;
        if (phraseDuration > 0 && !_phraseAdvancePending)
        {
            // Phrase target not yet reached — seamlessly loop the same clip
            var current = _playback.CurrentClip;
            if (current is not null) _ = _playback.PlayAsync(current);
            return;
        }

        if (_phraseAdvancePending) return; // AdvanceByPhraseAsync already handling this

        await DoAdvanceToNextClipAsync();
    }

    // Called from the phrase boundary when phrase count target is reached
    private async Task AdvanceByPhraseAsync()
    {
        if (!IsPlaying) return;
        await DoAdvanceToNextClipAsync();
    }

    private async Task DoAdvanceToNextClipAsync()
    {
        var clips = _session.Current.CurrentState?.Clips
                        .Where(c => c.IsEnabled)
                        .ToList() ?? [];
        if (clips.Count == 0) return;

        var currentIdx = clips.FindIndex(c => c.Id == _playback.CurrentClip?.Id);
        var nextIdx    = currentIdx + 1;
        var target     = nextIdx >= clips.Count ? clips[^1] : clips[nextIdx];

        if (_pendingOutTransition is not null)
        {
            await _pendingOutTransition;
            _pendingOutTransition = null;
        }
        else
        {
            await FireOutTransitionAsync();
        }

        PlayClip(target);
        FireInTransition();
    }

    private void ResetPhraseClock()
    {
        _beatClockPhrasesCounted = 0;
        _phraseAdvancePending    = false;
        _pendingOutTransition    = null;
        _clipGeneration++;
    }

    private void PlayClip(Clip clip)
    {
        ResetPhraseClock();

        if (_playback is VideoPlaybackService vps)
            vps.NearEndLookahead = _transitionPool.ClipDurationPhrases == 0
                ? TimeSpan.FromSeconds(_transitionPool.GetMaxOutDuration() + 0.2)
                : TimeSpan.Zero;

        _ = _playback.PlayAsync(clip);
    }

    private async Task FireOutTransitionAsync()
    {
        var (type, duration) = _transitionPool.PickOut();
        if (OutTransitionRequired is not null)
            await OutTransitionRequired(type, duration);
    }

    private void FireInTransition()
    {
        var (type, duration) = _transitionPool.PickIn();
        InTransitionStarted?.Invoke(type, duration);
    }

    private string BuildTriggerDescription(string stateName) => DelayType switch
    {
        TriggerDelayType.Immediate => $"{stateName} — Now",
        TriggerDelayType.Seconds   => $"{stateName} in {(int)DelayValue}s",
        TriggerDelayType.Bars      => $"{stateName} in {(int)DelayValue} bars",
        _                          => string.Empty
    };
}
