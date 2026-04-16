using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.Database;

/// <summary>
/// SQLite-backed implementation of IClipRepository.
/// Clips are stored in the shared clips table and linked to states via state_clips.
/// </summary>
public sealed class SqliteClipRepository : IClipRepository
{
    private readonly LocalDatabase _db;

    public SqliteClipRepository(LocalDatabase db) => _db = db;

    public Task<IReadOnlyList<Clip>> GetAllAsync() =>
        _db.QueryAsync<IReadOnlyList<Clip>>(conn =>
        {
            var clips = new List<Clip>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, file_path, display_name, duration_ticks, is_enabled FROM clips ORDER BY display_name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                clips.Add(new Clip
                {
                    Id          = Guid.Parse(reader.GetString(0)),
                    FilePath    = reader.GetString(1),
                    DisplayName = reader.GetString(2),
                    Duration    = TimeSpan.FromTicks(reader.GetInt64(3)),
                    IsEnabled   = reader.GetInt32(4) != 0,
                });
            return clips;
        });

    public Task AddAsync(Clip clip) =>
        _db.ExecuteAsync(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT OR REPLACE INTO clips (id, file_path, display_name, duration_ticks, is_enabled)
                VALUES ($id, $path, $name, $ticks, $enabled)
                """;
            cmd.Parameters.AddWithValue("$id",      clip.Id.ToString());
            cmd.Parameters.AddWithValue("$path",    clip.FilePath);
            cmd.Parameters.AddWithValue("$name",    clip.DisplayName);
            cmd.Parameters.AddWithValue("$ticks",   clip.Duration.Ticks);
            cmd.Parameters.AddWithValue("$enabled", clip.IsEnabled ? 1 : 0);
            cmd.ExecuteNonQuery();
        });

    public Task UpdateAsync(Clip clip) =>
        _db.ExecuteAsync(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                UPDATE clips SET file_path=$path, display_name=$name, duration_ticks=$ticks, is_enabled=$enabled
                WHERE id=$id
                """;
            cmd.Parameters.AddWithValue("$id",      clip.Id.ToString());
            cmd.Parameters.AddWithValue("$path",    clip.FilePath);
            cmd.Parameters.AddWithValue("$name",    clip.DisplayName);
            cmd.Parameters.AddWithValue("$ticks",   clip.Duration.Ticks);
            cmd.Parameters.AddWithValue("$enabled", clip.IsEnabled ? 1 : 0);
            cmd.ExecuteNonQuery();
        });

    public Task DeleteAsync(Guid id) =>
        _db.ExecuteAsync(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM clips WHERE id=$id";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            cmd.ExecuteNonQuery();
        });
}
