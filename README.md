# Phrazie

A high-performance, cross-platform VJ desktop application built with Avalonia UI and .NET 9. Phrazie is designed for DJs who want professional-grade live visuals without a dedicated VJ, allowing for precise, timing-based visual orchestration with minimal live interaction.

## Core Philosophy: Live Visual Timing

Phrazie moves away from traditional, manual VJing. Its core strength is **Timing-Based Automation**. You can pre-schedule visual state changes to occur exactly on musical transitions without interrupting your DJ performance.

- **Musical Timing:** Trigger visuals based on bars or phrases (e.g., "Drop in 8 bars", "Break in 4 bars").
- **State-Based Workflow:** Arrange clips into distinct states (Normal, Break, Drop) and switch between them seamlessly.
- **Set Consistency:** Map specific video collections to your sets to maintain a consistent vibe while allowing for randomized, dynamic clip selection.

## Key Features (MVP)

- **Dynamic Trigger System:** Schedule visual transitions precisely by bars or seconds.
- **Smart Collection Management:** Organize your visual assets into "Collections" and "States" (Normal/Break/Drop).
- **Randomized Playback:** Keep visuals fresh by automatically cycling through a pool of clips assigned to the current state.
- **Mobile Remote Control:** A web-based remote to trigger state changes and monitor countdowns from your phone or tablet.
- **Dual-Display Output:** High-performance video output dedicated to the club screen/projector.
- **Emergency Switch:** Instantly clear or black-out the visual output in one click.

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
