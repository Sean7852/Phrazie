using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Phrazie.Desktop.ViewModels;

namespace Phrazie.Desktop.Views;

public partial class CollectionsView : UserControl
{
    public CollectionsView()
    {
        InitializeComponent();
    }

    protected override void OnTapped(TappedEventArgs e)
    {
        base.OnTapped(e);

        // If the tap landed inside a Button (play or manage), let the button handle it.
        if (e.Source is Visual src && src.FindAncestorOfType<Button>() is not null)
            return;

        // Tap on the card body → open collection management
        if (e.Source is Control { DataContext: CollectionCardViewModel card })
            card.ManageCommand.Execute(null);
    }
}
