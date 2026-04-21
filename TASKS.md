# Phrazie — Task Checklist

## Included Features (Must Have)

### 1. Collections (Content Organization)
- [x] Create collections
- [x] Rename collections
- [x] Delete collections
- [x] Collection description
- [x] Collection cover image (with fallback "P" glyph)
- [x] Collection detail header (cover + name + description)
- [x] Edit collection modal (name, description, cover image, delete)
- [x] Cover image copied to app storage on pick (survives original file deletion)

### 2. State System
- [x] Default states: Normal, Break, Drop
- [x] Rename states
- [x] Add custom states
- [x] Remove states
- [x] State color coding (color tab on card, color picker in edit modal)
- [x] Drag to reorder states

### 3. Clip Management
- [x] Import local video files (drag & drop or file picker)
- [x] Assign clips to states
- [x] Video thumbnail preview in state card
- [x] Playback mode per state: Loop / Random / Sequential
- [x] Clip manager window (grid view per state, opened via "Manage" button)
- [x] Disable / enable clips (greyed-out thumbnail overlay when disabled)
- [x] Delete clips from manager
- [x] Drag & drop video files into clip manager window to add clips
- [x] Rubber-band / marquee multi-select in clip manager
- [x] Multi-select context menu — delete all selected clips at once
- [x] Clip browser modal — 3-level drill-down (Collections → States → Clips) with CrossFade transition

### 4. Live Performance Screen
- [x] Current state display
- [x] Next state selection
- [x] Trigger scheduling (immediate / bars / seconds)
- [x] Countdown indicator
- [x] Emergency switch (immediate cut)
- [x] Video output area (LibVLC via custom NativeControlHost)
- [x] Clip name overlay on video
- [x] Play / pause controls
- [x] Auto-advance to next clip on clip end (loops last clip)
- [x] Auto-play first clip on unpause
- [ ] Manual clip advance

### 5. Trigger System
- [x] Trigger type: Immediate
- [x] Trigger type: After X seconds
- [x] Trigger type: After X bars (BPM-based)
- [x] UI label: "Drop in 8 bars"

### 6. Playback (Basic)
- [x] Video playback (LibVLC, Windows)
- [x] Loop clip when it ends
- [x] State-based clip switching (respects PlaybackMode)
- [ ] External full-screen output (second monitor)
- [ ] Preview window separate from output

### 7. Mobile Remote (Basic)
- [x] View current collection & state
- [x] Select next state from remote
- [x] Control playback (play / pause)
- [x] First version: web-based remote (embedded HTTP + WebSocket, no deps)
- [x] QR code on Live page — scan to open remote on phone
- [ ] Control trigger from remote
- [ ] Emergency override from remote

### 8. Settings
- [x] Settings page (master menu + detail panel)
- [x] Hotkey mapping (rebindable keyboard shortcuts for live controls)
- [ ] Display settings
- [ ] MIDI settings

### 9. Account & Auth
- [x] IAuthService interface (SignUp, SignIn, SignOut)
- [x] ISessionStore interface (CurrentUser, IsAuthenticated, AuthStateChanged)
- [x] AuthService — Supabase Gotrue implementation
- [x] SessionStore — in-memory, fires AuthStateChanged on transition
- [x] Login / sign-up UI (email + password, toggle between modes, error banner)
- [x] Auth gate in MainWindow — shows login overlay when not authenticated
- [x] Account section in Settings — shows email, sign-out button, About dialog
- [ ] Persist session across restarts (custom Supabase SessionHandler)
- [ ] Free trial (7–14 days)
- [ ] Subscription (basic implementation)

### 10. Database
- [x] Setup local SQLite database (Microsoft.Data.Sqlite, WAL mode, foreign keys)
- [x] Save collections, states, clips locally (SqliteCollectionRepository, SqliteClipRepository)
- [ ] Setup remote database (Supabase)
- [ ] Sync between local and remote

---

