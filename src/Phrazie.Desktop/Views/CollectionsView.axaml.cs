using Avalonia.Controls;
using Avalonia.Input;
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

        // Single-click anywhere on a card opens the collection management page
        if (e.Source is Control { DataContext: CollectionCardViewModel card })
            card.ManageCommand.Execute(null);
    }
}
