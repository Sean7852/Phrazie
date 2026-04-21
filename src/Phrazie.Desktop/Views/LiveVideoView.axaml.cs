using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Phrazie.Desktop.Controls;
using Phrazie.Desktop.Services;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class LiveVideoView : UserControl
{
    public LiveVideoView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => WireMediaPlayer();
        Loaded             += OnLoaded;
    }

    private Action<IntPtr>? _handleAvailableHandler;

    private void WireMediaPlayer()
    {
        if (DataContext is not LivePerformanceViewModel vm) return;

        VideoOutput.MediaPlayer = vm.MediaPlayer;

        if (vm.VideoService is not VideoPlaybackService vps) return;

        vps.HwndProvider = () => VideoOutput.NativeHandle;

        // Remove any previous subscription before adding a new one — DataContextChanged
        // can fire more than once if the parent re-binds.
        if (_handleAvailableHandler is not null)
            VideoOutput.HandleAvailable -= _handleAvailableHandler;

        _handleAvailableHandler = hwnd =>
        {
            Debug.WriteLine($"[LiveVideoView] HandleAvailable 0x{hwnd:X}");
            if (vps.DeferredClip is not null)
                _ = vps.PlayDeferredAsync(hwnd);
            else
                vm.TriggerAutoPlay();
        };

        VideoOutput.HandleAvailable += _handleAvailableHandler;
    }

    // Loaded fires after layout but BEFORE the render pass that calls CreateNativeControlCore.
    // TriggerAutoPlay is now driven by VideoOutput.HandleAvailable instead.
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[LiveVideoView] Loaded — NativeHandle=0x{VideoOutput.NativeHandle:X}  (playback deferred until HandleAvailable)");
    }
}
