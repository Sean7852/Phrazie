I need to fix the playback deferral issue in Phrazie caused by the NativeControlHost lifecycle. Currently, VLC stops rendering when the LivePerformanceView loses focus because the visual tree is destroyed.

1. Architectural Shift (Off-screen Rendering):

Instead of rendering directly to a NativeControlHost, modify the VideoPlaybackService to use VLC's Callback Rendering (Memory/BitMap) mode.

Implement a Persistent Video Buffer: Create a shared memory buffer (or WritableBitmap) that remains resident in the VideoPlaybackService regardless of which tab is active.

2. Avalonia Integration:

In the VideoView control, replace the NativeControlHost with a standard Avalonia Image control or a CustomControl that overrides Render.

Bind this UI element to the persistent buffer from the service. When the LivePerformanceView is loaded, it should simply start 'listening' to the buffer that is already running in the background.

3. Performance & Synchronization:

Use the Phrazie.Engine clock to ensure the buffer updates are synchronized with the CurrentPhase.

Ensure the buffer supports 1080p60 to match our recording and output requirements.

4. Transition Logic:

Ensure that when I switch from the Clip Queue back to Live, the video is already at the correct frame, perfectly synced with the BPM and Phase.