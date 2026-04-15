using Phrazie.Core.Enums;

namespace Phrazie.Core.Models;

public class Trigger
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TargetStateId { get; set; }
    public TriggerDelayType DelayType { get; set; } = TriggerDelayType.Immediate;
    public double DelayValue { get; set; } = 0;
    public TriggerStatus Status { get; set; } = TriggerStatus.Idle;
    public DateTimeOffset? ScheduledAt { get; set; }
}
