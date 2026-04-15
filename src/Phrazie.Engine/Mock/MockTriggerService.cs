using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Engine.Mock;

/// <summary>
/// Simulates trigger scheduling with a System.Threading.Timer.
/// BPM-to-milliseconds conversion is real; swap in a precision timer in Phase 3.
/// </summary>
public sealed class MockTriggerService : ITriggerService, IDisposable
{
    private CancellationTokenSource? _cts;

    public Trigger? ActiveTrigger { get; private set; }

    public event Action<Trigger>? TriggerFired;
    public event Action<TimeSpan>? CountdownTick;

    public async Task ScheduleAsync(Trigger trigger, double bpm)
    {
        await CancelAsync();

        trigger.Status = TriggerStatus.Scheduled;
        trigger.ScheduledAt = DateTimeOffset.UtcNow;
        ActiveTrigger = trigger;

        var delayMs = CalculateDelayMs(trigger, bpm);

        _cts = new CancellationTokenSource();
        _ = RunCountdownAsync(trigger, delayMs, _cts.Token);
    }

    public Task CancelAsync()
    {
        _cts?.Cancel();
        if (ActiveTrigger is not null)
        {
            ActiveTrigger.Status = TriggerStatus.Cancelled;
            ActiveTrigger = null;
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private static double CalculateDelayMs(Trigger trigger, double bpm) =>
        trigger.DelayType switch
        {
            TriggerDelayType.Immediate => 0,
            TriggerDelayType.Seconds   => trigger.DelayValue * 1000,
            TriggerDelayType.Bars      => trigger.DelayValue * (4 * 60_000.0 / bpm),
            _                          => 0
        };

    private async Task RunCountdownAsync(Trigger trigger, double totalMs, CancellationToken ct)
    {
        trigger.Status = TriggerStatus.Counting;
        var remaining = TimeSpan.FromMilliseconds(totalMs);
        var interval  = TimeSpan.FromSeconds(1);

        while (remaining > TimeSpan.Zero && !ct.IsCancellationRequested)
        {
            CountdownTick?.Invoke(remaining);
            var wait = remaining < interval ? remaining : interval;
            await Task.Delay(wait, ct).ConfigureAwait(false);
            remaining -= wait;
        }

        if (!ct.IsCancellationRequested)
        {
            trigger.Status = TriggerStatus.Fired;
            TriggerFired?.Invoke(trigger);
            ActiveTrigger = null;
        }
    }
}
