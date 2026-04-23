using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.Services;

/// <summary>
/// Software-callback rendering via LibVLC with gapless double buffering.
/// Two PlayerSlot instances (A/B) allow pre-rolling the same clip at 95%
/// completion so the loop swap is seamless with no VLC stop/start cycle.
/// </summary>
public sealed class VideoPlaybackService : IPlaybackService, IDisposable
{
    private readonly LibVLC _libVlc;
    private int             _sequentialIndex;

    // ── Double-buffer slots ────────────────────────────────────────────────
    private readonly PlayerSlot[] _slots;
    private volatile int          _activeSlot;  // 0 or 1
    private volatile bool         _loopMode;
    private volatile bool         _near95Fired;
    private volatile int          _videoWidth;
    private volatile int          _videoHeight;

    // ── Public surface ─────────────────────────────────────────────────────
    public MediaPlayer MediaPlayer => _slots[_activeSlot].Player;

    public bool  IsPlaying   => _slots[_activeSlot].Player.IsPlaying;
    public Clip? CurrentClip { get; private set; }

    public int FrameWidth  => _videoWidth;
    public int FrameHeight => _videoHeight;

    public event Action<Clip?>?    ClipChanged;
    public event Action?           ClipEnded;
    /// <summary>Fires on a VLC thread whenever a new frame is ready to display.</summary>
    public event Action?           FrameReady;
    /// <summary>Fires when the video dimensions change (new clip with different resolution).</summary>
    public event Action<int, int>? VideoFormatChanged;
    /// <summary>Fires on a background thread when remaining playback time drops below NearEndLookahead.</summary>
    public event Action?           ClipNearEnd;

    /// <summary>How far before clip end to fire ClipNearEnd. Zero disables the event.</summary>
    public TimeSpan NearEndLookahead { get; set; } = TimeSpan.Zero;
    private volatile bool _nearEndFired;

    public VideoPlaybackService()
    {
        LibVLCSharp.Shared.Core.Initialize();
        _libVlc = new LibVLC(
            "--no-keyboard-events", "--no-mouse-events",
            "--file-caching=150",   "--network-caching=150");

        _slots = [new PlayerSlot(_libVlc, 0, this), new PlayerSlot(_libVlc, 1, this)];
    }

    // ── Callbacks from PlayerSlot ──────────────────────────────────────────

    internal void OnSlotFormatChanged(int slotIndex, int w, int h)
    {
        if (slotIndex != _activeSlot) return;
        _videoWidth  = w;
        _videoHeight = h;
        VideoFormatChanged?.Invoke(w, h);
    }

    internal void OnSlotFrameReady(int slotIndex)
    {
        if (slotIndex == _activeSlot)
            FrameReady?.Invoke();
    }

    internal void OnSlotEndReached(int slotIndex)
    {
        if (slotIndex != _activeSlot) return;

        if (_loopMode)
        {
            // Gapless loop: swap to the pre-rolled standby slot
            PerformSwap();
            _near95Fired = false; // re-arm pre-roll for next loop
        }
        else
        {
            Task.Run(() =>
            {
                Thread.Sleep(50);
                ClipEnded?.Invoke();
            });
        }
    }

    internal void OnSlotTimeChanged(int slotIndex, long currentMs, long length)
    {
        if (slotIndex != _activeSlot) return;

        // ViewModel near-end event (used for transition pre-fire)
        var lookahead = NearEndLookahead;
        if (lookahead != TimeSpan.Zero && !_nearEndFired && length > 0)
        {
            var remainingMs = length - currentMs;
            if (currentMs > 500 && remainingMs <= lookahead.TotalMilliseconds)
            {
                _nearEndFired = true;
                Task.Run(() => ClipNearEnd?.Invoke());
            }
        }

        // Internal 95% pre-roll trigger for gapless looping
        if (_loopMode && !_near95Fired && length > 0)
        {
            // Use at least 500 ms lookahead so very short clips still get buffered
            var threshold = Math.Max(length * 0.05, 500.0);
            if (currentMs > 200 && (length - currentMs) <= threshold)
            {
                _near95Fired = true;
                var clip = CurrentClip;
                if (clip is not null)
                {
                    var thread = new Thread(() => _slots[1 - _activeSlot].Play(clip))
                    {
                        IsBackground = true,
                        Priority     = ThreadPriority.Highest,
                        Name         = "VJStudio-Preroll"
                    };
                    thread.Start();
                }
            }
        }
    }

    // ── Swap ───────────────────────────────────────────────────────────────

    private void PerformSwap()
    {
        var incomingIdx = 1 - _activeSlot;
        var outgoingIdx = _activeSlot;

        var inSlot = _slots[incomingIdx];

        // Notify consumers if the resolution changed (e.g. different encode)
        if (inSlot.Width > 0 && inSlot.Height > 0 &&
            (inSlot.Width != _videoWidth || inSlot.Height != _videoHeight))
        {
            _videoWidth  = inSlot.Width;
            _videoHeight = inSlot.Height;
            VideoFormatChanged?.Invoke(inSlot.Width, inSlot.Height);
        }

        // Atomic flip — from this point DisplayCallbacks from the new slot drive FrameReady
        Interlocked.Exchange(ref _activeSlot, incomingIdx);

        // Stop the outgoing player on a high-priority background thread
        var outSlot = _slots[outgoingIdx];
        var thread  = new Thread(() => outSlot.Stop())
        {
            IsBackground = true,
            Priority     = ThreadPriority.Highest,
            Name         = "VJStudio-SlotStop"
        };
        thread.Start();
    }

    // ── IPlaybackService ───────────────────────────────────────────────────

    Task IPlaybackService.PlayAsync(Clip clip) => PlayAsync(clip, false);

