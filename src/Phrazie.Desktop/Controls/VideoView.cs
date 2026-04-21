using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Platform;
using LibVLCSharp.Shared;

namespace Phrazie.Desktop.Controls;

/// <summary>
/// NativeControlHost that passes its Win32 HWND directly to a LibVLC MediaPlayer.
/// Replaces LibVLCSharp.Avalonia which targets Avalonia 11 and crashes on Avalonia 12.
/// </summary>
public sealed class VideoView : NativeControlHost
{
    private MediaPlayer?     _mediaPlayer;
    private IPlatformHandle? _handle;

    /// <summary>Current native handle — returns Zero if the host hasn't been realised yet.</summary>
    public IntPtr NativeHandle
    {
        get
        {
            if (_handle is null)
            {
                Debug.WriteLine("[VideoView] NativeHandle → 0  (_handle is null)");
                return IntPtr.Zero;
            }
            return _handle.Handle;
        }
    }

    /// <summary>
    /// Fires on the UI thread each time the native window is (re-)created and has a valid HWND.
    /// Subscribe here rather than Loaded to guarantee the HWND exists before starting playback.
    /// </summary>
    public event Action<IntPtr>? HandleAvailable;

    public MediaPlayer? MediaPlayer
    {
        get => _mediaPlayer;
        set
        {
            if (_mediaPlayer == value) return;

            if (_mediaPlayer is not null)
            {
                _mediaPlayer.Opening    -= OnOpening;
                _mediaPlayer.EndReached -= OnEndReached;
                Debug.WriteLine($"[VideoView] Clearing Hwnd (was 0x{_mediaPlayer.Hwnd:X})");
                _mediaPlayer.Hwnd     = IntPtr.Zero;
            }

            _mediaPlayer = value;

            if (_mediaPlayer is not null && _handle is not null)
            {
                _mediaPlayer.Hwnd        = _handle.Handle;
                _mediaPlayer.Opening    += OnOpening;
                _mediaPlayer.EndReached += OnEndReached;
                Debug.WriteLine($"[VideoView] Set Hwnd = 0x{_mediaPlayer.Hwnd:X}  handle=0x{_handle.Handle:X}");
            }
            else
            {
                Debug.WriteLine($"[VideoView] MediaPlayer set but _handle is null — Hwnd NOT set");
            }
        }
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        _handle = base.CreateNativeControlCore(parent);
        Debug.WriteLine($"[VideoView] CreateNativeControlCore  handle=0x{_handle.Handle:X}");

        if (_mediaPlayer is not null)
        {
            _mediaPlayer.Hwnd        = _handle.Handle;
            _mediaPlayer.Opening    += OnOpening;
            _mediaPlayer.EndReached += OnEndReached;
            Debug.WriteLine($"[VideoView] (late) Set Hwnd = 0x{_mediaPlayer.Hwnd:X}");
        }

        Debug.WriteLine($"[VideoView] Firing HandleAvailable 0x{_handle.Handle:X}");
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => HandleAvailable?.Invoke(_handle?.Handle ?? IntPtr.Zero),
            Avalonia.Threading.DispatcherPriority.Loaded);

        return _handle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        var callers = new System.Diagnostics.StackTrace(1, false)
            .GetFrames()
            .Take(10)
            .Select(f => $"{f.GetMethod()?.DeclaringType?.Name}.{f.GetMethod()?.Name}");
        Debug.WriteLine($"[VideoView] DestroyNativeControlCore — {string.Join(" → ", callers)}");
        if (_mediaPlayer is not null)
        {
            _mediaPlayer.Opening    -= OnOpening;
            _mediaPlayer.EndReached -= OnEndReached;
            _mediaPlayer.Hwnd        = IntPtr.Zero;
        }

        _handle = null;
        base.DestroyNativeControlCore(control);
    }

    private void OnOpening(object? sender, EventArgs e)
    {
        var handle = _handle;
        var mp     = _mediaPlayer;
        Debug.WriteLine($"[VideoView] OnOpening — handle={(handle is null ? "NULL" : $"0x{handle.Handle:X}")}  current Hwnd=0x{mp?.Hwnd:X}");
        if (handle is not null && mp is not null)
        {
            mp.Hwnd = handle.Handle;
            Debug.WriteLine($"[VideoView] OnOpening — Hwnd set to 0x{mp.Hwnd:X}");
        }
    }

    // VLC zeroes Hwnd internally when EndReached fires — re-attach immediately so the
    // next Play() call finds a valid render target before it opens its own window.
    private void OnEndReached(object? sender, EventArgs e)
    {
        var handle = _handle;
        var mp     = _mediaPlayer;
        Debug.WriteLine($"[VideoView] OnEndReached — re-attaching handle={(handle is null ? "NULL" : $"0x{handle.Handle:X}")}");
        if (handle is not null && mp is not null)
        {
            mp.Hwnd = handle.Handle;
            Debug.WriteLine($"[VideoView] OnEndReached — Hwnd set to 0x{mp.Hwnd:X}");
        }
    }
}
