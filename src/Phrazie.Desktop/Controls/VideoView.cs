using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Phrazie.Desktop.Services;

namespace Phrazie.Desktop.Controls;

/// <summary>
/// Software-rendered video output control. Subscribes to VideoPlaybackService frame callbacks
/// and blits decoded BGRA frames into an Avalonia WriteableBitmap on each display tick.
/// Works regardless of which page is currently active.
/// </summary>
public sealed class VideoView : Control
{
    private VideoPlaybackService? _vps;
    private WriteableBitmap?      _bitmap;
    private int                   _pendingUpdate; // Interlocked flag — 0 = idle, 1 = queued

    private Action<int, int>? _formatHandler;
    private Action?           _frameHandler;

    public void Attach(VideoPlaybackService? vps)
    {
        if (_vps is not null)
        {
            _vps.VideoFormatChanged -= _formatHandler;
            _vps.FrameReady         -= _frameHandler;
            _formatHandler = null;
            _frameHandler  = null;
        }

        _vps = vps;

        if (_vps is null) return;

        _formatHandler = (w, h) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(
                () => EnsureBitmap(w, h),
                Avalonia.Threading.DispatcherPriority.Loaded);

        _frameHandler = () =>
        {
            if (System.Threading.Interlocked.CompareExchange(ref _pendingUpdate, 1, 0) == 0)
                Avalonia.Threading.Dispatcher.UIThread.Post(
                    UpdateFrame,
                    Avalonia.Threading.DispatcherPriority.Render);
        };

        _vps.VideoFormatChanged += _formatHandler;
        _vps.FrameReady         += _frameHandler;

        // If the service already has a known size, create the bitmap immediately.
        int w0 = _vps.FrameWidth, h0 = _vps.FrameHeight;
        if (w0 > 0 && h0 > 0)
            EnsureBitmap(w0, h0);
    }

    private void EnsureBitmap(int w, int h)
    {
        if (_bitmap is not null && _bitmap.PixelSize.Width == w && _bitmap.PixelSize.Height == h)
            return;
        _bitmap = new WriteableBitmap(
            new PixelSize(w, h),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);
    }

    private void UpdateFrame()
    {
        System.Threading.Interlocked.Exchange(ref _pendingUpdate, 0);

        var vps    = _vps;
        var bitmap = _bitmap;
        if (vps is null || bitmap is null) return;

        using var fb = bitmap.Lock();
        vps.TryCopyFrame(fb.Address, fb.RowBytes);

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var bitmap = _bitmap;
        if (bitmap is null)
        {
            context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));
            return;
        }
        context.DrawImage(bitmap, new Rect(bitmap.Size), new Rect(Bounds.Size));
    }
}
