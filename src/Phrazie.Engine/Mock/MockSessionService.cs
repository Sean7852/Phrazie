using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Engine.Mock;

/// <summary>
/// Pre-seeded with a default "Live Set" collection so the Live screen is
/// immediately usable without the user having to select a collection first.
/// </summary>
public sealed class MockSessionService : ISessionService
{
    public Session Current { get; }

    public event Action<Session>? SessionChanged;

    public MockSessionService()
    {
        var normal = new State { Name = "Normal" };
        var @break = new State { Name = "Break"  };
        var drop   = new State { Name = "Drop"   };

        var defaultCollection = new Collection
        {
            Name   = "Live Set",
            States = [normal, @break, drop]
        };

        Current = new Session
        {
            ActiveCollection = defaultCollection,
            CurrentState     = normal,
            Bpm              = 128.0
        };
    }

    public Task SetActiveCollectionAsync(Collection collection)
    {
        Current.ActiveCollection = collection;
        Current.CurrentState     = collection.States.FirstOrDefault();
        NotifyChanged();
        return Task.CompletedTask;
    }

    public Task TransitionToStateAsync(State state)
    {
        Current.CurrentState     = state;
        Current.PendingTrigger   = null;
        NotifyChanged();
        return Task.CompletedTask;
    }

    public Task SetBpmAsync(double bpm)
    {
        Current.Bpm = bpm;
        NotifyChanged();
        return Task.CompletedTask;
    }

    private void NotifyChanged() => SessionChanged?.Invoke(Current);
}
