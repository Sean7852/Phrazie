using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;

namespace Phrazie.Desktop.ViewModels;

public partial class StateItemViewModel : ObservableObject
{
    private readonly IReadOnlyList<Clip> _allClips;
    private readonly Func<StateItemViewModel, Task> _onSaveRename;
    private readonly Action<StateItemViewModel> _onRemove;
    private readonly Action<StateItemViewModel> _onEdit;

    public State Model { get; }

    public string Name => Model.Name;

    public string Color => Model.Color;

    public ISolidColorBrush ColorBrush =>
        new SolidColorBrush(Avalonia.Media.Color.Parse(Model.Color));

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

    // ── clip import ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ImportClipsAsync()
    {
        var filePicker = App.Services.GetRequiredService<IFilePickerService>();
        var paths = await filePicker.PickVideoFilesAsync();
        if (paths.Count == 0) return;
        await AddClipsFromPathsAsync(paths);
    }

    /// <summary>Called by the view's file drag-drop handler to import clips.</summary>
    public async Task AddClipsFromPathsAsync(IEnumerable<string> paths)
    {
        var clipRepo = App.Services.GetRequiredService<IClipRepository>();
        foreach (var path in paths)
        {
            var clip = new Clip
            {
                FilePath    = path,
                DisplayName = Path.GetFileNameWithoutExtension(path),
                Duration    = TimeSpan.Zero,
            };
            await clipRepo.AddAsync(clip);
            Model.Clips.Add(clip);
            AssignedClips.Add(new ClipItemViewModel(clip, Unassign));
        }
        await _onSaveRename(this);
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private void Unassign(ClipItemViewModel item)
    {
        Model.Clips.Remove(item.Model);
        AssignedClips.Remove(item);
        _ = _onSaveRename(this);
    }

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
