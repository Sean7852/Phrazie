using LibVLCSharp.Shared;
using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.Services;

/// <summary>
/// Real video playback using LibVLC. Registered as singleton so the MediaPlayer
/// instance stays alive and the VideoView can hold a reference to it.
/// </summary>
public sealed class VideoPlaybackService : IPlaybackService, IDisposable
{
    private readonly LibVLC _libVlc;
    private Media?          _currentMedia;
    private int             _sequentialIndex;

    public MediaPlayer MediaPlayer { get; }

    public bool  IsPlaying   => MediaPlayer.IsPlaying;
    public Clip? CurrentClip { get; private set; }

    public event Action<Clip?>? ClipChanged;
    public event Action?        ClipEnded;

    public VideoPlaybackService()
    {
        LibVLCSharp.Shared.Core.Initialize();

        // Disable VLC's own keyboard/mouse handling to prevent it from
        // self-triggering fullscreen mode (topmost HWND that blocks the app).
        _libVlc     = new LibVLC("--no-keyboard-events", "--no-mouse-events");
        MediaPlayer = new MediaPlayer(_libVlc);

        MediaPlayer.EndReached += (_, _) =>
            Task.Run(() => { Thread.Sleep(50); ClipEnded?.Invoke(); });
    }

    public Task PlayAsync(Clip clip)
    {
        _currentMedia?.Dispose();
        _currentMedia = new Media(_libVlc, clip.FilePath, FromType.FromPath);
        MediaPlayer.Play(_currentMedia);
        CurrentClip = clip;
        ClipChanged?.Invoke(clip);
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        MediaPlayer.SetPause(true);
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        if (_currentMedia is not null)
            MediaPlayer.SetPause(false);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        MediaPlayer.Stop();
        _currentMedia?.Dispose();
        _currentMedia = null;
        CurrentClip   = null;
        ClipChanged?.Invoke(null);
        return Task.CompletedTask;
    }

    public Task TransitionToStateAsync(State state)
    {
        if (state.Clips.Count == 0) return StopAsync();

        var clip = state.PlaybackMode switch
        {
            PlaybackMode.Random     => state.Clips[Random.Shared.Next(state.Clips.Count)],
            PlaybackMode.Sequential => state.Clips[_sequentialIndex++ % state.Clips.Count],
            _                       => state.Clips[0]
        };

        _sequentialIndex = 0;
        return PlayAsync(clip);
    }

    public void Dispose()
    {
        MediaPlayer.Dispose();
        _currentMedia?.Dispose();
        _libVlc.Dispose();
    }
}
