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

### 3. Clip Management
- [x] Import local video files
- [x] Assign clips to states (mock)
- [x] Playback mode: Loop
- [x] Playback mode: Random
- [x] Playback mode: Sequential

### 4. Live Performance Screen
- [x] Current state display
- [x] Next state selection
- [x] Trigger scheduling
- [x] Countdown indicator
- [x] Emergency switch

### 5. Trigger System
- [x] Trigger type: Immediate
- [x] Trigger type: After X seconds
- [x] Trigger type: After X bars
- [x] UI label: "Drop in 8 bars"
- [x] UI label: "Break in 4 bars"

### 6. Playback (Basic)
- [ ] Video playback
- [ ] State-based switching
- [ ] External full-screen output
- [ ] Preview window

### 7. Mobile Remote (Basic)
- [ ] View current state
- [ ] Select next state
- [ ] Control trigger
- [ ] Emergency override
- [ ] First version: web-based remote

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
- [x] Persist session across restarts (tokens saved to disk, restored via SetSession on launch)
- [ ] Free trial (7–14 days)
- [ ] Subscription (basic implementation)

---

## Development Phases

### Phase 1 — App Foundation (1–2 weeks)
> Goal: App runs with basic structure

- [x] Project setup
- [x] Page skeletons
- [x] Navigation
- [x] UI component base
- [x] Mock data

### Phase 2 — Data Model & Mock Interaction (2 weeks)
> Goal: Fully simulated workflow

- [x] Collection / State CRUD (create, delete, rename, add/remove states)
- [x] Mock clip assignment
- [x] Trigger simulation
- [x] Countdown UI

### Phase 3 — Core Functionality (3–4 weeks)
> Goal: Functional prototype

- [x] Clip import (drag & drop + file picker)
- [x] Video thumbnail extraction (Windows Shell)
- [x] Video playback (LibVLC)
- [x] State-based clip switching
- [x] Auth service (Supabase sign-up / sign-in / sign-out)
- [x] Auth gate UI (login screen before app)
- [x] Local database persistence (SQLite — collections, states, clips)
- [x] Account settings section (email display, sign-out, About dialog)
- [x] Session persistence across restarts
- [ ] Trigger execution wired to real playback

### Phase 4 — Output & Remote (2–3 weeks)
> Goal: Real-world testable

- [ ] External display output
- [ ] Mobile remote control
- [ ] Session sync

### Phase 5 — Polish & Testing (2 weeks)
> Goal: Stable MVP

- [ ] Bug fixes
- [ ] UI refinement
- [ ] Performance tuning

---

## UI / UX Improvements (Done)

- [x] Remove gear icon from collection cards — settings moved to management page
- [x] Single-click collection card opens management page
- [x] Collection detail rich header — cover image, name, description, edit icon
- [x] Edit collection modal — name, description, cover image picker, delete
- [x] Settings page with master menu + detail panel
- [x] Hotkey mapping — rebindable shortcuts, live key capture
- [x] State cards redesigned — thin color tab on left edge, horizontal clip scroll row
- [x] Video thumbnails in clip tiles (Windows Shell thumbnail API)
- [x] State name and color strip both clickable to open edit modal
- [x] Live page redesigned — video output left, controls panel right
- [x] Login / sign-up screen with toggle, error handling, dark card design
- [x] Account section in Settings — email display, sign-out, About Phrazie dialog
- [x] Cover image saved to app-owned storage (resilient to original file deletion)
- [x] Session persists across restarts — no re-login until explicit sign-out

---

## Releases

- [x] v0.1.0 — First public preview (Apr 2026) — collections, states, clip import, live page with video playback
