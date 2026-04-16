using Microsoft.Data.Sqlite;
using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.Database;

/// <summary>
/// SQLite-backed implementation of ICollectionRepository.
/// UpdateAsync does a full sync: collection row + delete/reinsert states + state_clips.
/// </summary>
public sealed class SqliteCollectionRepository : ICollectionRepository
{
    private readonly LocalDatabase _db;

    public SqliteCollectionRepository(LocalDatabase db) => _db = db;

    // ── read ──────────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<Collection>> GetAllAsync() =>
        _db.QueryAsync<IReadOnlyList<Collection>>(conn =>
        {
            // 1. Load all collections
            var collections = new List<Collection>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, name, description, cover_image_path, created_at FROM collections ORDER BY sort_order, created_at";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    collections.Add(ReadCollection(reader));
            }

            if (collections.Count == 0) return collections;

            var colMap = collections.ToDictionary(c => c.Id);

            // 2. Load all states
            var stateMap = new Dictionary<Guid, State>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, collection_id, name, color, playback_mode FROM states ORDER BY sort_order";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var state = ReadState(reader);
                    var collectionId = Guid.Parse(reader.GetString(1));
                    if (colMap.TryGetValue(collectionId, out var col))
                        col.States.Add(state);
                    stateMap[state.Id] = state;
                }
            }

            if (stateMap.Count == 0) return collections;

            // 3. Load all clips via state_clips join
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT sc.state_id, c.id, c.file_path, c.display_name, c.duration_ticks
                    FROM state_clips sc
                    JOIN clips c ON sc.clip_id = c.id
                    ORDER BY sc.sort_order
                    """;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var stateId = Guid.Parse(reader.GetString(0));
                    if (stateMap.TryGetValue(stateId, out var state))
                        state.Clips.Add(ReadClip(reader, offset: 1));
                }
            }

            return collections;
        });

    public Task<Collection?> GetByIdAsync(Guid id) =>
        _db.QueryAsync<Collection?>(conn =>
        {
            Collection? collection = null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, name, description, cover_image_path, created_at FROM collections WHERE id = $id";
                cmd.Parameters.AddWithValue("$id", id.ToString());
                using var reader = cmd.ExecuteReader();
                if (reader.Read()) collection = ReadCollection(reader);
            }
            if (collection is null) return null;

            var stateMap = new Dictionary<Guid, State>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, collection_id, name, color, playback_mode FROM states WHERE collection_id = $cid ORDER BY sort_order";
                cmd.Parameters.AddWithValue("$cid", id.ToString());
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var state = ReadState(reader);
                    collection.States.Add(state);
                    stateMap[state.Id] = state;
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT sc.state_id, c.id, c.file_path, c.display_name, c.duration_ticks
                    FROM state_clips sc
                    JOIN clips c ON sc.clip_id = c.id
                    JOIN states s ON sc.state_id = s.id
                    WHERE s.collection_id = $cid
                    ORDER BY sc.sort_order
                    """;
                cmd.Parameters.AddWithValue("$cid", id.ToString());
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var stateId = Guid.Parse(reader.GetString(0));
                    if (stateMap.TryGetValue(stateId, out var state))
                        state.Clips.Add(ReadClip(reader, offset: 1));
                }
            }

            return collection;
        });

    // ── write ─────────────────────────────────────────────────────────────────

    public Task<Collection> CreateAsync(string name) =>
        _db.QueryAsync(conn =>
        {
            var collection = new Collection
            {
                Name   = name,
                States =
                [
                    new State { Name = "Normal", Color = "#FF3D3D" },
                    new State { Name = "Break",  Color = "#FFCC00" },
                    new State { Name = "Drop",   Color = "#3388FF" },
                ]
            };
            InsertCollection(conn, collection);
            return collection;
        });

    public Task UpdateAsync(Collection collection) =>
        _db.ExecuteAsync(conn =>
        {
            using var tx = conn.BeginTransaction();
            try
            {
                // Update collection row
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = """
                        UPDATE collections
                        SET name = $name, description = $desc, cover_image_path = $cover
                        WHERE id = $id
                        """;
                    cmd.Parameters.AddWithValue("$id",    collection.Id.ToString());
                    cmd.Parameters.AddWithValue("$name",  collection.Name);
                    cmd.Parameters.AddWithValue("$desc",  collection.Description);
                    cmd.Parameters.AddWithValue("$cover", (object?)collection.CoverImagePath ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                // Delete all states for this collection (cascades to state_clips)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction  = tx;
                    cmd.CommandText  = "DELETE FROM states WHERE collection_id = $cid";
                    cmd.Parameters.AddWithValue("$cid", collection.Id.ToString());
                    cmd.ExecuteNonQuery();
                }

                // Reinsert states and their clip links
                for (int si = 0; si < collection.States.Count; si++)
                {
                    var state = collection.States[si];

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = """
                            INSERT INTO states (id, collection_id, name, color, playback_mode, sort_order)
                            VALUES ($id, $cid, $name, $color, $mode, $order)
                            """;
                        cmd.Parameters.AddWithValue("$id",    state.Id.ToString());
                        cmd.Parameters.AddWithValue("$cid",   collection.Id.ToString());
                        cmd.Parameters.AddWithValue("$name",  state.Name);
                        cmd.Parameters.AddWithValue("$color", state.Color);
                        cmd.Parameters.AddWithValue("$mode",  (int)state.PlaybackMode);
                        cmd.Parameters.AddWithValue("$order", si);
                        cmd.ExecuteNonQuery();
                    }

                    for (int ci = 0; ci < state.Clips.Count; ci++)
                    {
                        var clip = state.Clips[ci];

                        // Upsert clip (may already exist from a prior import)
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = """
                                INSERT OR REPLACE INTO clips (id, file_path, display_name, duration_ticks)
                                VALUES ($id, $path, $name, $ticks)
                                """;
                            cmd.Parameters.AddWithValue("$id",    clip.Id.ToString());
                            cmd.Parameters.AddWithValue("$path",  clip.FilePath);
                            cmd.Parameters.AddWithValue("$name",  clip.DisplayName);
                            cmd.Parameters.AddWithValue("$ticks", clip.Duration.Ticks);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = """
                                INSERT OR REPLACE INTO state_clips (state_id, clip_id, sort_order)
                                VALUES ($sid, $cid, $order)
                                """;
                            cmd.Parameters.AddWithValue("$sid",   state.Id.ToString());
                            cmd.Parameters.AddWithValue("$cid",   clip.Id.ToString());
                            cmd.Parameters.AddWithValue("$order", ci);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });

    public Task DeleteAsync(Guid id) =>
        _db.ExecuteAsync(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM collections WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            cmd.ExecuteNonQuery();
        });

    // ── helpers ───────────────────────────────────────────────────────────────

    private static void InsertCollection(SqliteConnection conn, Collection col)
    {
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO collections (id, name, description, cover_image_path, sort_order)
                    VALUES ($id, $name, $desc, $cover, 0)
                    """;
                cmd.Parameters.AddWithValue("$id",    col.Id.ToString());
                cmd.Parameters.AddWithValue("$name",  col.Name);
                cmd.Parameters.AddWithValue("$desc",  col.Description);
                cmd.Parameters.AddWithValue("$cover", (object?)col.CoverImagePath ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            for (int i = 0; i < col.States.Count; i++)
            {
                var s = col.States[i];
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO states (id, collection_id, name, color, playback_mode, sort_order)
                    VALUES ($id, $cid, $name, $color, $mode, $order)
                    """;
                cmd.Parameters.AddWithValue("$id",    s.Id.ToString());
                cmd.Parameters.AddWithValue("$cid",   col.Id.ToString());
                cmd.Parameters.AddWithValue("$name",  s.Name);
                cmd.Parameters.AddWithValue("$color", s.Color);
                cmd.Parameters.AddWithValue("$mode",  (int)s.PlaybackMode);
                cmd.Parameters.AddWithValue("$order", i);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static Collection ReadCollection(SqliteDataReader r) => new()
    {
        Id              = Guid.Parse(r.GetString(0)),
        Name            = r.GetString(1),
        Description     = r.GetString(2),
        CoverImagePath  = r.IsDBNull(3) ? null : r.GetString(3),
        CreatedAt       = DateTimeOffset.Parse(r.GetString(4)),
    };

    private static State ReadState(SqliteDataReader r) => new()
    {
        Id           = Guid.Parse(r.GetString(0)),
        // column 1 is collection_id — skip
        Name         = r.GetString(2),
        Color        = r.GetString(3),
        PlaybackMode = (PlaybackMode)r.GetInt32(4),
    };

    private static Clip ReadClip(SqliteDataReader r, int offset = 0) => new()
    {
        Id          = Guid.Parse(r.GetString(offset)),
        FilePath    = r.GetString(offset + 1),
        DisplayName = r.GetString(offset + 2),
        Duration    = TimeSpan.FromTicks(r.GetInt64(offset + 3)),
    };
}
