using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Threading;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class ClipQueueView : UserControl
{
    public ClipQueueView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Dispatcher.UIThread.Post(AnimateEntrance, DispatcherPriority.Loaded);
    }

    private async void AnimateEntrance()
    {
        var list = this.FindControl<ItemsControl>("ClipList");
        if (list is null) return;

        // Hide all items first
        for (int i = 0; i < list.ItemCount; i++)
        {
            if (list.ContainerFromIndex(i) is Control c)
                c.Opacity = 0;
        }

        // Fade each in with a stagger
        for (int i = 0; i < list.ItemCount; i++)
        {
            await Task.Delay(55);
            if (list.ContainerFromIndex(i) is not Control c) continue;

            c.Transitions ??= new Transitions();
            if (!c.Transitions.OfType<DoubleTransition>().Any(t => t.Property == OpacityProperty))
                c.Transitions.Add(new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(180) });

            c.Opacity = 1;
        }
    }
}
