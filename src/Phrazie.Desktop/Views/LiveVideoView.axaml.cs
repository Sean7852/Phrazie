using Avalonia.Controls;
using Phrazie.Core.Enums;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class LiveVideoView : UserControl
{
    private LivePerformanceViewModel? _vm;

    public LiveVideoView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => WireVideoOutput();
    }

    private void WireVideoOutput()
    {
        if (_vm is not null)
        {
            _vm.OutTransitionRequired -= OnOutTransitionRequired;
            _vm.InTransitionStarted   -= OnInTransitionStarted;
        }

        _vm = DataContext as LivePerformanceViewModel;
        VideoOutput.Attach(_vm?.VideoService);

        if (_vm is not null)
        {
            _vm.OutTransitionRequired += OnOutTransitionRequired;
            _vm.InTransitionStarted   += OnInTransitionStarted;
        }
    }

    // Awaited by the VM — blocks the clip switch until the out effect finishes
    private Task OnOutTransitionRequired(TransitionType type, double duration)
        => VideoOutput.PlayTransitionAsync(type, duration, outgoing: true);

    // Fire-and-forget — plays the in effect as the new clip starts
    private void OnInTransitionStarted(TransitionType type, double duration)
        => _ = VideoOutput.PlayTransitionAsync(type, duration);
}
