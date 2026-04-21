using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

// ── Clip item wrapper carrying selection state ────────────────────────────────

public partial class BrowserClipItem : ObservableObject
{
    public Clip   Model          { get; }
    public string CollectionName { get; }
    public string StateName      { get; }
    public string StateColor     { get; }

    [ObservableProperty] private bool _isSelected;

    public BrowserClipItem(Clip clip, string collectionName, string stateName, string stateColor)
    {
        Model          = clip;
        CollectionName = collectionName;
        StateName      = stateName;
        StateColor     = stateColor;
    }
}

// ── Level content wrappers (drive TransitioningContentControl) ────────────────

public sealed class BrowserCollectionsContent
{
    public IReadOnlyList<Collection> Items   { get; init; } = [];
    public ICommand                  DrillIn { get; init; } = null!;
}

public sealed class BrowserStatesContent
{
    public Collection           Collection { get; init; } = null!;
    public IReadOnlyList<State> Items      { get; init; } = [];
    public ICommand             DrillIn    { get; init; } = null!;
}

public sealed class BrowserClipsContent
{
    public Collection            Collection { get; init; } = null!;
    public State                 State      { get; init; } = null!;
    public List<BrowserClipItem> Items      { get; init; } = [];
}

// ── Main VM ───────────────────────────────────────────────────────────────────

public partial class ClipBrowserViewModel : ViewModelBase
{
    private readonly ICollectionRepository                   _collections;
    private readonly Action<IReadOnlyList<SelectedClipInfo>> _onSelect;
    private readonly Action                                  _onClose;

    private readonly Stack<object> _history = new();
    private int _lastClickedIndex = -1;

    [ObservableProperty] private string  _breadcrumb     = "Library";
    [ObservableProperty] private bool    _canGoBack      = false;
    [ObservableProperty] private object? _currentContent;
    [ObservableProperty] private bool    _isLoading      = false;
    [ObservableProperty] private int     _selectedCount  = 0;
    [ObservableProperty] private bool    _isOnClipsLevel = false;

    public bool HasSelection => SelectedCount > 0;

    partial void OnSelectedCountChanged(int value)
        => OnPropertyChanged(nameof(HasSelection));

    partial void OnCurrentContentChanged(object? value)
    {
        IsOnClipsLevel    = value is BrowserClipsContent;
        SelectedCount     = 0;
        _lastClickedIndex = -1;
    }

    public ClipBrowserViewModel(
        ICollectionRepository collections,
        Action<IReadOnlyList<SelectedClipInfo>> onSelect,
        Action onClose)
    {
        _collections = collections;
        _onSelect    = onSelect;
        _onClose     = onClose;

        _ = LoadCollectionsAsync();
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Back()
    {
        if (_history.Count == 0) return;
        CurrentContent = _history.Pop();
        RefreshBreadcrumb();
        CanGoBack = _history.Count > 0;
    }

    [RelayCommand]
    private async Task DrillIntoCollectionAsync(Collection collection)
    {
        _history.Push(CurrentContent!);
        CanGoBack = true;

        var states = collection.States.ToList();
        CurrentContent = new BrowserStatesContent
        {
            Collection = collection,
            Items      = states,
            DrillIn    = NavigateToStateCommand,
        };
        RefreshBreadcrumb();
    }

    [RelayCommand]
    private void NavigateToState(State state)
    {
        var collection = (CurrentContent as BrowserStatesContent)?.Collection ?? new Collection();
        _history.Push(CurrentContent!);
        CanGoBack = true;

        var clips = state.Clips
            .Where(c => c.IsEnabled)
            .Select(c => new BrowserClipItem(c, collection.Name, state.Name, state.Color))
            .ToList();

        CurrentContent = new BrowserClipsContent
        {
            Collection = collection,
            State      = state,
            Items      = clips,
        };
        RefreshBreadcrumb();
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    public void HandleClipClick(BrowserClipItem item, bool shift, bool ctrl)
    {
        var content = CurrentContent as BrowserClipsContent;
        if (content is null) return;

        int idx = content.Items.IndexOf(item);

        if (shift && _lastClickedIndex >= 0 && idx >= 0)
        {
            int from = Math.Min(_lastClickedIndex, idx);
            int to   = Math.Max(_lastClickedIndex, idx);
            for (int i = from; i <= to; i++)
                content.Items[i].IsSelected = true;
        }
        else
        {
            item.IsSelected   = !item.IsSelected;
            _lastClickedIndex = idx;
        }

        NotifySelectionChanged();
    }

    internal void NotifySelectionChanged()
    {
        var content = CurrentContent as BrowserClipsContent;
        SelectedCount = content?.Items.Count(i => i.IsSelected) ?? 0;
    }

    [RelayCommand]
    private void ConfirmSelection()
    {
        var content = CurrentContent as BrowserClipsContent;
        if (content is null) return;

        var selected = content.Items
            .Where(i => i.IsSelected)
            .Select(i => new SelectedClipInfo(i.Model, i.CollectionName, i.StateName, i.StateColor))
            .ToList();

        if (selected.Count == 0) return;
        _onSelect(selected);
        _onClose();
    }

    [RelayCommand]
    private void Close() => _onClose();

    // ── Internal ──────────────────────────────────────────────────────────────

    private async Task LoadCollectionsAsync()
    {
        IsLoading = true;
        var all = await _collections.GetAllAsync();
        CurrentContent = new BrowserCollectionsContent
        {
            Items   = all,
            DrillIn = DrillIntoCollectionCommand,
        };
        IsLoading = false;
    }

    private void RefreshBreadcrumb()
    {
        Breadcrumb = CurrentContent switch
        {
            BrowserCollectionsContent               => "Library",
            BrowserStatesContent  s                 => $"Library  ›  {s.Collection.Name}",
            BrowserClipsContent   c                 => $"Library  ›  {c.Collection.Name}  ›  {c.State.Name}",
            _                                       => "Library",
        };
    }
}
