using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Phrazie.Desktop.Controls;

/// <summary>
/// Beat-grid visualiser: 4 bars × 4 beats, with header showing phrase and beat position.
/// Single Render() pass — no layout elements, no allocation per frame.
/// </summary>
public sealed class BeatGrid : Control
{
    // ── Styled properties ─────────────────────────────────────────────────────

    public static readonly StyledProperty<double> PhaseProperty =
        AvaloniaProperty.Register<BeatGrid, double>(nameof(Phase), 0.0);

    public static readonly StyledProperty<int> PhraseNumberProperty =
        AvaloniaProperty.Register<BeatGrid, int>(nameof(PhraseNumber), 1);

    public static readonly StyledProperty<int> TotalPhrasesProperty =
        AvaloniaProperty.Register<BeatGrid, int>(nameof(TotalPhrases), 8);

    public static readonly StyledProperty<bool> AnimateProperty =
        AvaloniaProperty.Register<BeatGrid, bool>(nameof(Animate), true);

    public double Phase        { get => GetValue(PhaseProperty);        set => SetValue(PhaseProperty,        value); }
    public int    PhraseNumber { get => GetValue(PhraseNumberProperty); set => SetValue(PhraseNumberProperty, value); }
    public int    TotalPhrases { get => GetValue(TotalPhrasesProperty); set => SetValue(TotalPhrasesProperty, value); }
    public bool   Animate      { get => GetValue(AnimateProperty);      set => SetValue(AnimateProperty,      value); }

    static BeatGrid()
    {
        AffectsRender<BeatGrid>(PhaseProperty, PhraseNumberProperty, TotalPhrasesProperty, AnimateProperty);
    }

    // ── Constants ─────────────────────────────────────────────────────────────

    private const int    Bars        = 4;
    private const int    BeatsPerBar = 4;
    private const int    TotalBeats  = 16;
    private const double HeaderH     = 22.0;
    private const double BarPad      = 8.0;   // horizontal padding inside each bar
    private const double BarGap      = 8.0;   // gap between bars
    private const double SidePad     = 4.0;
    private const double CellGap     = 5.0;
    private const double BarLabelH   = 18.0;
    private const double CellRadius  = 5.0;

    // ── Pre-allocated brushes ─────────────────────────────────────────────────

    private static readonly Typeface Mono = new("Consolas,monospace", FontStyle.Normal, FontWeight.Medium);

    private static readonly IBrush LabelMuted  = new SolidColorBrush(Color.Parse("#8E8E93"));
    private static readonly IBrush LabelAccent = new SolidColorBrush(Color.Parse("#FFB800"));

    private static readonly IBrush PastBrush   = new SolidColorBrush(Color.Parse("#C8C8CC"));
    private static readonly IBrush FutureBrush = new SolidColorBrush(Color.Parse("#252528"));
    private static readonly IBrush CurrentBrush = new SolidColorBrush(Color.Parse("#FFB800"));

    private static readonly IBrush BarActiveBg   = new SolidColorBrush(Color.FromArgb(18, 255, 184, 0));
    private static readonly IBrush GlowMid       = new SolidColorBrush(Color.FromArgb(80, 255, 184, 0));
    private static readonly Pen    ActiveBarPen  = new(new SolidColorBrush(Color.Parse("#FFB800")), 1.5);

    // ── Render ────────────────────────────────────────────────────────────────

