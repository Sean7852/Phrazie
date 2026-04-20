# Task: Clip Queue View

New sidebar tab below "Collection" and "Live" showing the current state's clip list.

## 1. Navigation
- Add a **"Clip Queue"** tab to the main sidebar nav (after Collection / Live)
- New view: `ClipQueueView.axaml`

## 2. UI Structure

### List
- `ListBox` / `ItemsControl` bound to `CurrentStateClips`
- Each row:
  - **Thumbnail** — 16:9 small image, left
  - **Clip name** + **BPM** metadata, centre
  - **Remove (✕)** icon button, right
  - **Drag handle (≡)** icon, right

### Active clip
- Currently-playing clip has a slow-breathing **green glow** border

## 3. Animations
| Trigger | Animation |
|---|---|
| Tab opens | Items slide in from left, 0.05s stagger per row |
| Clip removed | Scale 1→0 + fade out ("poof"), rows below slide up |
| Active clip | Looping opacity pulse on green glow border |

