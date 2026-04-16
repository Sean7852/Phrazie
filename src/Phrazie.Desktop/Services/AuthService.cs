using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;
using Supabase;

namespace Phrazie.Desktop.Services;

/// <summary>
/// Supabase-backed implementation of IAuthService.
/// Wraps Gotrue sign-up / sign-in / sign-out and keeps SessionStore in sync.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly Client       _supabase;
    private readonly ISessionStore _store;

    public AuthService(Client supabase, ISessionStore store)
    {
        _supabase = supabase;
        _store    = store;
    }

    public async Task<AuthResult> SignUpAsync(string email, string password)
    {
        try
        {
            var session = await _supabase.Auth.SignUp(email, password);
            if (session?.User is null)
                return AuthResult.Fail("Sign-up failed — no user returned.");

            if (session.AccessToken is not null && session.RefreshToken is not null)
                SupabaseSessionHandler.Save(session.AccessToken, session.RefreshToken);

            var user = new AuthUser { Id = session.User.Id!, Email = session.User.Email! };
            _store.SetUser(user);
            return AuthResult.Ok(user);
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(ExtractMessage(ex));
        }
    }

    public async Task<AuthResult> SignInAsync(string email, string password)
    {
        try
        {
            var session = await _supabase.Auth.SignIn(email, password);
            if (session?.User is null)
                return AuthResult.Fail("Invalid email or password.");

            if (session.AccessToken is not null && session.RefreshToken is not null)
                SupabaseSessionHandler.Save(session.AccessToken, session.RefreshToken);

            var user = new AuthUser { Id = session.User.Id!, Email = session.User.Email! };
            _store.SetUser(user);
            return AuthResult.Ok(user);
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(ExtractMessage(ex));
        }
    }

    public async Task SignOutAsync()
    {
        try   { await _supabase.Auth.SignOut(); }
        catch { /* ignore network errors on sign-out */ }
        finally
        {
            SupabaseSessionHandler.Destroy();
            _store.ClearUser();
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static string ExtractMessage(Exception ex)
    {
        // Supabase wraps HTTP error bodies in the Message; strip the JSON noise
        var msg = ex.Message;
        if (msg.Contains("Invalid login credentials"))
            return "Invalid email or password.";
        if (msg.Contains("User already registered"))
            return "An account with this email already exists.";
        if (msg.Contains("Password should be"))
            return "Password must be at least 6 characters.";
        return msg;
    }
}
