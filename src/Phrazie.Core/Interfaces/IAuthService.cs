using Phrazie.Core.Models;

namespace Phrazie.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResult> SignUpAsync(string email, string password);
    Task<AuthResult> SignInAsync(string email, string password);
    Task             SignOutAsync();
}
