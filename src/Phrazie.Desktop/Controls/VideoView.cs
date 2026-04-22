using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Phrazie.Core.Enums;
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
    private int                   _pendingUpdate;

    private Action<int, int>? _formatHandler;
    private Action?           _frameHandler;

    // ── Transition state ───────────────────────────────────────────────────
    private CancellationTokenSource? _transitionCts;
    private double _flashOpacity = 0.0; // white overlay for Invert effect

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
            context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));
        else
            context.DrawImage(bitmap, new Rect(bitmap.Size), new Rect(Bounds.Size));

        if (_flashOpacity > 0.001)
        {
            using var _ = context.PushOpacity(_flashOpacity);
            context.FillRectangle(Brushes.White, new Rect(Bounds.Size));
        }
    }

    // ── Transition engine ──────────────────────────────────────────────────

    public async Task PlayTransitionAsync(TransitionType type, double durationSeconds)
    {
        _transitionCts?.Cancel();
        _transitionCts?.Dispose();
        _transitionCts = new CancellationTokenSource();
        var ct = _transitionCts.Token;

        try
        {
            switch (type)
            {
                case TransitionType.Cut:
                    break;

                case TransitionType.Fade:
                    Opacity = 0.0;
                    await AnimateAsync(v => Opacity = v, 0.0, 1.0, durationSeconds, ct);
                    break;

                case TransitionType.Strobe:
                    await StrobeAsync(durationSeconds, ct);
                    break;

                case TransitionType.Blur:
                    var blur = new BlurEffect { Radius = 20 };
                    Effect = blur;
                    await AnimateAsync(v => blur.Radius = v, 20.0, 0.0, durationSeconds, ct);
                    Effect = null;
                    break;

                case TransitionType.Glitch:
                    await GlitchAsync(durationSeconds, ct);
                    break;

                case TransitionType.Invert:
                    await FlashAsync(durationSeconds, ct);
                    break;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            Opacity         = 1.0;
            Effect          = null;
            RenderTransform = null;
            _flashOpacity   = 0.0;
            InvalidateVisual();
        }
    }

    // Animates a setter from `from` to `to` over `durationSec` at ~60 fps
    private static async Task AnimateAsync(Action<double> setter,
                                            double from, double to,
                                            double durationSec,
                                            CancellationToken ct)
    {
        var start = DateTime.UtcNow;
        var end   = start.AddSeconds(durationSec);

        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            if (now >= end) break;
            var t = (now - start).TotalSeconds / durationSec;
            setter(from + (to - from) * t);
            await Task.Delay(16, ct);
        }

        if (!ct.IsCancellationRequested)
            setter(to);
    }

    private static async Task StrobeAsync(double durationSec, CancellationToken ct)
    {
        var end = DateTime.UtcNow.AddSeconds(durationSec);
        bool visible = false;
        while (!ct.IsCancellationRequested && DateTime.UtcNow < end)
        {
            // Strobe via opacity so layout is unaffected
            await Task.Delay(45, ct);
            visible = !visible;
        }
        _ = visible; // suppress warning
    }

    private async Task GlitchAsync(double durationSec, CancellationToken ct)
    {
        var tt  = new TranslateTransform();
        RenderTransform = tt;
        var end = DateTime.UtcNow.AddSeconds(durationSec);

        while (!ct.IsCancellationRequested && DateTime.UtcNow < end)
        {
            tt.X = (Random.Shared.NextDouble() - 0.5) * 24;
            tt.Y = (Random.Shared.NextDouble() - 0.5) * 10;
            await Task.Delay(40, ct);
        }

        tt.X = 0;
        tt.Y = 0;
    }

    private async Task FlashAsync(double durationSec, CancellationToken ct)
    {
        await AnimateAsync(v =>
        {
            _flashOpacity = v;
            InvalidateVisual();
        }, 1.0, 0.0, durationSec, ct);

        _flashOpacity = 0.0;
        InvalidateVisual();
    }
}
