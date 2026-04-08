# Phrazie

A cross-platform VJ desktop application built with Avalonia UI and .NET 9. Phrazie reads your installed Rekordbox library, links tracks to local video clips, and plays them live to a club screen.

## Features

- Browse Rekordbox playlists and track metadata (BPM, key, cue points)
- Link tracks to local video clip files
- Live video playback synced to your DJ set
- Dual-display output: control surface + club screen

## Solution Structure

```
Phrazie.sln
└── src/
    ├── Phrazie.Desktop/   Avalonia UI — windows, views, view models
    ├── Phrazie.Core/      Application logic, MIDI input, playback engine
    └── Phrazie.Data/      Rekordbox SQLite + binary file parsing
```

### Project responsibilities

| Project | Role | Key dependencies |
|---|---|---|
| `Phrazie.Desktop` | UI shell, navigation, video output window | Avalonia 12, Avalonia.Themes.Fluent |
| `Phrazie.Core` | Playlist sync, MIDI mapping, video scheduling | NAudio (MIDI), → Phrazie.Data |
| `Phrazie.Data` | Read Rekordbox `master.db`, parse `.pdb`/`.edb` files | Microsoft.Data.Sqlite, Dapper |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- Pioneer Rekordbox installed (for database access)

## Getting Started

```bash
git clone https://github.com/your-org/phraze.git
cd phraze
dotnet build Phrazie.sln
dotnet run --project src/Phrazie.Desktop
```

## Rekordbox Database

Rekordbox stores its library in a SQLite database (`master.db`) located at:

| OS | Path |
|---|---|
| Windows | `%APPDATA%\Pioneer\rekordbox\master.db` |
| macOS | `~/Library/Application Support/Pioneer/rekordbox/master.db` |

Phrazie opens the database **read-only** and never modifies it.

## License

MIT
