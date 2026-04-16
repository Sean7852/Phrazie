using Microsoft.Data.Sqlite;

namespace Phrazie.Desktop.Database;

/// <summary>
/// Manages the single SQLite connection for the local Phrazie database.
/// File is stored in %LOCALAPPDATA%/Phrazie/phrazie.db.
/// All operations are serialized through a semaphore to keep the connection
/// thread-safe while staying non-blocking on the UI thread.
/// </summary>
public sealed class LocalDatabase : IDisposable
{
    private readonly SqliteConnection  _connection;
    private readonly SemaphoreSlim     _lock = new(1, 1);

    public LocalDatabase()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Phrazie");
        Directory.CreateDirectory(folder);

        var dbPath  = Path.Combine(folder, "phrazie.db");
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();

        Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
            cmd.ExecuteNonQuery();
        });

        CreateSchema();
    }

    // ── public helpers ────────────────────────────────────────────────────────

    public async Task ExecuteAsync(Action<SqliteConnection> work)
    {
        await _lock.WaitAsync();
        try   { await Task.Run(() => work(_connection)); }
        finally { _lock.Release(); }
    }

    public async Task<T> QueryAsync<T>(Func<SqliteConnection, T> work)
    {
        await _lock.WaitAsync();
        try   { return await Task.Run(() => work(_connection)); }
        finally { _lock.Release(); }
    }

    public void Dispose()
    {
        _connection.Dispose();
        _lock.Dispose();
    }

    // ── private ───────────────────────────────────────────────────────────────

    private void Execute(Action<SqliteConnection> work) => work(_connection);

    private void CreateSchema()
    {
        Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS collections (
                    id               TEXT PRIMARY KEY,
                    name             TEXT NOT NULL,
                    description      TEXT NOT NULL DEFAULT '',
                    cover_image_path TEXT,
                    created_at       TEXT NOT NULL DEFAULT (datetime('now')),
                    sort_order       INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS states (
                    id              TEXT PRIMARY KEY,
                    collection_id   TEXT NOT NULL,
                    name            TEXT NOT NULL,
                    color           TEXT NOT NULL DEFAULT '#FF4444',
                    playback_mode   INTEGER NOT NULL DEFAULT 0,
                    sort_order      INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (collection_id) REFERENCES collections(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS clips (
                    id            TEXT PRIMARY KEY,
                    file_path     TEXT NOT NULL,
                    display_name  TEXT NOT NULL,
                    duration_ticks INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS state_clips (
                    state_id   TEXT NOT NULL,
                    clip_id    TEXT NOT NULL,
                    sort_order INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY (state_id, clip_id),
                    FOREIGN KEY (state_id) REFERENCES states(id) ON DELETE CASCADE,
                    FOREIGN KEY (clip_id)  REFERENCES clips(id)  ON DELETE CASCADE
                );
                """;
            cmd.ExecuteNonQuery();
        });
    }
}
