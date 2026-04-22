using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.Services;

/// <summary>
/// Software-callback rendering via LibVLC. VLC decodes every frame into a pinned buffer;
/// the Display callback copies it to a "ready" buffer that VideoView reads on the UI thread.
/// This keeps playback running regardless of which page is active.
/// </summary>
public sealed class VideoPlaybackService : IPlaybackService, IDisposable
{
    private readonly LibVLC _libVlc;
    private Media?          _currentMedia;
    private int             _sequentialIndex;

    // ── Frame buffers ──────────────────────────────────────────────────────
    private byte[]   _decodeBuffer = Array.Empty<byte>(); // VLC writes here (pinned)
    private GCHandle _decodeHandle;
    private byte[]   _readyBuffer  = Array.Empty<byte>(); // latest complete frame (UI reads)
    private readonly object _bufferLock = new();
    private volatile int _videoWidth;
    private volatile int _videoHeight;

    // Delegate fields — must be kept alive to prevent GC collection
    private readonly MediaPlayer.LibVLCVideoFormatCb  _formatCb;
    private readonly MediaPlayer.LibVLCVideoCleanupCb _cleanupCb;
    private readonly MediaPlayer.LibVLCVideoLockCb    _lockCb;
    private readonly MediaPlayer.LibVLCVideoDisplayCb _displayCb;

    // ── Public surface ─────────────────────────────────────────────────────
    public MediaPlayer MediaPlayer { get; }

    public bool  IsPlaying   => MediaPlayer.IsPlaying;
    public Clip? CurrentClip { get; private set; }

    public int FrameWidth  => _videoWidth;
    public int FrameHeight => _videoHeight;

    public event Action<Clip?>?    ClipChanged;
    public event Action?           ClipEnded;
    /// <summary>Fires on a VLC thread whenever a new frame is ready to display.</summary>
    public event Action?           FrameReady;
    /// <summary>Fires when the video dimensions change (new clip with different resolution).</summary>
    public event Action<int, int>? VideoFormatChanged;

    public VideoPlaybackService()
    {
        LibVLCSharp.Shared.Core.Initialize();
        _libVlc = new LibVLC("--no-keyboard-events", "--no-mouse-events");
        MediaPlayer = new MediaPlayer(_libVlc);

        // Store as fields so the GC never collects them while VLC holds native pointers
        _formatCb  = FormatCallback;
        _cleanupCb = CleanupCallback;
        _lockCb    = LockCallback;
        _displayCb = DisplayCallback;

        MediaPlayer.SetVideoFormatCallbacks(_formatCb, _cleanupCb);
        MediaPlayer.SetVideoCallbacks(_lockCb, null, _displayCb);

        MediaPlayer.EndReached += (_, _) =>
            Task.Run(() =>
            {
                Thread.Sleep(50);
                ClipEnded?.Invoke();
            });
    }

    // ── VLC callbacks ──────────────────────────────────────────────────────

    private uint FormatCallback(ref IntPtr opaque, IntPtr chroma,
                                ref uint width, ref uint height,
                                ref uint pitches, ref uint lines)
    {
        int w = (int)width;
        int h = (int)height;

        lock (_bufferLock)
            AllocateBuffers(w, h);

        _videoWidth  = w;
        _videoHeight = h;

        // Tell VLC we want raw BGRA pixels (matches Avalonia's Bgra8888)
        Marshal.Copy(new byte[] { (byte)'B', (byte)'G', (byte)'R', (byte)'A' }, 0, chroma, 4);
        pitches = (uint)(w * 4); // bytes per row
        lines   = (uint)h;       // rows per plane

        VideoFormatChanged?.Invoke(w, h);
        return 1; // one buffer plane
    }

    private void CleanupCallback(ref IntPtr opaque) { /* buffers are freed on next FormatCallback */ }

    private IntPtr LockCallback(IntPtr opaque, IntPtr planes)
    {
        // Hand VLC the pointer to our pinned buffer for writing
        Marshal.WriteIntPtr(planes,
            _decodeHandle.IsAllocated ? _decodeHandle.AddrOfPinnedObject() : IntPtr.Zero);
        return IntPtr.Zero;
    }

    private void DisplayCallback(IntPtr opaque, IntPtr picture)
    {
        lock (_bufferLock)
        {
            if (_decodeBuffer.Length > 0 && _readyBuffer.Length == _decodeBuffer.Length)
                Buffer.BlockCopy(_decodeBuffer, 0, _readyBuffer, 0, _decodeBuffer.Length);
        }
        FrameReady?.Invoke();
    }

    // ── Buffer helpers ─────────────────────────────────────────────────────

    private void AllocateBuffers(int w, int h)
    {
        if (_decodeHandle.IsAllocated) _decodeHandle.Free();
        int size      = w * h * 4;
        _decodeBuffer = new byte[size];
        _decodeHandle = GCHandle.Alloc(_decodeBuffer, GCHandleType.Pinned);
        _readyBuffer  = new byte[size];
    }

    /// <summary>
    /// Copies the latest decoded frame into <paramref name="dest"/>.
    /// <paramref name="stride"/> is the destination row stride in bytes.
    /// Returns false if no frame has been decoded yet.
    /// </summary>
    public bool TryCopyFrame(IntPtr dest, int stride)
    {
        lock (_bufferLock)
        {
            if (_readyBuffer.Length == 0) return false;
            int srcStride = _videoWidth * 4;
            if (stride == srcStride)
            {
                Marshal.Copy(_readyBuffer, 0, dest, _readyBuffer.Length);
            }
            else
            {
                for (int y = 0; y < _videoHeight; y++)
                    Marshal.Copy(_readyBuffer, y * srcStride, dest + y * stride, srcStride);
            }
            return true;
        }
    }

    // ── IPlaybackService ───────────────────────────────────────────────────

    public Task PlayAsync(Clip clip)
    {
        _currentMedia?.Dispose();
        _currentMedia = new Media(_libVlc, clip.FilePath, FromType.FromPath);
        MediaPlayer.Play(_currentMedia);
        CurrentClip = clip;
        ClipChanged?.Invoke(clip);
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
        if (_decodeHandle.IsAllocated) _decodeHandle.Free();
        _libVlc.Dispose();
    }
}
