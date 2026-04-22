using Avalonia.Controls;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class LiveVideoView : UserControl
{
    public LiveVideoView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => WireVideoOutput();
    }

    private void WireVideoOutput()
    {
        var vps = (DataContext as LivePerformanceViewModel)?.VideoService;
        VideoOutput.Attach(vps);
    }
}
