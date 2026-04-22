using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Phrazie.Desktop.ViewModels;

public partial class TransitionOptionViewModel : ObservableObject
{
    public string Name { get; }

    [ObservableProperty] private bool _isArmed;

    public TransitionOptionViewModel(string name, bool isArmed = false)
    {
        Name    = name;
        IsArmed = isArmed;
    }

    [RelayCommand]
    private void Toggle() => IsArmed = !IsArmed;
}
