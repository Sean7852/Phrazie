using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

public partial class CollectionsViewModel : ViewModelBase
{
    private readonly ICollectionRepository _repository;
    private readonly ISessionService _session;

    public ObservableCollection<Collection> Collections { get; } = new();

    [ObservableProperty]
    private Collection? _selectedCollection;

    [ObservableProperty]
    private string _newCollectionName = string.Empty;

    public CollectionsViewModel(ICollectionRepository repository, ISessionService session)
    {
        _repository = repository;
        _session    = session;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task CreateCollectionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCollectionName)) return;
        var collection = await _repository.CreateAsync(NewCollectionName);
        Collections.Add(collection);
        NewCollectionName = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteCollectionAsync(Collection collection)
    {
        await _repository.DeleteAsync(collection.Id);
        Collections.Remove(collection);
        if (SelectedCollection == collection)
            SelectedCollection = null;
    }

    [RelayCommand]
    private async Task SelectCollectionAsync(Collection collection)
    {
        SelectedCollection = collection;
        await _session.SetActiveCollectionAsync(collection);
    }

    private async Task LoadAsync()
    {
        var all = await _repository.GetAllAsync();
        Collections.Clear();
        foreach (var c in all)
            Collections.Add(c);
    }
}
