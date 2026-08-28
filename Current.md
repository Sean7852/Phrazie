I need to refactor the CollectionsView.axaml to match the new high-fidelity design. This involves a complete overhaul of the header, card layout, and semantic styling.

1. Page Header:

Title & Stats: Display 'Collections' in a large, bold font, with a secondary label below it showing total counts (e.g., '6 collections · 188 clips') in {StaticResource TextBrushSecondary}.

Search & Filters: Add a top-right toolbar containing:

A search bar with a subtle border and placeholder 'Search collections...'.

Segmented filter buttons for 'Recent', 'A-Z', and 'Size'.

New Collection Button: A pill-shaped button on the left with an amber '+' icon and the text 'New Collection'.

2. Collection Card (The Grid Item):

Container: Use a Border with {StaticResource CardBackgroundBrush}, a 1px {StaticResource BorderBrushDefault}, and a CornerRadius of 8.

Thumbnail: A large 16:9 image at the top with ClipToBounds="True" to match the card rounding.

Info Section:

Title (e.g., 'Tech House') in bold white.

Clip count (e.g., '24 clips') in a smaller, muted gray font.

A three-dot vertical menu button (⋮) in the top-right corner of the info section.

Action Footer:

Play Button: A wide, dark button spanning most of the card width with a purple or blue play arrow (▶) and the text 'Play'.

Delete Button: A small, square dark button on the right with a trash icon.

3. Visual Polish:

Use the color tokens from the new Colors.axaml (e.g., PhzSurface2 for cards).

Layout: Use a WrapPanel or a responsive UniformGrid to display the cards with a gap of 20px between them.

Hover State: When hovering over a card, the border should glow slightly using {StaticResource PhzAccentGlow}.

4. MVVM Integration:

Bind the ItemsControl to your existing CollectionRepository data.

Ensure the 'Play' button command launches the session with the selected collection.