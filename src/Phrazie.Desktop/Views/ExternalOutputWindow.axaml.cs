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
    private readonly DispatcherTimer      _hideBarTimer;

    public ExternalOutputWindow(VideoPlaybackService vps)
    {
        _vps = vps;
        InitializeComponent();

        VideoOutput.Attach(vps);

        // Auto-hide title bar after 2 s of inactivity when fullscreen
        _hideBarTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _hideBarTimer.Tick += (_, _) =>
        {
            _hideBarTimer.Stop();
            if (WindowState == WindowState.FullScreen)
                TitleBar.IsVisible = false;
        };

        // Show/hide title bar based on window state
        PropertyChanged += (_, e) =>
        {
            if (e.Property != WindowStateProperty) return;
            if (WindowState == WindowState.FullScreen)
            {
                TitleBar.IsVisible = false;
                _hideBarTimer.Stop();
            }
            else
            {
                TitleBar.IsVisible = true;
                _hideBarTimer.Stop();
            }
        };

        // Reveal title bar when pointer enters the top of the screen in fullscreen
        PointerMoved += (_, e) =>
        {
            if (WindowState != WindowState.FullScreen) return;
            var y = e.GetPosition(this).Y;
            if (y < 50)
            {
                TitleBar.IsVisible = true;
                _hideBarTimer.Stop();
                _hideBarTimer.Start();
            }
        };

        // Wire title bar controls
        DragArea.PointerPressed += (_, e) => BeginMoveDrag(e);

        MinBtn.Click += (_, _) => WindowState = WindowState.Minimized;

        MaxBtn.Click += (_, _) => ToggleFullscreen();

        CloseBtn.Click += (_, _) => Close();

        // Double-click video to toggle fullscreen
        VideoOutput.DoubleTapped += (_, _) => ToggleFullscreen();

        // Escape exits fullscreen
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && WindowState == WindowState.FullScreen)
                ToggleFullscreen();
        };

        // Crossfade on clip change
        _clipChangedHandler = _clip =>
            Dispatcher.UIThread.Post(() => _ = PlayCrossfadeAsync());
        _vps.ClipChanged += _clipChangedHandler;

        Closed += (_, _) =>
        {
            _hideBarTimer.Stop();
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
        VideoOutput.Opacity = 0.0;
        await VideoOutput.PlayTransitionAsync(TransitionType.Fade, 0.5, outgoing: false);
    }
}
