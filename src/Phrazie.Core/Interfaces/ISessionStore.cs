using Phrazie.Core.Models;

namespace Phrazie.Core.Interfaces;

public interface ISessionStore
{
    AuthUser? CurrentUser    { get; }
    bool      IsAuthenticated { get; }

    event Action? AuthStateChanged;

    void SetUser(AuthUser user);
    void ClearUser();
}
