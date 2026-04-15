using Phrazie.Core.Enums;

namespace Phrazie.Core.Models;

public class State
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public PlaybackMode PlaybackMode { get; set; } = PlaybackMode.Loop;
    public List<Clip> Clips { get; set; } = new();
}
