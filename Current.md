I need to implement a bottom control bar for Phrazie consisting of three distinct functional groups: Play Controls, BPM Controls, and Record Controls. Use a horizontal Grid or StackPanel with a deep charcoal background (#0D0D12).

Group 1: Play Controls (Left)

Layout: A small horizontal group of three buttons.

Previous/Next: Smaller, dark buttons with arrow icons.

Play/Pause Button: A large, prominent Vibrant Orange (#FF9900) button. When playing, show a 'Pause' (||) icon. When stopped, show a 'Play' (▶) icon. Use rounded corners (4px).

Group 2: BPM Controls (Center)

BPM Display: Large white numeric text (e.g., '128') centered at the top.

Slider: Below the number, implement a custom-styled Slider. The 'Thumb' (the handle) should be a glowing orange circle. The 'Track' should be orange to the left of the thumb and dark gray to the right.

Range Labels: Small gray text for '60' and '200' at the ends of the slider.

Buttons: Include a '-' button on the left, and a '+' button followed by a 'TAP' button on the right. The 'TAP' button should have an orange border and orange text.

Group 3: Record Controls (Right)

REC Button: A button with a red 'Record' dot and the text 'REC'. When active, it should have a subtle red outer glow.

Status Panel: A dark container to the right of the REC button showing:

Timer: Large mono-spaced white text (e.g., '00:00:00').

Metadata: Smaller blue-gray text below the timer showing resolution and size (e.g., '1080p60 · 0.0 MB').