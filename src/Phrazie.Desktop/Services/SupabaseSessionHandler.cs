using System.Text.Json;

namespace Phrazie.Desktop.Services;

/// <summary>
/// Saves only the access + refresh tokens to disk.
/// On the next startup, App.axaml.cs calls SetSession to restore the session
/// without needing to serialize the full Gotrue Session graph.
/// File: %LOCALAPPDATA%/Phrazie/session.json
/// </summary>
public static class SupabaseSessionHandler
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Phrazie", "session.json");

    private sealed record TokenData(string AccessToken, string RefreshToken);

    public static void Save(string accessToken, string refreshToken)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(new TokenData(accessToken, refreshToken)));
        }
        catch { }
    }

    public static (string AccessToken, string RefreshToken)? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var data = JsonSerializer.Deserialize<TokenData>(File.ReadAllText(FilePath));
            if (data is null || string.IsNullOrEmpty(data.AccessToken)) return null;
            return (data.AccessToken, data.RefreshToken);
        }
        catch { return null; }
    }

    public static void Destroy()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); }
        catch { }
    }
}
