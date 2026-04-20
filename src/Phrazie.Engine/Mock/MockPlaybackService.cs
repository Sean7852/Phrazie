using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Engine.Mock;

/// <summary>
/// No-op playback service. Replace with a real video player (LibVLCSharp, etc.) in Phase 3.
/// </summary>
public sealed class MockPlaybackService : IPlaybackService
{
    public bool IsPlaying { get; private set; }
    public Clip? CurrentClip { get; private set; }

    public event Action<Clip?>? ClipChanged;
    public event Action?        ClipEnded;

    public Task PlayAsync(Clip clip)
    {
        CurrentClip = clip;
        IsPlaying = true;
        ClipChanged?.Invoke(clip);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        CurrentClip = null;
        IsPlaying = false;
        ClipChanged?.Invoke(null);
        return Task.CompletedTask;
    }

    public Task TransitionToStateAsync(State state)
    {
        // In a real implementation, pick the next clip from the state according to PlaybackMode.
        var clip = state.Clips.FirstOrDefault();
        return clip is not null ? PlayAsync(clip) : StopAsync();
    }
}
