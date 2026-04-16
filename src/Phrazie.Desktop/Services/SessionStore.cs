using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.Services;

/// <summary>
/// Holds the signed-in user in memory for the lifetime of the app.
/// Raises AuthStateChanged whenever sign-in or sign-out occurs.
/// </summary>
public sealed class SessionStore : ISessionStore
{
    public AuthUser? CurrentUser     { get; private set; }
    public bool      IsAuthenticated => CurrentUser is not null;

    public event Action? AuthStateChanged;

    public void SetUser(AuthUser user)
    {
        CurrentUser = user;
        AuthStateChanged?.Invoke();
    }

    public void ClearUser()
    {
        CurrentUser = null;
        AuthStateChanged?.Invoke();
    }
}
