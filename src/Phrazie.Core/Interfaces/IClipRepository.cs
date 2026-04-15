using Phrazie.Core.Models;

namespace Phrazie.Core.Interfaces;

public interface IClipRepository
{
    Task<IReadOnlyList<Clip>> GetAllAsync();
}
