namespace Phrazie.Core.Models;

public sealed class AuthResult
{
    public bool      Success      { get; init; }
    public string?   ErrorMessage { get; init; }
    public AuthUser? User         { get; init; }

    public static AuthResult Ok(AuthUser user)  => new() { Success = true,  User = user };
    public static AuthResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}
