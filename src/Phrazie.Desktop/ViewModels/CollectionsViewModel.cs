using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;

namespace Phrazie.Desktop.ViewModels;

public partial class CollectionsViewModel : ViewModelBase
{
    private readonly ICollectionRepository _repository;
    private readonly ISessionService _session;
    private readonly IFilePickerService _filePicker;
    private readonly Action<Collection> _onManage;

    public ObservableCollection<CollectionCardViewModel> Collections { get; } = new();

    [ObservableProperty]
    private string _newCollectionName = string.Empty;

    public CollectionsViewModel(
        ICollectionRepository repository,
        ISessionService session,
        IFilePickerService filePicker,
        Action<Collection> onManage)
    {
        _repository = repository;
        _session    = session;
        _filePicker = filePicker;
        _onManage   = onManage;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task CreateCollectionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCollectionName)) return;
        var collection = await _repository.CreateAsync(NewCollectionName);
        Collections.Add(MakeCard(collection));
        NewCollectionName = string.Empty;
    }

    private async Task LoadAsync()
    {
        var all = await _repository.GetAllAsync();
        Collections.Clear();
        foreach (var c in all)
            Collections.Add(MakeCard(c));
    }

    private CollectionCardViewModel MakeCard(Collection c) =>
        new(c, _filePicker, _repository, _session,
            onDelete: card => Collections.Remove(card),
            onManage: _onManage);
}
