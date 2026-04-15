using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Engine.Mock;

/// <summary>
/// Hard-coded mock clips. Replace with a real file-system scan in Phase 3.
/// </summary>
public sealed class MockClipRepository : IClipRepository
{
    private static readonly IReadOnlyList<Clip> _clips =
    [
        new Clip { DisplayName = "Dark Tunnel Loop",   Duration = TimeSpan.FromSeconds(12) },
        new Clip { DisplayName = "Strobe Flash",       Duration = TimeSpan.FromSeconds(4)  },
        new Clip { DisplayName = "Crowd Energy",       Duration = TimeSpan.FromSeconds(20) },
        new Clip { DisplayName = "Bass Drop Reveal",   Duration = TimeSpan.FromSeconds(8)  },
        new Clip { DisplayName = "Slow Neon Drift",    Duration = TimeSpan.FromSeconds(16) },
        new Clip { DisplayName = "Abstract Grid",      Duration = TimeSpan.FromSeconds(10) },
    ];

    public Task<IReadOnlyList<Clip>> GetAllAsync() => Task.FromResult(_clips);
}
