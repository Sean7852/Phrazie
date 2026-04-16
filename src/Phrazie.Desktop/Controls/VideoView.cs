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

    public MediaPlayer? MediaPlayer
    {
        get => _mediaPlayer;
        set
        {
            if (_mediaPlayer == value) return;

            if (_mediaPlayer is not null)
                _mediaPlayer.Hwnd = IntPtr.Zero;

            _mediaPlayer = value;

            if (_mediaPlayer is not null && _handle is not null)
                _mediaPlayer.Hwnd = _handle.Handle;
        }
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        _handle = base.CreateNativeControlCore(parent);

        if (_mediaPlayer is not null)
            _mediaPlayer.Hwnd = _handle.Handle;

        return _handle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (_mediaPlayer is not null)
            _mediaPlayer.Hwnd = IntPtr.Zero;

        _handle = null;
        base.DestroyNativeControlCore(control);
    }
}
