namespace Phrazie.Core.Interfaces;

/// <summary>
/// Embedded HTTP + WebSocket server that powers the mobile remote control.
/// </summary>
public interface IRemoteServer : IDisposable
{
    bool    IsRunning { get; }
    string? LocalUrl  { get; }

    /// <summary>Raised when a remote client selects a state as "next".</summary>
    event Action<Guid>? NextStateRequested;

    Task StartAsync(int port = 8765);
    void Stop();
}
