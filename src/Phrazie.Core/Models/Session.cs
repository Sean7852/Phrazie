namespace Phrazie.Core.Models;

public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Collection? ActiveCollection { get; set; }
    public State? CurrentState { get; set; }
    public State? NextState { get; set; }
    public Trigger? PendingTrigger { get; set; }
    public double Bpm { get; set; } = 128.0;
}
