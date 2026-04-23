using Avalonia.Controls;
using Phrazie.Desktop.Views;

namespace Phrazie.Desktop.Services;

public enum ProjectionMode
{
    /// <summary>Smart: second screen if available, floating otherwise.</summary>
    Auto,
    FullscreenExternal,
    WindowedExternal,
    FullscreenMain,
    WindowedMain,
}

/// <summary>
/// Manages the lifecycle of the external output window.
/// Must be called from the UI thread.
/// </summary>
public sealed class ProjectionService
{
    private readonly VideoPlaybackService _vps;
    private ExternalOutputWindow?         _window;
    private ProjectionMode                _pendingMode = ProjectionMode.Auto;

    public bool IsProjecting { get; private set; }

    /// <summary>Fires on the UI thread whenever the projection state changes.</summary>
    public event Action<bool>? IsProjectingChanged;

    public ProjectionService(VideoPlaybackService vps) => _vps = vps;

    /// <summary>Toggle on/off using the last chosen mode (or Auto on first open).</summary>
    public void Toggle()
    {
        if (IsProjecting) CloseWindow();
        else              OpenWindow(_pendingMode);
    }

    /// <summary>Apply the given mode — adjusts the existing window if open, opens one if not.</summary>
    public void OpenWithMode(ProjectionMode mode)
    {
        _pendingMode = mode;
        if (_window is not null)
            ApplyMode(mode);   // already open — just adjust state/position
        else
            OpenWindow(mode);
    }

    /// <summary>Closes the output window unconditionally (e.g. on app exit).</summary>
    public void Close() => _window?.Close();

    private void CloseWindow() => _window?.Close();

    private void OpenWindow(ProjectionMode mode)
    {
        _pendingMode = mode;
        _window = new ExternalOutputWindow(_vps);
        _window.Opened += OnWindowOpened;
        _window.Closed += OnWindowClosed;
        _window.Show();
        IsProjecting = true;
        IsProjectingChanged?.Invoke(true);
    }

    private void OnWindowOpened(object? sender, EventArgs _) =>
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => ApplyMode(_pendingMode),
            Avalonia.Threading.DispatcherPriority.Loaded);

    private void ApplyMode(ProjectionMode mode)
    {
        if (_window is null) return;

        var screens   = _window.Screens?.All;
        var secondary = screens?.FirstOrDefault(s => !s.IsPrimary);
        var primary   = screens?.FirstOrDefault(s =>  s.IsPrimary) ?? screens?.FirstOrDefault();

        switch (mode)
        {
            case ProjectionMode.FullscreenExternal:
            {
                var target = secondary ?? primary;
                if (target is not null) _window.Position = target.Bounds.TopLeft;
                _window.WindowState = WindowState.FullScreen;
                break;
            }
            case ProjectionMode.WindowedExternal:
            {
                // Restore to normal — Windows remembers the previous windowed size.
                // On first open, centre the window on the target screen.
                var target = secondary ?? primary;
                if (_window.WindowState != WindowState.Normal && target is not null)
                {
                    var scaling = target.Scaling;
                    var cx = target.Bounds.X + (target.Bounds.Width  - (int)(640 * scaling)) / 2;
                    var cy = target.Bounds.Y + (target.Bounds.Height - (int)(360 * scaling)) / 2;
                    _window.Position = new Avalonia.PixelPoint(cx, cy);
                    _window.Width    = 640;
                    _window.Height   = 360;
                }
                _window.WindowState = WindowState.Normal;
                break;
            }
            case ProjectionMode.FullscreenMain:
            {
                if (primary is not null) _window.Position = primary.Bounds.TopLeft;
                _window.WindowState = WindowState.FullScreen;
                break;
            }
            case ProjectionMode.WindowedMain:
            {
                // Restore to normal — Windows remembers the previous windowed size.
                if (_window.WindowState != WindowState.Normal)
                {
                    _window.Width  = 640;
                    _window.Height = 360;
                    _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                _window.WindowState = WindowState.Normal;
                break;
            }
            default: // Auto
            {
                if (secondary is not null)
                {
                    _window.Position    = secondary.Bounds.TopLeft;
                    _window.WindowState = WindowState.FullScreen;
                }
                else
                {
                    _window.Width       = 640;
                    _window.Height      = 360;
                    _window.WindowState = WindowState.Normal;
                    _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                break;
            }
        }
    }

    private void OnWindowClosed(object? sender, EventArgs _)
    {
        _window = null;
        IsProjecting = false;
        IsProjectingChanged?.Invoke(false);
    }
}
