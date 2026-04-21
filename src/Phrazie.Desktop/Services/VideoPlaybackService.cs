using System.Diagnostics;
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

    /// <summary>
    /// Wired by VideoView so PlayAsync can re-attach the HWND right before every Play() call,
    /// bypassing the unreliable VLC event-based HWND management.
    /// </summary>
    public Func<IntPtr>? HwndProvider { get; set; }

    /// <summary>Clip stored when PlayAsync was called before the HWND was ready.</summary>
    public Clip? DeferredClip { get; private set; }

    public bool  IsPlaying   => MediaPlayer.IsPlaying;
    public Clip? CurrentClip { get; private set; }

    public event Action<Clip?>? ClipChanged;
    public event Action?        ClipEnded;

    public VideoPlaybackService()
    {
        LibVLCSharp.Shared.Core.Initialize();

        _libVlc     = new LibVLC(
            "--no-keyboard-events",
            "--no-mouse-events",
            "--no-overlay",           // prevent overlay surfaces that ignore HWND on resolution change
            "--no-video-title-show"); // suppress title pop-up which can create a separate surface
        MediaPlayer = new MediaPlayer(_libVlc);

        MediaPlayer.EndReached += (_, _) =>
        {
            Debug.WriteLine($"[VPS] EndReached — Hwnd=0x{MediaPlayer.Hwnd:X}  thread={Thread.CurrentThread.ManagedThreadId}");
            Task.Run(() =>
            {
                Thread.Sleep(50);
                Debug.WriteLine($"[VPS] Firing ClipEnded — Hwnd=0x{MediaPlayer.Hwnd:X}");
                ClipEnded?.Invoke();
            });
        };

        MediaPlayer.Opening += (_, _) =>
            Debug.WriteLine($"[VPS] MediaPlayer.Opening — Hwnd=0x{MediaPlayer.Hwnd:X}  thread={Thread.CurrentThread.ManagedThreadId}");

        MediaPlayer.Playing += (_, _) =>
            Debug.WriteLine($"[VPS] MediaPlayer.Playing — Hwnd=0x{MediaPlayer.Hwnd:X}");
    }

    public Task PlayAsync(Clip clip)
    {
        var hwnd = HwndProvider?.Invoke() ?? IntPtr.Zero;
        Debug.WriteLine($"[VPS] PlayAsync '{clip.DisplayName}' — provider=0x{hwnd:X}  Hwnd=0x{MediaPlayer.Hwnd:X}  thread={Thread.CurrentThread.ManagedThreadId}");

        if (hwnd == IntPtr.Zero)
        {
            Debug.WriteLine($"[VPS] HWND not ready — deferring '{clip.DisplayName}'");
            DeferredClip = clip;
            return Task.CompletedTask;
        }

        DeferredClip = null;
        MediaPlayer.Hwnd = hwnd;
        _currentMedia?.Dispose();
        _currentMedia = new Media(_libVlc, clip.FilePath, FromType.FromPath);
        MediaPlayer.Play(_currentMedia);
        Debug.WriteLine($"[VPS] Play() called — Hwnd=0x{MediaPlayer.Hwnd:X}");
        CurrentClip = clip;
        ClipChanged?.Invoke(clip);
        return Task.CompletedTask;
    }

    /// <summary>Called by VideoView.HandleAvailable — plays the clip that was deferred due to missing HWND.</summary>
    public Task PlayDeferredAsync(IntPtr hwnd)
    {
        if (DeferredClip is null) return Task.CompletedTask;
        Debug.WriteLine($"[VPS] PlayDeferredAsync '{DeferredClip.DisplayName}' — Hwnd=0x{hwnd:X}");
        MediaPlayer.Hwnd = hwnd;
        var clip = DeferredClip;
        DeferredClip = null;
        return PlayAsync(clip);
    }

    public Task StopAsync()
    {
        Debug.WriteLine($"[VPS] StopAsync");
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
