using System.Diagnostics;
using Phrazie.Core.Interfaces;

namespace Phrazie.Desktop.Services;

/// <summary>
/// High-precision BPM phrase clock.
/// Uses Stopwatch (hardware timer) for phase calculation and a background
/// System.Threading.Timer (~16 ms cadence, ≈60 fps) to fire PhaseChanged.
///
/// Phase = 0.0 at every phrase downbeat, approaching 1.0 at phrase end.
/// One phrase = 16 beats (4 bars × 4/4).
///
/// Changing Bpm re-anchors at the current phase — no jump, just a speed change.
/// </summary>
public sealed class BeatClock : IBeatClock
{
    private const int   PhraseBeats  = 16;
    private const int   TickMs       = 16;   // ~62.5 fps

    private readonly Stopwatch _sw    = new();
    private readonly object    _lock  = new();

    private Timer?  _timer;
    private double  _bpm            = 128.0;
    private double  _referencePhase = 0.0;
    private double  _referenceElapsed = 0.0;

    public bool   IsRunning { get; private set; }

    public double Bpm
    {
        get { lock (_lock) return _bpm; }
        set
        {
            lock (_lock)
            {
                _referencePhase   = ComputePhase();
                _referenceElapsed = _sw.Elapsed.TotalSeconds;
                _bpm = Math.Clamp(value, 20.0, 300.0);
            }
        }
    }

    public event Action<double>? PhaseChanged;

    public void Start()
    {
        if (IsRunning) return;
        _sw.Restart();
        lock (_lock)
        {
            _referencePhase   = 0.0;
            _referenceElapsed = 0.0;
        }
        _timer = new Timer(_ => Tick(), null, 0, TickMs);
        IsRunning = true;
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        _sw.Stop();
        IsRunning = false;
    }

    public void Reset()
    {
        lock (_lock)
        {
            _referencePhase   = 0.0;
            _referenceElapsed = _sw.Elapsed.TotalSeconds;
        }
    }

    public void Dispose() => Stop();

    private void Tick()
    {
        var phase = ComputePhase();
        PhaseChanged?.Invoke(phase);
    }

    private double ComputePhase()
    {
        double bpm, refPhase, refElapsed;
        lock (_lock)
        {
            bpm        = _bpm;
            refPhase   = _referencePhase;
            refElapsed = _referenceElapsed;
        }

        var elapsed  = _sw.Elapsed.TotalSeconds - refElapsed;
        var beatHz   = bpm / 60.0;
        var phraseHz = beatHz / PhraseBeats;
        var phase    = (refPhase + elapsed * phraseHz) % 1.0;
        return phase < 0.0 ? phase + 1.0 : phase;
    }
}
