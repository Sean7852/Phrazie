using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Enums;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

public partial class StateItemViewModel : ObservableObject
{
    private readonly IReadOnlyList<Clip> _allClips;
    private readonly Func<StateItemViewModel, Task> _onSaveRename;
    private readonly Action<StateItemViewModel> _onRemove;
    private readonly Action<StateItemViewModel> _onEdit;

    public State Model { get; }

    public string Name => Model.Name;

    /// <summary>Hex color string (e.g. "#FF3D3D") used for the state indicator swatch.</summary>
    public string Color => Model.Color;

    public ISolidColorBrush ColorBrush =>
        new SolidColorBrush(Avalonia.Media.Color.Parse(Model.Color));

    /// <summary>Five placeholder slots for the clip thumbnail row (MVP simulation).</summary>
    public IEnumerable<int> PlaceholderSlots { get; } = Enumerable.Range(0, 5);

    public static IReadOnlyList<PlaybackMode> AllPlaybackModes { get; } =
        Enum.GetValues<PlaybackMode>();

    [ObservableProperty] private PlaybackMode _playbackMode;

    public ObservableCollection<ClipItemViewModel> AssignedClips { get; } = new();

    public StateItemViewModel(
        State model,
        IReadOnlyList<Clip> allClips,
        Func<StateItemViewModel, Task> onSaveRename,
        Action<StateItemViewModel> onRemove,
        Action<StateItemViewModel> onEdit)
    {
        Model         = model;
        _allClips     = allClips;
        _onSaveRename = onSaveRename;
        _onRemove     = onRemove;
        _onEdit       = onEdit;
        _playbackMode = model.PlaybackMode;

        foreach (var clip in model.Clips)
            AssignedClips.Add(new ClipItemViewModel(clip, Unassign));
    }

    partial void OnPlaybackModeChanged(PlaybackMode value)
    {
        Model.PlaybackMode = value;
        _ = _onSaveRename(this);
    }

    [RelayCommand]
    private void OpenEdit() => _onEdit(this);

    [RelayCommand]
    private void Remove() => _onRemove(this);

    // ── clip support (used internally / future UI) ─────────────────────────

    private void Unassign(ClipItemViewModel item)
    {
        Model.Clips.Remove(item.Model);
        AssignedClips.Remove(item);
    }

    /// <summary>
    /// Apply pending edits from the parent modal and notify the view.
    /// </summary>
    public void ApplyEdit(string newName, string newColor)
    {
        if (!string.IsNullOrWhiteSpace(newName))
            Model.Name  = newName.Trim();
        Model.Color = newColor;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Color));
        OnPropertyChanged(nameof(ColorBrush));
    }
}
