I want to unify the application so it only shows one icon in the Windows Taskbar. the "ClipManageWindow" window is currently showing up as a separate instance in the System menu.

Task: Convert the ClipManageWindow into a Modal Dialog or a Child Window.

Requirements:

Set the Owner of the ClipManageWindow to MainWindow.

Set ShowInTaskbar="False" in the ClipManageWindow XAML.

Ensure the ClipManageWindow opens centered relative to the main app window.

Please update the logic where the ClipManageWindow is instantiated and the ClipManageWindow.axaml file.
