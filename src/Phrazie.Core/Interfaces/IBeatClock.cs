namespace Phrazie.Core.Interfaces;

/// <summary>
/// Continuous BPM-driven phrase clock.
/// Fires PhaseChanged at ~60 fps; Phase runs 0.0 → 1.0 over one phrase (16 beats).
/// </summary>
public interface IBeatClock : IDisposable
{
    bool   IsRunning { get; }
    double Bpm       { get; set; }

    /// <summary>
    /// Raised on a background thread ~60 times per second.
    /// phase: 0.0 = phrase downbeat, approaching 1.0 = phrase end.
    /// </summary>
    event Action<double>? PhaseChanged;

    void Start();
    void Stop();

    /// <summary>Snap the playhead back to the phrase downbeat (phase 0.0).</summary>
    void Reset();
}
