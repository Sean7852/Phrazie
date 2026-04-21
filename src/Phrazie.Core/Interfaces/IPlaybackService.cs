using Phrazie.Core.Models;

namespace Phrazie.Core.Interfaces;

public interface IPlaybackService
{
    bool IsPlaying { get; }
    Clip? CurrentClip { get; }

    event Action<Clip?>? ClipChanged;
    event Action?        ClipEnded;

    Task PlayAsync(Clip clip);
    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();
    Task TransitionToStateAsync(State state);
}