    public override void Render(DrawingContext ctx)
    {
        var phase  = Math.Clamp(Phase, 0.0, 1.0);
        var w      = Bounds.Width;
        var h      = Bounds.Height;

        var beatF         = phase * TotalBeats;
        var beatIdx       = (int)beatF % TotalBeats;
        var beatPhase     = beatF - Math.Floor(beatF);          // 0→1 within current slot
        var currentBar    = beatIdx / BeatsPerBar;
        var prevBeatIdx   = (beatIdx - 1 + TotalBeats) % TotalBeats;
        var pulseT        = Math.Max(0.0, 1.0 - beatPhase / 0.175);  // sharp decay at beat start

        // ── Header ─────────────────────────────────────────────────────────

        DrawHeader(ctx, beatIdx);

        // ── Bar sections ───────────────────────────────────────────────────

        var barAreaTop = HeaderH + 4.0;
        var barAreaH   = h - barAreaTop - 2.0;
        var barW       = (w - SidePad * 2 - BarGap * (Bars - 1)) / Bars;
        var cellH      = barAreaH - BarLabelH - 4.0;
        var cellW      = (barW - BarPad * 2 - CellGap * (BeatsPerBar - 1)) / BeatsPerBar;

        for (int b = 0; b < Bars; b++)
        {
            var barX    = SidePad + b * (barW + BarGap);
            bool active = b == currentBar;

            // Bar frame is always the same fixed size
            var barRect = new Rect(barX, barAreaTop, barW, barAreaH);

            // Active bar: subtle bg tint + accent border
            if (active)
            {
                ctx.DrawRectangle(BarActiveBg, null, new RoundedRect(barRect, 6));
                ctx.DrawRectangle(null, ActiveBarPen, new RoundedRect(barRect.Inflate(-0.75), 5.25));
            }

            // Bar label
            var label   = $"BAR {b + 1}";
            var ftLabel = new FormattedText(label,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, Mono, 9.5,
                active ? LabelAccent : LabelMuted);
            ctx.DrawText(ftLabel, new Point(barX + BarPad, barAreaTop + 4.0));

            const double BottomPad = 6.0;
            var cellBottom = barAreaTop + barAreaH - BottomPad;
            var cellLeft   = barX + BarPad;

            // Height tiers — all cells reach the same activeH when on the current beat
            var kickH   = cellH * 0.63;
            var normalH = cellH * 0.42;
            var activeH = cellH * 0.87;

            for (int c = 0; c < BeatsPerBar; c++)
            {
                var gbeat    = b * BeatsPerBar + c;
                bool current = gbeat == beatIdx;

                var baseH     = c == 0 ? kickH : normalH;
                bool isPrev   = gbeat == prevBeatIdx;
                // Current cell holds full height; previous cell shrinks as the new beat starts
                var thisCellH = current ? activeH
                              : isPrev && Animate ? baseH + (activeH - baseH) * pulseT
                              : baseH;
                var cellX     = cellLeft + c * (cellW + CellGap);
                var cellY     = cellBottom - thisCellH;
                var cellRect  = new Rect(cellX, cellY, cellW, thisCellH);
                var rounded   = new RoundedRect(cellRect, CellRadius);

                if (gbeat < beatIdx)
                {
                    ctx.DrawRectangle(PastBrush, null, rounded);
                }
                else if (current)
                {
                    if (Animate)
                    {
                        // Glow pulses in at beat start then holds steady
                        var glowT    = Math.Max(0.3, pulseT);
                        var expand   = glowT * 4.0;
                        var glowRect = new RoundedRect(cellRect.Inflate(expand), CellRadius + expand * 0.4);
                        ctx.DrawRectangle(GlowMid, null, glowRect);
                    }
                    ctx.DrawRectangle(CurrentBrush, null, rounded);
                }
                else
                {
                    ctx.DrawRectangle(FutureBrush, null, rounded);
                }
            }
        }
    }

    // ── Header drawing ────────────────────────────────────────────────────────

    private void DrawHeader(DrawingContext ctx, int beatIdx)
    {
        const double y    = 3.0;
        const double size = 11.0;
        double x          = SidePad + 2.0;

        x = DrawSegment(ctx, "PHRASE", LabelMuted, x, y, size);
        x = DrawSegment(ctx, "  ", LabelMuted, x, y, size);
        x = DrawSegment(ctx, $"{PhraseNumber}/{TotalPhrases}", LabelAccent, x, y, size);
        x = DrawSegment(ctx, "  ·  ", LabelMuted, x, y, size);
        x = DrawSegment(ctx, "BEAT", LabelMuted, x, y, size);
        x = DrawSegment(ctx, "  ", LabelMuted, x, y, size);
        DrawSegment(ctx, $"{beatIdx + 1}/{TotalBeats}", LabelAccent, x, y, size);
    }

    private static double DrawSegment(DrawingContext ctx, string text, IBrush brush,
                                      double x, double y, double size)
    {
        var ft = new FormattedText(text,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, Mono, size, brush);
        ctx.DrawText(ft, new Point(x, y));
        return x + ft.Width;
    }
}
