using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Engine.Mock;

public sealed class MockSessionService : ISessionService
{
    public Session Current { get; } = new();

    public event Action<Session>? SessionChanged;

    public Task SetActiveCollectionAsync(Collection collection)
    {
        Current.ActiveCollection = collection;
        Current.CurrentState = collection.States.FirstOrDefault();
        NotifyChanged();
        return Task.CompletedTask;
    }

    public Task TransitionToStateAsync(State state)
    {
        Current.CurrentState = state;
        Current.PendingTrigger = null;
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
