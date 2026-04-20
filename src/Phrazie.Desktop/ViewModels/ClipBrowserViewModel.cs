using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

// ── Level content wrappers (drive TransitioningContentControl) ────────────

public sealed class BrowserCollectionsContent
{
    public IReadOnlyList<Collection> Items      { get; init; } = [];
    public ICommand                  DrillIn    { get; init; } = null!;
}

public sealed class BrowserStatesContent
{
    public Collection                Collection { get; init; } = null!;
    public IReadOnlyList<State>      Items      { get; init; } = [];
    public ICommand                  DrillIn    { get; init; } = null!;
}

public sealed class BrowserClipsContent
{
    public Collection                Collection { get; init; } = null!;
    public State                     State      { get; init; } = null!;
    public IReadOnlyList<Clip>       Items      { get; init; } = [];
    public ICommand                  Select     { get; init; } = null!;
}

// ── Main VM ───────────────────────────────────────────────────────────────

public partial class ClipBrowserViewModel : ViewModelBase
{
    private readonly ICollectionRepository _collections;
    private readonly Action<Clip>          _onSelect;
    private readonly Action                _onClose;

    private readonly Stack<object> _history = new();

    [ObservableProperty] private string  _breadcrumb   = "Library";
    [ObservableProperty] private bool    _canGoBack    = false;
    [ObservableProperty] private object? _currentContent;
    [ObservableProperty] private bool    _isLoading    = false;

    public ClipBrowserViewModel(
        ICollectionRepository collections,
        Action<Clip> onSelect,
        Action onClose)
    {
        _collections = collections;
        _onSelect    = onSelect;
        _onClose     = onClose;

        _ = LoadCollectionsAsync();
    }

    // ── Navigation ────────────────────────────────────────────────────────

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

        var clips = state.Clips.Where(c => c.IsEnabled).ToList();
        CurrentContent = new BrowserClipsContent
        {
            Collection = collection,
            State      = state,
            Items      = clips,
            Select     = SelectClipCommand,
        };
        RefreshBreadcrumb();
    }

    [RelayCommand]
    private void SelectClip(Clip clip)
    {
        _onSelect(clip);
        _onClose();
    }

    [RelayCommand]
    private void Close() => _onClose();

    // ── Internal ──────────────────────────────────────────────────────────

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
            BrowserCollectionsContent                       => "Library",
            BrowserStatesContent  s                        => $"Library  ›  {s.Collection.Name}",
            BrowserClipsContent   c                        => $"Library  ›  {c.Collection.Name}  ›  {c.State.Name}",
            _                                              => "Library",
        };
    }
}
