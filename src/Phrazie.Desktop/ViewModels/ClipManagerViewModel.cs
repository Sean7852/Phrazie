using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Clip Manager window.
/// Owns the list of clips for one state and handles drag-drop import.
/// </summary>
public partial class ClipManagerViewModel : ObservableObject
{
    private readonly State    _state;
    private readonly Func<Task> _onSave;

    public string StateName => _state.Name;

    public ObservableCollection<ManagedClipViewModel> Clips { get; } = new();

    public ClipManagerViewModel(State state, Func<Task> onSave)
    {
        _state  = state;
        _onSave = onSave;

        foreach (var clip in state.Clips)
            Clips.Add(MakeVm(clip));
    }

    // ── drag-drop / import ─────────────────────────────────────────────────

    /// <summary>Called by the window's drop handler with file paths.</summary>
    public async Task AddClipsFromPathsAsync(IEnumerable<string> paths)
    {
        var clipRepo = App.Services.GetRequiredService<IClipRepository>();
        foreach (var path in paths)
        {
            // Skip if already assigned to this state
            if (_state.Clips.Any(c => c.FilePath == path))
                continue;

            var clip = new Clip
            {
                FilePath    = path,
                DisplayName = Path.GetFileNameWithoutExtension(path),
                Duration    = TimeSpan.Zero,
                IsEnabled   = true,
            };
            await clipRepo.AddAsync(clip);
            _state.Clips.Add(clip);
            Clips.Add(MakeVm(clip));
        }
        await _onSave();
    }

    // ── private ────────────────────────────────────────────────────────────

    private ManagedClipViewModel MakeVm(Clip clip) =>
        new(clip, DeleteClip, () => _ = PersistChangedAsync());

    private void DeleteClip(ManagedClipViewModel vm)
    {
        _state.Clips.Remove(vm.Model);
        Clips.Remove(vm);
        _ = _onSave();
    }

    private async Task PersistChangedAsync()
    {
        var clipRepo = App.Services.GetRequiredService<IClipRepository>();
        // Update is_enabled for all clips
        foreach (var vm in Clips)
            await clipRepo.UpdateAsync(vm.Model);
        await _onSave();
    }
}
