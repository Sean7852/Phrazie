using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Engine.Mock;

/// <summary>
/// In-memory mock implementation. Replace with a SQLite-backed version in Phase 3.
/// </summary>
public sealed class MockCollectionRepository : ICollectionRepository
{
    private readonly List<Collection> _store = new()
    {
        new Collection
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000001"),
            Name = "Dark",
            States =
            [
                new State { Name = "Normal" },
                new State { Name = "Break" },
                new State { Name = "Drop" },
            ]
        },
        new Collection
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000002"),
            Name = "Tech",
            States =
            [
                new State { Name = "Normal" },
                new State { Name = "Break" },
                new State { Name = "Drop" },
            ]
        },
        new Collection
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000003"),
            Name = "Euphoric",
            States =
            [
                new State { Name = "Normal" },
                new State { Name = "Break" },
                new State { Name = "Drop" },
            ]
        }
    };

    public Task<IReadOnlyList<Collection>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Collection>>(_store.AsReadOnly());

    public Task<Collection?> GetByIdAsync(Guid id) =>
        Task.FromResult(_store.FirstOrDefault(c => c.Id == id));

    public Task<Collection> CreateAsync(string name)
    {
        var collection = new Collection
        {
            Name = name,
            States =
            [
                new State { Name = "Normal" },
                new State { Name = "Break" },
                new State { Name = "Drop" },
            ]
        };
        _store.Add(collection);
        return Task.FromResult(collection);
    }

    public Task UpdateAsync(Collection collection)
    {
        var index = _store.FindIndex(c => c.Id == collection.Id);
        if (index >= 0) _store[index] = collection;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _store.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
}
