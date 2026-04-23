using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Phrazie.Core.Enums;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;

namespace Phrazie.Desktop.Views;

public partial class ExternalOutputWindow : Window
{
    private readonly VideoPlaybackService _vps;
    private Action<Clip?>?               _clipChangedHandler;

    public ExternalOutputWindow(VideoPlaybackService vps)
    {
        _vps = vps;
        InitializeComponent();

        VideoOutput.Attach(vps);

        // Show clip crossfade on the external window independently of the main UI
        _clipChangedHandler = _clip =>
            Dispatcher.UIThread.Post(() => _ = PlayCrossfadeAsync());
        _vps.ClipChanged += _clipChangedHandler;

        // Wire chrome bar controls
        DragArea.PointerPressed      += (_, e) => BeginMoveDrag(e);
        FullscreenBtn.Click          += (_, _) => ToggleFullscreen();
        CloseBtn.Click               += (_, _) => Close();

        // Hide chrome bar when fullscreen, show when floating
        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty)
                ChromeBar.IsVisible = WindowState != WindowState.FullScreen;
        };

        // Double-click on the video also toggles fullscreen
        VideoOutput.DoubleTapped += (_, _) => ToggleFullscreen();

        // Escape exits fullscreen
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && WindowState == WindowState.FullScreen)
                ToggleFullscreen();
        };

        Closed += (_, _) =>
        {
            _vps.ClipChanged -= _clipChangedHandler;
            _clipChangedHandler = null;
        };
    }

    private void ToggleFullscreen()
    {
        WindowState = WindowState == WindowState.FullScreen
            ? WindowState.Normal
            : WindowState.FullScreen;
    }

    private async Task PlayCrossfadeAsync()
    {
        // New clip just started — buffer is cleared so the view is already black.
        // Fade in from black over 0.5 s for a smooth club-screen crossfade.
        VideoOutput.Opacity = 0.0;
        await VideoOutput.PlayTransitionAsync(TransitionType.Fade, 0.5, outgoing: false);
    }
}
