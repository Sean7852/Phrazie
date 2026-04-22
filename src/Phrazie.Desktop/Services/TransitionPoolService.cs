using Phrazie.Core.Enums;

namespace Phrazie.Desktop.Services;

/// <summary>
/// Shared singleton that holds the user's transition pool settings and
/// vends a randomly-picked transition on demand.
/// </summary>
public sealed class TransitionPoolService
{
    private List<TransitionType> _armed        = [TransitionType.Fade, TransitionType.Cut];
    private bool                 _isFixed      = true;
    private double               _fixedSeconds = 1.0;
    private double               _minSeconds   = 0.5;
    private double               _maxSeconds   = 2.0;

    public void SetArmed(IEnumerable<TransitionType> types)
        => _armed = [.. types];

    public void SetDuration(bool isFixed, double fixedSeconds, double min, double max)
    {
        _isFixed      = isFixed;
        _fixedSeconds = fixedSeconds;
        _minSeconds   = min;
        _maxSeconds   = max;
    }

    /// <summary>Returns a randomly chosen armed transition and its duration in seconds.</summary>
    public (TransitionType Type, double Duration) Pick()
    {
        var type = _armed.Count > 0
            ? _armed[Random.Shared.Next(_armed.Count)]
            : TransitionType.Cut;

        var duration = _isFixed
            ? _fixedSeconds
            : _minSeconds + Random.Shared.NextDouble() * Math.Max(0, _maxSeconds - _minSeconds);

        return (type, Math.Max(0.05, duration));
    }
}
