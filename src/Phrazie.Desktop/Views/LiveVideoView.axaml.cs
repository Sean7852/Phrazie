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
            _vm.TransitionStarted -= OnTransitionStarted;

        _vm = DataContext as LivePerformanceViewModel;
        VideoOutput.Attach(_vm?.VideoService);

        if (_vm is not null)
            _vm.TransitionStarted += OnTransitionStarted;
    }

    private void OnTransitionStarted(TransitionType type, double duration)
        => _ = VideoOutput.PlayTransitionAsync(type, duration);
}
