using Phrazie.Core.Models;

namespace Phrazie.Core.Interfaces;

public interface ICollectionRepository
{
    Task<IReadOnlyList<Collection>> GetAllAsync();
    Task<Collection?> GetByIdAsync(Guid id);
    Task<Collection> CreateAsync(string name);
    Task UpdateAsync(Collection collection);
    Task DeleteAsync(Guid id);
}