## Development Phases

### Phase 1 — App Foundation ✅
> Goal: App runs with basic structure

- [x] Project setup
- [x] Page skeletons
- [x] Navigation
- [x] UI component base
- [x] Mock data

### Phase 2 — Data Model & Mock Interaction ✅
> Goal: Fully simulated workflow

- [x] Collection / State CRUD (create, delete, rename, add/remove states)
- [x] Mock clip assignment
- [x] Trigger simulation
- [x] Countdown UI

### Phase 3 — Core Functionality (in progress)
> Goal: Functional prototype

- [x] Clip import (drag & drop + file picker)
- [x] Video thumbnail extraction (Windows Shell)
- [x] Video playback (LibVLC)
- [x] State-based clip switching
- [x] Auto-advance queue (next clip on end, loop last)
- [x] Auth service (Supabase sign-up / sign-in / sign-out)
- [x] Auth gate UI (login screen before app)
- [x] Local database persistence (SQLite — collections, states, clips)
- [x] Account settings section (email display, sign-out, About dialog)
- [x] Session persistence across restarts
- [x] Clip manager window (per-state grid, disable/enable, delete, drag-drop import)
- [ ] Trigger execution wired to real playback

### Phase 4 — Output & Remote (not started)
> Goal: Real-world testable

- [ ] External display output (second monitor / fullscreen)
- [x] Mobile remote control (web-based, QR code, WebSocket)
- [ ] Session sync

### Phase 5 — Polish & Testing (not started)
> Goal: Stable MVP

- [ ] Bug fixes
- [ ] UI refinement
- [ ] Performance tuning

### 11. Window Chrome
- [x] Remove OS title bar (WindowDecorations="None" — Avalonia 12)
- [x] Custom 36px title bar on MainWindow — PHRAZIE label + min/max/close buttons
- [x] Title bar draggable (BeginMoveDrag + ElementRole="TitleBar")
- [x] Double-click title bar to maximize/restore
- [x] Max button icon syncs with window state (▢ / ⊐)
- [x] Close button hover turns red (#C42B1C)
- [x] AboutDialog — floating ✕ close button (top-right), no OS chrome
- [x] ClipManagerWindow — draggable header + ✕ close button, no OS chrome

---

## UI / UX Improvements (Done)

- [x] Queue page — live clip list with active clip highlight (green glow + NOW PLAYING badge), per-item collection/state color badges, video thumbnails, add/remove clips
- [x] Remove gear icon from collection cards — settings moved to management page
- [x] Single-click collection card opens management page
- [x] Collection detail rich header — cover image, name, description inline editing
- [x] Edit collection modal — name, description, cover image picker, delete
- [x] Settings page with master menu + detail panel
- [x] Hotkey mapping — rebindable shortcuts, live key capture
- [x] State cards redesigned — thin color tab on left edge, horizontal clip scroll row
- [x] Video thumbnails in clip tiles (Windows Shell thumbnail API)
- [x] State color tab is drag handle (pointer-capture reorder, no OS drag-drop dependency)
- [x] State name click opens rename/color edit modal
- [x] Live page redesigned — video output left, controls panel right
- [x] Login / sign-up screen with toggle, error handling, dark card design
- [x] Account section in Settings — email display, sign-out, About Phrazie dialog
- [x] Cover image saved to app-owned storage (resilient to original file deletion)
- [x] Session persists across restarts — no re-login until explicit sign-out
- [x] Clip manager window — "Manage" button on each state card, per-state grid view, right-click context menu (disable/enable/delete), drag-drop import
- [x] Clip manager rubber-band multi-select with dark-themed context menu (delete selected)
- [x] Custom dark window chrome — frameless MainWindow with min/max/close buttons, AboutDialog + ClipManagerWindow with ✕ close, no OS title bar

---

## Releases

- [x] v0.1.0 — First public preview (Apr 2026) — collections, states, clip import, live page with video playback
