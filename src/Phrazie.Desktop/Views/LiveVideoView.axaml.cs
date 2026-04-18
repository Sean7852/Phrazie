using Avalonia.Controls;
using Phrazie.Desktop.Controls;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class LiveVideoView : UserControl
{
    public LiveVideoView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => WireMediaPlayer();
    }

    private void WireMediaPlayer()
    {
        if (DataContext is LivePerformanceViewModel vm)
            VideoOutput.MediaPlayer = vm.MediaPlayer;
    }
}
