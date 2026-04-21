using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Phrazie.Desktop.Controls;

/// <summary>
/// High-performance phrase-position indicator.
/// Single Render() pass — no ItemsControl, no per-dot layout elements.
/// </summary>
public sealed class PhraseTracker : Control
{
    public static readonly StyledProperty<double> PhaseProperty =
        AvaloniaProperty.Register<PhraseTracker, double>(nameof(Phase), 0.0);

    public static readonly StyledProperty<int> DotCountProperty =
        AvaloniaProperty.Register<PhraseTracker, int>(nameof(DotCount), 16);

    public double Phase    { get => GetValue(PhaseProperty);    set => SetValue(PhaseProperty,    value); }
    public int    DotCount { get => GetValue(DotCountProperty); set => SetValue(DotCountProperty, value); }

    static PhraseTracker()
    {
        AffectsRender<PhraseTracker>(PhaseProperty, DotCountProperty);
    }

    private static readonly IBrush NormalInactiveBrush = new SolidColorBrush(Color.Parse("#252535"));
    private static readonly IBrush KickInactiveBrush   = new SolidColorBrush(Color.Parse("#3E3E54"));
    private static readonly IBrush ActiveBrush         = new SolidColorBrush(Color.Parse("#00FF00"));

    private static readonly IBrush GlowOuter = new SolidColorBrush(Color.FromArgb(20,  0, 255, 0));
    private static readonly IBrush GlowMid   = new SolidColorBrush(Color.FromArgb(55,  0, 255, 0));
    private static readonly IBrush GlowInner = new SolidColorBrush(Color.FromArgb(110, 0, 255, 0));

    private static readonly IBrush KickGlowOuter = new SolidColorBrush(Color.FromArgb(35,  0, 255, 0));
    private static readonly IBrush KickGlowMid   = new SolidColorBrush(Color.FromArgb(90,  0, 255, 0));
    private static readonly IBrush KickGlowInner = new SolidColorBrush(Color.FromArgb(170, 0, 255, 0));

    public override void Render(DrawingContext ctx)
    {
        var count = Math.Max(1, DotCount);
        var phase = Math.Clamp(Phase, 0.0, 1.0);
        var w     = Bounds.Width;
        var cy    = Bounds.Height / 2.0;
        var slotW = w / count;

        var activeIdx = (int)(phase * count) % count;
        var slotPhase = (phase * count) - activeIdx;
        var pulseT    = Math.Max(0.0, 1.0 - slotPhase / 0.30);

        for (var i = 0; i < count; i++)
        {
            var cx       = slotW * i + slotW / 2.0;
            var isKick   = (i % 4) == 0;
            var isActive = i == activeIdx;

            if (isActive)
            {
                double baseR    = isKick ? 5.5 : 4.5;
                double pulseAmp = isKick ? 3.5 : 1.8;
                var    r        = baseR + pulseT * pulseAmp;

                var (go, gm, gi) = isKick
                    ? (KickGlowOuter, KickGlowMid, KickGlowInner)
                    : (GlowOuter, GlowMid, GlowInner);

                var pt = new Point(cx, cy);
                ctx.DrawEllipse(go, null, pt, r * 2.8, r * 2.8);
                ctx.DrawEllipse(gm, null, pt, r * 1.7, r * 1.7);
                ctx.DrawEllipse(gi, null, pt, r * 1.2, r * 1.2);
                ctx.DrawEllipse(ActiveBrush, null, pt, r, r);
            }
            else
            {
                var r = isKick ? 3.5 * 1.2 : 2.5;
                ctx.DrawEllipse(
                    isKick ? KickInactiveBrush : NormalInactiveBrush,
                    null,
                    new Point(cx, cy),
                    r, r);
            }
        }
    }
}
