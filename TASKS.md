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

### 9. Account & Trial
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

- [x] Clip import
- [ ] Playback switching
- [ ] State transitions
- [ ] Trigger execution

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
