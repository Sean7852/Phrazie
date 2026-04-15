using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

public partial class LivePerformanceViewModel : ViewModelBase
{
    private readonly ISessionService _session;
    private readonly ITriggerService _trigger;
    private readonly IPlaybackService _playback;

    // ── current performance state ──────────────────────────────────────────

    [ObservableProperty] private string _currentStateName = "—";
    [ObservableProperty] private string _nextStateName    = "—";
    [ObservableProperty] private string _countdownDisplay = "—";
    [ObservableProperty] private TriggerStatus _triggerStatus = TriggerStatus.Idle;

    // ── trigger scheduling controls ────────────────────────────────────────

    [ObservableProperty] private double _delayValue = 8;
    [ObservableProperty] private TriggerDelayType _delayType = TriggerDelayType.Bars;

    public IReadOnlyList<TriggerDelayType> DelayTypes { get; } =
        Enum.GetValues<TriggerDelayType>();

    // ── next-state candidates (populated from session's active collection) ─

    private State? _selectedNextState;
    public State? SelectedNextState
    {
        get => _selectedNextState;
        set => SetProperty(ref _selectedNextState, value);
    }

    public IReadOnlyList<State> AvailableStates =>
        _session.Current.ActiveCollection?.States ?? [];

    public LivePerformanceViewModel(
        ISessionService session,
        ITriggerService trigger,
        IPlaybackService playback)
    {
        _session  = session;
        _trigger  = trigger;
        _playback = playback;

        SyncFromSession(_session.Current);

        _session.SessionChanged += s => Avalonia.Threading.Dispatcher.UIThread.Post(() => SyncFromSession(s));
        _trigger.TriggerFired   += OnTriggerFired;
        _trigger.CountdownTick  += OnCountdownTick;
    }

    // ── commands ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ScheduleTriggerAsync()
    {
        if (SelectedNextState is null) return;

        var trigger = new Trigger
        {
            TargetStateId = SelectedNextState.Id,
            DelayType     = DelayType,
            DelayValue    = DelayValue,
        };

        _session.Current.NextState      = SelectedNextState;
        _session.Current.PendingTrigger = trigger;

        NextStateName = SelectedNextState.Name;
        TriggerStatus = TriggerStatus.Scheduled;
        CountdownDisplay = FormatDelay();

        await _trigger.ScheduleAsync(trigger, _session.Current.Bpm);
    }

    [RelayCommand]
    private async Task CancelTriggerAsync()
    {
        await _trigger.CancelAsync();
        TriggerStatus    = TriggerStatus.Idle;
        CountdownDisplay = "—";
        NextStateName    = "—";
    }

    [RelayCommand]
    private async Task EmergencySwitchAsync()
    {
        await _trigger.CancelAsync();
        if (_session.Current.NextState is { } next)
        {
            await _session.TransitionToStateAsync(next);
            await _playback.TransitionToStateAsync(next);
        }
        TriggerStatus    = TriggerStatus.Idle;
        CountdownDisplay = "—";
    }

    // ── event handlers ─────────────────────────────────────────────────────

    private void OnTriggerFired(Trigger trigger)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            var state = AvailableStates.FirstOrDefault(s => s.Id == trigger.TargetStateId);
            if (state is not null)
            {
                await _session.TransitionToStateAsync(state);
                await _playback.TransitionToStateAsync(state);
            }
            TriggerStatus    = TriggerStatus.Fired;
            CountdownDisplay = "Triggered";
        });
    }

    private void OnCountdownTick(TimeSpan remaining)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            TriggerStatus    = TriggerStatus.Counting;
            CountdownDisplay = remaining.TotalSeconds < 60
                ? $"{remaining.TotalSeconds:F0}s"
                : remaining.ToString(@"m\:ss");
        });
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private void SyncFromSession(Session s)
    {
        CurrentStateName = s.CurrentState?.Name ?? "—";
        OnPropertyChanged(nameof(AvailableStates));
    }

    private string FormatDelay() => DelayType switch
    {
        TriggerDelayType.Immediate => "Now",
        TriggerDelayType.Seconds   => $"{DelayValue}s",
        TriggerDelayType.Bars      => $"{DelayValue} bars",
        _                          => "—"
    };
}
