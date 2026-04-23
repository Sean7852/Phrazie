using Avalonia.Controls;
using Phrazie.Desktop.Views;

namespace Phrazie.Desktop.Services;

/// <summary>
/// Manages the lifecycle of the external output window.
/// Dual-screen: opens fullscreen on the second monitor.
/// Single-screen: opens as a floating 640×360 preview window.
/// Must be called from the UI thread.
/// </summary>
public sealed class ProjectionService
{
    private readonly VideoPlaybackService _vps;
    private ExternalOutputWindow?         _window;

    public bool IsProjecting { get; private set; }

    /// <summary>Fires on the UI thread whenever the projection state changes.</summary>
    public event Action<bool>? IsProjectingChanged;

    public ProjectionService(VideoPlaybackService vps) => _vps = vps;

    public void Toggle()
    {
        if (IsProjecting)
            CloseWindow();
        else
            OpenWindow();
    }

    private void OpenWindow()
    {
        _window = new ExternalOutputWindow(_vps);

        _window.Opened += OnWindowOpened;
        _window.Closed += OnWindowClosed;

        _window.Show();

        IsProjecting = true;
        IsProjectingChanged?.Invoke(true);
    }

    private void CloseWindow()
    {
        _window?.Close();
        // OnWindowClosed updates state
    }

    private void OnWindowOpened(object? sender, EventArgs _)
    {
        if (_window is null) return;

        var screens = _window.Screens?.All;
        if (screens is { Count: > 1 })
        {
            // Snap fullscreen to the second monitor
            var target = screens.FirstOrDefault(s => !s.IsPrimary) ?? screens[0];
            _window.Position    = target.Bounds.TopLeft;
            _window.WindowState = WindowState.FullScreen;
        }
        else
        {
            // Single-screen: open as a floating preview so the main UI remains usable
            _window.Width       = 640;
            _window.Height      = 360;
            _window.WindowState = WindowState.Normal;
            _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private void OnWindowClosed(object? sender, EventArgs _)
    {
        _window = null;
        IsProjecting = false;
        IsProjectingChanged?.Invoke(false);
    }
}
