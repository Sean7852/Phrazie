namespace Phrazie.Core.Models;

public class Clip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FilePath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool Loop { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
}
