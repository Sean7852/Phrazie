using Phrazie.Core.Models;

namespace Phrazie.Core.Interfaces;

public interface ISessionService
{
    Session Current { get; }

    /// <summary>Raised whenever session state changes (collection, state, trigger).</summary>
    event Action<Session>? SessionChanged;

    Task SetActiveCollectionAsync(Collection collection);
    Task TransitionToStateAsync(State state);
    Task SetBpmAsync(double bpm);
}
