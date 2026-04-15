using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

public sealed class ClipItemViewModel
{
    private readonly Action<ClipItemViewModel> _onUnassign;

    public Clip Model { get; }
    public string DisplayName => Model.DisplayName;
    public string Duration    => Model.Duration == TimeSpan.Zero
        ? "—"
        : Model.Duration.ToString(@"m\:ss");

    public IRelayCommand UnassignCommand { get; }

    public ClipItemViewModel(Clip model, Action<ClipItemViewModel> onUnassign)
    {
        Model       = model;
        _onUnassign = onUnassign;
        UnassignCommand = new RelayCommand(() => _onUnassign(this));
    }
}
