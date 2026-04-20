I need to implement the 'UP NEXT' preview card for the right panel of Phrazie. This should be a standalone section wrapped in a rounded-corner Border (#0D0D12).

1. Header Row:

Title: 'UP NEXT' in small, mono-spaced gray text (#7A8FA6) with wide letter spacing.

CUE Button: A pill-shaped button on the right with an orange border (#FF9900), orange text 'CUE →', and a dark background.

2. Content Area (Below Header):

Thumbnail: A rounded-corner Image (16:9 aspect ratio) on the left.

Metadata Group (Right of Thumbnail):

Tag Row: Two small pill-shaped badges.

GEOMETRY(What collection is this curent video from): A dark gray badge with a white diamond icon.

BUILD(The State name, what collection/state is this curent video from): A purple-tinted badge (#443366) with a purple dot.

Clip Info: Large white title 'Violet Grid' followed by a smaller, dimmed ID string 'CLP-128-C'.

3. Action Button:

Change Clip: A wide, dark button at the bottom of the card with the text 'Change clip' and a small '>' chevron on the far right. Use a very subtle border (#1C1C2C).

Technical Requirements:


Styling: Use CornerRadius on all borders to maintain the smooth industrial aesthetic. The badges should have a CornerRadius of at least 10 to create the pill shape.

Layout: Use a Grid for the main card structure and StackPanels for the small tag rows.

Please also make all color read from the color file, so all title use the same color.