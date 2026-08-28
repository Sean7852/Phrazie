using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

public partial class CollectionsViewModel : ViewModelBase
{
    private readonly ICollectionRepository _repository;
    private readonly ISessionService       _session;
    private readonly Action<Collection>    _onManage;

    private readonly List<CollectionCardViewModel> _allCards = new();

    public ObservableCollection<CollectionCardViewModel> FilteredCollections { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatsDisplay))]
    private string _newCollectionName = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSortRecent))]
    [NotifyPropertyChangedFor(nameof(IsSortAz))]
    [NotifyPropertyChangedFor(nameof(IsSortSize))]
    private int _sortMode = 0; // 0 = Recent, 1 = A-Z, 2 = Size

    public bool IsSortRecent => SortMode == 0;
    public bool IsSortAz     => SortMode == 1;
    public bool IsSortSize   => SortMode == 2;

    public string StatsDisplay =>
        $"{_allCards.Count} collections · {_allCards.Sum(c => c.ClipCount)} clips";

    partial void OnSearchTextChanged(string _) => ApplyFilter();
    partial void OnSortModeChanged(int _)      => ApplyFilter();

    [RelayCommand] private void SortByRecent() => SortMode = 0;
    [RelayCommand] private void SortByName()   => SortMode = 1;
    [RelayCommand] private void SortBySize()   => SortMode = 2;

    public CollectionsViewModel(
        ICollectionRepository repository,
        ISessionService       session,
        Action<Collection>    onManage)
    {
        _repository = repository;
        _session    = session;
        _onManage   = onManage;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task CreateCollectionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCollectionName)) return;
        var collection = await _repository.CreateAsync(NewCollectionName);
        var card = MakeCard(collection);
        _allCards.Insert(0, card);
        ApplyFilter();
        OnPropertyChanged(nameof(StatsDisplay));
        NewCollectionName = string.Empty;
    }

    private async Task LoadAsync()
    {
        var all = await _repository.GetAllAsync();
        _allCards.Clear();
        foreach (var c in all)
            _allCards.Add(MakeCard(c));
        ApplyFilter();
        OnPropertyChanged(nameof(StatsDisplay));
    }

    private void ApplyFilter()
    {
        IEnumerable<CollectionCardViewModel> view = _allCards;

        if (!string.IsNullOrWhiteSpace(SearchText))
            view = view.Where(c =>
                c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        view = SortMode switch
        {
            1 => view.OrderBy(c => c.Name),
            2 => view.OrderByDescending(c => c.ClipCount),
            _ => view.OrderByDescending(c => c.Model.CreatedAt)
        };

        FilteredCollections.Clear();
        foreach (var card in view)
            FilteredCollections.Add(card);
    }

    private async void DeleteCard(CollectionCardViewModel card)
    {
        await _repository.DeleteAsync(card.Model.Id);
        _allCards.Remove(card);
        ApplyFilter();
        OnPropertyChanged(nameof(StatsDisplay));
    }

    private CollectionCardViewModel MakeCard(Collection c) =>
        new(c, _session, _onManage, DeleteCard);
}
