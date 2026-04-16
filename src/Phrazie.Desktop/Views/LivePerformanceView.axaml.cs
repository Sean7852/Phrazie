using Avalonia.Controls;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class LivePerformanceView : UserControl
{
    public LivePerformanceView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is LivePerformanceViewModel vm)
                VideoOutput.MediaPlayer = vm.MediaPlayer;
        };
    }
}
