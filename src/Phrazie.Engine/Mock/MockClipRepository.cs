using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Engine.Mock;

/// <summary>
/// In-memory clip library. Hard-coded seeds are included for demo purposes.
/// Replace with a real file-system scan / SQLite store in Phase 3.
/// </summary>
public sealed class MockClipRepository : IClipRepository
{
    private readonly List<Clip> _clips =
    [
        new Clip { DisplayName = "Dark Tunnel Loop",   Duration = TimeSpan.FromSeconds(12) },
        new Clip { DisplayName = "Strobe Flash",       Duration = TimeSpan.FromSeconds(4)  },
        new Clip { DisplayName = "Crowd Energy",       Duration = TimeSpan.FromSeconds(20) },
        new Clip { DisplayName = "Bass Drop Reveal",   Duration = TimeSpan.FromSeconds(8)  },
        new Clip { DisplayName = "Slow Neon Drift",    Duration = TimeSpan.FromSeconds(16) },
        new Clip { DisplayName = "Abstract Grid",      Duration = TimeSpan.FromSeconds(10) },
    ];

    public Task<IReadOnlyList<Clip>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Clip>>(_clips.AsReadOnly());

    public Task AddAsync(Clip clip)
    {
        if (_clips.All(c => c.Id != clip.Id))
            _clips.Add(clip);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Clip clip)
    {
        var i = _clips.FindIndex(c => c.Id == clip.Id);
        if (i >= 0) _clips[i] = clip;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _clips.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
}
