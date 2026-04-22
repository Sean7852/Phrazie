using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Enums;

namespace Phrazie.Desktop.ViewModels;

public partial class TransitionOptionViewModel : ObservableObject
{
    public TransitionType Type { get; }
    public string         Name { get; }

    [ObservableProperty] private bool _isArmed;

    public TransitionOptionViewModel(TransitionType type, string name, bool isArmed = false)
    {
        Type    = type;
        Name    = name;
        IsArmed = isArmed;
    }

    [RelayCommand]
    private void Toggle() => IsArmed = !IsArmed;
}
