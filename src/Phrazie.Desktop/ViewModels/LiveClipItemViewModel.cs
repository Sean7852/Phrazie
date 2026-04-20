using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

public partial class LiveClipItemViewModel : ViewModelBase
{
    public Clip Model { get; }

    private readonly Action<LiveClipItemViewModel> _requestRemove;

    [ObservableProperty] private bool   _isActive;
    [ObservableProperty] private double _opacity = 1.0;

    public string Name           => Model.DisplayName;
    public string CollectionName { get; }
    public string StateName      { get; }
    public string Duration       => Model.Duration > TimeSpan.Zero
        ? Model.Duration.ToString(@"m\:ss")
        : "—";

    public LiveClipItemViewModel(Clip model, Action<LiveClipItemViewModel> requestRemove,
                                  string collectionName = "—", string stateName = "—")
    {
        Model          = model;
        _requestRemove = requestRemove;
        CollectionName = collectionName;
        StateName      = stateName;
    }

    [RelayCommand]
    private void Remove() => _requestRemove(this);
}
