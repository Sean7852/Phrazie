using Phrazie.Core.Models;

namespace Phrazie.Core.Interfaces;

public interface ITriggerService
{
    Trigger? ActiveTrigger { get; }

    /// <summary>Raised when a trigger fires and the state should transition.</summary>
    event Action<Trigger>? TriggerFired;

    /// <summary>Raised every second while a trigger is counting down.</summary>
    event Action<TimeSpan>? CountdownTick;

    Task ScheduleAsync(Trigger trigger, double bpm);
    Task CancelAsync();
}
