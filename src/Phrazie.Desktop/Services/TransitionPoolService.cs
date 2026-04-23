using Phrazie.Core.Enums;

namespace Phrazie.Desktop.Services;

/// <summary>
/// Shared singleton that holds the user's Out/In transition pool settings and
/// vends a randomly-picked transition for each side on demand.
/// </summary>
public sealed class TransitionPoolService
{
    private List<TransitionType> _armedOut     = [TransitionType.Fade];
    private List<TransitionType> _armedIn      = [TransitionType.Cut];
    private bool                 _isFixed      = true;
    private double               _fixedSeconds = 1.0;
    private double               _minSeconds   = 0.5;
    private double               _maxSeconds   = 2.0;

    /// <summary>
    /// How many user-phrazes each clip plays before transitioning.
    /// 1 phraze = 8 bars = 32 beats = 2 IBeatClock phrases.
    /// 0 means free-running (advance on clip end).
    /// </summary>
    public int ClipDurationPhrases { get; set; } = 4;

    public void SetArmedOut(IEnumerable<TransitionType> types) => _armedOut = [.. types];
    public void SetArmedIn(IEnumerable<TransitionType> types)  => _armedIn  = [.. types];

    public void SetDuration(bool isFixed, double fixedSeconds, double min, double max)
    {
        _isFixed      = isFixed;
        _fixedSeconds = fixedSeconds;
        _minSeconds   = min;
        _maxSeconds   = max;
    }

    public (TransitionType Type, double Duration) PickOut() => Pick(_armedOut);
    public (TransitionType Type, double Duration) PickIn()  => Pick(_armedIn);

    /// <summary>Returns the worst-case out-transition duration for lookahead purposes.</summary>
    public double GetMaxOutDuration() => _isFixed ? _fixedSeconds : _maxSeconds;

    private (TransitionType, double) Pick(List<TransitionType> pool)
    {
        var type = pool.Count > 0
            ? pool[Random.Shared.Next(pool.Count)]
            : TransitionType.Cut;

        var duration = _isFixed
            ? _fixedSeconds
            : _minSeconds + Random.Shared.NextDouble() * Math.Max(0, _maxSeconds - _minSeconds);

        return (type, Math.Max(0.05, duration));
    }
}
