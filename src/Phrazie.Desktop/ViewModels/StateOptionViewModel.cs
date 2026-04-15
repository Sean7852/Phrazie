using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

/// <summary>
/// Wraps a <see cref="State"/> for display as a selectable button on the Live screen.
/// IsActive  = currently playing state.
/// IsNext    = chosen as the next transition target.
/// </summary>
public partial class StateOptionViewModel : ObservableObject
{
    private readonly Action<StateOptionViewModel> _onSelect;

    public State Model { get; }
    public string Name => Model.Name;

    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private bool _isNext;

    public StateOptionViewModel(State model, Action<StateOptionViewModel> onSelect)
    {
        Model     = model;
        _onSelect = onSelect;
    }

    [RelayCommand]
    private void Select() => _onSelect(this);
}
