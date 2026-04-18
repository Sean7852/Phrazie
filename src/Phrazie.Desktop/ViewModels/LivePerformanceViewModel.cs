using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;
using Avalonia.Threading;

namespace Phrazie.Desktop.ViewModels;

public partial class LivePerformanceViewModel : ViewModelBase
{
    private readonly ISessionService  _session;
    private readonly ITriggerService  _trigger;
    private readonly IPlaybackService _playback;
    private readonly IBeatClock       _beatClock;

    /// <summary>Exposed so LivePerformanceView.axaml.cs can wire it to VideoView.</summary>
    public MediaPlayer? MediaPlayer => (_playback as VideoPlaybackService)?.MediaPlayer;

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

    // ── BPM ───────────────────────────────────────────────────────────────

    [ObservableProperty] private double _bpm = 128;

    partial void OnBpmChanged(double value)
    {
        _ = _session.SetBpmAsync(value);
        _beatClock.Bpm = value;
    }

    // ── Phrase tracker ────────────────────────────────────────────────────

    [ObservableProperty] private double _currentPhase = 0.0;

    // ── ctor ──────────────────────────────────────────────────────────────

    public LivePerformanceViewModel(
        ISessionService  session,
        ITriggerService  trigger,
        IPlaybackService playback,
        IBeatClock       beatClock)
    {
        _session   = session;
        _trigger   = trigger;
        _playback  = playback;
        _beatClock = beatClock;

        BuildStateOptions(_session.Current);
        SyncFromSession(_session.Current);

        _session.SessionChanged += s =>
            Dispatcher.UIThread.Post(() =>
            {
                RebuildOptionsIfCollectionChanged(s);
                SyncFromSession(s);
            });

        _trigger.TriggerFired  += OnTriggerFired;
        _trigger.CountdownTick += OnCountdownTick;

        _playback.ClipChanged += clip =>
            Dispatcher.UIThread.Post(() =>
                CurrentClipName = clip?.DisplayName ?? string.Empty);

        _beatClock.Bpm = Bpm;
        _beatClock.PhaseChanged += phase =>
            Dispatcher.UIThread.Post(
                () => CurrentPhase = phase,
                DispatcherPriority.Render);
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

        var nextOpt = StateOptions.FirstOrDefault(o => o.IsNext);
        if (nextOpt is not null)
        {
            await _session.TransitionToStateAsync(nextOpt.Model);
            await _playback.TransitionToStateAsync(nextOpt.Model);
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
            var opt = StateOptions.FirstOrDefault(o => o.Model.Id == trigger.TargetStateId);
            if (opt is not null)
            {
                await _session.TransitionToStateAsync(opt.Model);
                await _playback.TransitionToStateAsync(opt.Model);
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

    private string BuildTriggerDescription(string stateName) => DelayType switch
    {
        TriggerDelayType.Immediate => $"{stateName} — Now",
        TriggerDelayType.Seconds   => $"{stateName} in {(int)DelayValue}s",
        TriggerDelayType.Bars      => $"{stateName} in {(int)DelayValue} bars",
        _                          => string.Empty
    };
}
