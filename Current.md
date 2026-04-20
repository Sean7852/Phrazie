I want to replace the standard Windows file browser for 'Add/Change Clip' with a custom Internal Browser View. This view should only navigate data from our SQLite database.

1. Navigation Logic (Drill-Down):

Level 1 (Collections): Display all available Collections as large icons or cards.

Level 2 (States): Clicking a Collection icon navigates 'into' it, displaying all States associated with that collection.

Level 3 (Clips): Clicking a State displays all enabled Clips within that state.

Selection: Clicking a Clip selects it and closes the browser, returning the clip data to the LivePerformanceViewModel.

2. The Header & Path Tracking:

Breadcrumb Path: In the browser title area, implement a dynamic path display (e.g., Library > Techno Set > Build Up).

Back Button: Provide a 'Back' arrow next to the path to navigate up one level.

3. UI & Aesthetic:

Use a Grid with a sidebar for categories and a WrapPanel for the main content area to show icons.

Match the Phrazie industrial dark theme: #0D0D12 background and #1C1C2C borders.

Add a 'cool animation' for navigation: Use a Cross-Fade or Slide transition when moving between levels (Collection → State) so it feels like a modern media browser.

4. Technical Requirements:

MVVM: Create a ClipBrowserViewModel that uses SqliteCollectionRepository and SqliteClipRepository to fetch data.

Dialog Host: Implement this as a ContentControl or a modal overlay within the MainWindow.