using System.Collections.ObjectModel;
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

    public State Model { get; }
    public string Name => Model.Name;

    /// <summary>All available playback modes, for binding to a ComboBox.</summary>
    public static IReadOnlyList<PlaybackMode> AllPlaybackModes { get; } =
        Enum.GetValues<PlaybackMode>();

    // ── rename ─────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isRenaming;
    [ObservableProperty] private string _renameInput = string.Empty;

    // ── playback mode ───────────────────────────────────────────────────────
    [ObservableProperty] private PlaybackMode _playbackMode;

    // ── clip assignment ────────────────────────────────────────────────────
    [ObservableProperty] private bool  _isClipPickerOpen;
    [ObservableProperty] private Clip? _clipToAssign;

    public ObservableCollection<ClipItemViewModel> AssignedClips { get; } = new();

    public IEnumerable<Clip> UnassignedClips =>
        _allClips.Where(c => AssignedClips.All(ac => ac.Model.Id != c.Id));

    public StateItemViewModel(
        State model,
        IReadOnlyList<Clip> allClips,
        Func<StateItemViewModel, Task> onSaveRename,
        Action<StateItemViewModel> onRemove)
    {
        Model          = model;
        _allClips      = allClips;
        _onSaveRename  = onSaveRename;
        _onRemove      = onRemove;
        _playbackMode  = model.PlaybackMode;

        foreach (var clip in model.Clips)
            AssignedClips.Add(new ClipItemViewModel(clip, Unassign));

        AssignedClips.CollectionChanged += (_, _) =>
            OnPropertyChanged(nameof(UnassignedClips));
    }

    partial void OnPlaybackModeChanged(PlaybackMode value)
    {
        Model.PlaybackMode = value;
        _ = _onSaveRename(this);
    }

    // ── rename commands ────────────────────────────────────────────────────

    [RelayCommand]
    private void StartRename()
    {
        RenameInput = Model.Name;
        IsRenaming  = true;
    }

    [RelayCommand]
    private async Task SaveRenameAsync()
    {
        if (string.IsNullOrWhiteSpace(RenameInput)) return;
        Model.Name = RenameInput.Trim();
        await _onSaveRename(this);
        OnPropertyChanged(nameof(Name));
        IsRenaming = false;
    }

    [RelayCommand]
    private void CancelRename()
    {
        IsRenaming  = false;
        RenameInput = string.Empty;
    }

    [RelayCommand]
    private void Remove() => _onRemove(this);

    // ── clip commands ──────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleClipPicker()
    {
        IsClipPickerOpen = !IsClipPickerOpen;
        ClipToAssign     = null;
    }

    [RelayCommand]
    private void AssignClip()
    {
        if (ClipToAssign is null) return;
        if (AssignedClips.Any(c => c.Model.Id == ClipToAssign.Id)) return;

        Model.Clips.Add(ClipToAssign);
        AssignedClips.Add(new ClipItemViewModel(ClipToAssign, Unassign));

        ClipToAssign     = null;
        IsClipPickerOpen = false;
    }

    private void Unassign(ClipItemViewModel item)
    {
        Model.Clips.Remove(item.Model);
        AssignedClips.Remove(item);
    }
}