    public Task PlayAsync(Clip clip, bool loop = false)
    {
        _nearEndFired = false;
        _near95Fired  = false;
        _loopMode     = loop;

        // Stop any standby pre-roll from a previous cycle
        _slots[1 - _activeSlot].Stop();

        var active = _slots[_activeSlot];
        active.ClearReadyBuffer();
        active.Play(clip);

        CurrentClip = clip;
        ClipChanged?.Invoke(clip);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _loopMode = false;
        foreach (var slot in _slots)
            slot.Stop();
        CurrentClip = null;
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

    // ── Frame access ───────────────────────────────────────────────────────

    /// <summary>
    /// Copies the latest decoded frame into <paramref name="dest"/>.
    /// Returns false if no frame has been decoded yet.
    /// </summary>
    public bool TryCopyFrame(IntPtr dest, int stride)
        => _slots[_activeSlot].TryCopyFrame(dest, stride, _videoWidth, _videoHeight);

    public void Dispose()
    {
        foreach (var slot in _slots)
            slot.Dispose();
        _libVlc.Dispose();
    }

    // ── PlayerSlot inner class ─────────────────────────────────────────────

    private sealed class PlayerSlot : IDisposable
    {
        private readonly VideoPlaybackService _owner;
        private readonly int                  _index;
        private readonly LibVLC               _libVlc;
        private Media?                        _media;

        private byte[]   _decodeBuffer = Array.Empty<byte>();
        private GCHandle _decodeHandle;
        private byte[]   _readyBuffer  = Array.Empty<byte>();
        private readonly object _bufferLock = new();

        // Held as fields to prevent GC from collecting delegates while VLC holds native pointers
        private readonly MediaPlayer.LibVLCVideoFormatCb  _formatCb;
        private readonly MediaPlayer.LibVLCVideoCleanupCb _cleanupCb;
        private readonly MediaPlayer.LibVLCVideoLockCb    _lockCb;
        private readonly MediaPlayer.LibVLCVideoDisplayCb _displayCb;

        public MediaPlayer Player { get; }
        public int Width  { get; private set; }
        public int Height { get; private set; }

        public PlayerSlot(LibVLC libVlc, int index, VideoPlaybackService owner)
        {
            _owner  = owner;
            _index  = index;
            _libVlc = libVlc;
            Player  = new MediaPlayer(libVlc);

            _formatCb  = FormatCallback;
            _cleanupCb = CleanupCallback;
            _lockCb    = LockCallback;
            _displayCb = DisplayCallback;

            Player.SetVideoFormatCallbacks(_formatCb, _cleanupCb);
            Player.SetVideoCallbacks(_lockCb, null, _displayCb);

            Player.EndReached  += (_, _) => _owner.OnSlotEndReached(_index);
            Player.TimeChanged += (_, e) => _owner.OnSlotTimeChanged(_index, e.Time, Player.Length);
        }

        // ── VLC callbacks ──────────────────────────────────────────────────

        private uint FormatCallback(ref IntPtr opaque, IntPtr chroma,
                                    ref uint width, ref uint height,
                                    ref uint pitches, ref uint lines)
        {
            int w = (int)width;
            int h = (int)height;

            lock (_bufferLock)
                AllocateBuffers(w, h);

            Width  = w;
            Height = h;

            _owner.OnSlotFormatChanged(_index, w, h);

            Marshal.Copy(new byte[] { (byte)'B', (byte)'G', (byte)'R', (byte)'A' }, 0, chroma, 4);
            pitches = (uint)(w * 4);
            lines   = (uint)h;
            return 1;
        }

        private void CleanupCallback(ref IntPtr opaque) { }

        private IntPtr LockCallback(IntPtr opaque, IntPtr planes)
        {
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
            _owner.OnSlotFrameReady(_index);
        }

        // ── Buffer helpers ─────────────────────────────────────────────────

        private void AllocateBuffers(int w, int h)
        {
            if (_decodeHandle.IsAllocated) _decodeHandle.Free();
            int size      = w * h * 4;
            _decodeBuffer = new byte[size];
            _decodeHandle = GCHandle.Alloc(_decodeBuffer, GCHandleType.Pinned);
            _readyBuffer  = new byte[size];
        }

        public void ClearReadyBuffer()
        {
            lock (_bufferLock)
            {
                if (_readyBuffer.Length > 0)
                    Array.Clear(_readyBuffer);
            }
        }

        public bool TryCopyFrame(IntPtr dest, int stride, int w, int h)
        {
            lock (_bufferLock)
            {
                if (_readyBuffer.Length == 0) return false;
                int srcStride = w * 4;
                if (stride == srcStride)
                {
                    Marshal.Copy(_readyBuffer, 0, dest, _readyBuffer.Length);
                }
                else
                {
                    for (int y = 0; y < h; y++)
                        Marshal.Copy(_readyBuffer, y * srcStride, dest + y * stride, srcStride);
                }
                return true;
            }
        }

        // ── Playback control ───────────────────────────────────────────────

        public void Play(Clip clip)
        {
            try
            {
                _media?.Dispose();
                _media = new Media(_libVlc, clip.FilePath, FromType.FromPath);
                Player.Play(_media);
            }
            catch
            {
                // VLC can throw if the player is mid-Stop on another thread; swallow so the
                // pre-roll failure degrades gracefully rather than crashing the app.
            }
        }

        public void Stop()
        {
            try { Player.Stop(); } catch { }
            _media?.Dispose();
            _media = null;
        }

        public void Dispose()
        {
            Player.Dispose();
            _media?.Dispose();
            if (_decodeHandle.IsAllocated) _decodeHandle.Free();
        }
    }
}
