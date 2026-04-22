Please refactor the Queue page into a dual-pane Pool management system. Follow these structural and logical requirements:

1. Layout Structure:

Horizontal Split: Divide the screen into a Clip Pool (Left, 70% width) and a Transition Pool (Right, 30% width).


Sidebar Update: Rename the navigation item from 'Queue' to 'Pool' to reflect the content organization strategy.

2. Clip Pool (Left Pane):

Grid/List Toggle: Add a toggle in the top-left to switch between the current list view and a Grid View featuring large, rounded-corner thumbnails.

Accent Styling: Use the AccentYellow (#FFB800) for the + Add Clip button and ensure the 'NOW PLAYING' footer remains at the bottom of this pane.

3. Transition Pool (Right Pane - Randomization Engine):


Multi-Select Selection: Display transition options (Fade, Cut, Strobe, Glitch, Blur, Invert) as a grid of toggles.

Selection Logic: Clicking an option 'arms' it for randomization. Enabled transitions must be highlighted with the BoxBorderFocused (Yellow) border; disabled ones use BoxBorderDefault.

Randomization Principle: The system will randomly cycle through only the Enabled transitions during clip changes.

4. Transition Timing Suite:

Duration Controls: Add a section below the selection grid titled 'Transition Timing'.

Mode Toggle: Include a switch between 'Fixed' and 'Random Range'.

Inputs: * If Fixed, show a single numeric input/slider for duration (0.1s – 5.0s).

If Random Range, show 'Min' and 'Max' duration inputs.

Rhythm Sync: Add a 'Sync to Beat' checkbox. When enabled, it should snap the transition duration to the nearest musical increment (e.g., 1/2 bar, 1 bar) based on the global BPM.

5. Global Styling:

Use the shared color palette: SurfaceDark for panel backgrounds and TextBrushPrimary for all enabled text.

All box elements must have the established CornerRadius of 8 to match the StateInstance.jpg aesthetic.