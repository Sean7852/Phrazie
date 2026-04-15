using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;

namespace Phrazie.Desktop.ViewModels;

public partial class CollectionDetailViewModel : ViewModelBase
{
    private readonly ICollectionRepository _repository;
    private readonly Action _goBack;
    private IReadOnlyList<Clip> _allClips = [];

    public Collection Model { get; }

    public ObservableCollection<StateItemViewModel> States { get; } = new();

    // ── collection rename ──────────────────────────────────────────────────
    [ObservableProperty] private bool   _isRenamingCollection;
    [ObservableProperty] private string _collectionRenameInput = string.Empty;

    // ── add state ──────────────────────────────────────────────────────────
    [ObservableProperty] private string _newStateName = string.Empty;

    // ── clip import ────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImportedClips))]
    private int _importedClipCount;

    public bool HasImportedClips => ImportedClipCount > 0;

    public CollectionDetailViewModel(
        Collection model,
        ICollectionRepository repository,
        Action goBack)
    {
        Model       = model;
        _repository = repository;
        _goBack     = goBack;

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        // Repos/services are resolved from DI via App.Services to keep constructor simple
        var clipRepo = App.Services.GetRequiredService<IClipRepository>();
        _allClips    = await clipRepo.GetAllAsync();

        States.Clear();
        foreach (var state in Model.States)
            States.Add(MakeStateItem(state));
    }

    // ── clip import ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ImportClipsAsync()
    {
        var filePicker = App.Services.GetRequiredService<IFilePickerService>();
        var clipRepo   = App.Services.GetRequiredService<IClipRepository>();

        var paths = await filePicker.PickVideoFilesAsync();
        if (paths.Count == 0) return;

        int added = 0;
        foreach (var path in paths)
        {
            var clip = new Clip
            {
                FilePath    = path,
                DisplayName = Path.GetFileNameWithoutExtension(path),
                Duration    = TimeSpan.Zero   // real duration requires LibVLC (Phase 3)
            };
            await clipRepo.AddAsync(clip);
            added++;
        }

        ImportedClipCount = added;
        // Reload so state pickers see the new clips
        await LoadAsync();
    }

    // ── back navigation ────────────────────────────────────────────────────

    [RelayCommand]
    private void GoBack() => _goBack();

    // ── collection rename ──────────────────────────────────────────────────

    [RelayCommand]
    private void StartRenameCollection()
    {
        CollectionRenameInput    = Model.Name;
        IsRenamingCollection     = true;
    }

    [RelayCommand]
    private async Task SaveCollectionRenameAsync()
    {
        if (string.IsNullOrWhiteSpace(CollectionRenameInput)) return;
        Model.Name = CollectionRenameInput.Trim();
        await _repository.UpdateAsync(Model);
        OnPropertyChanged(nameof(Model));
        IsRenamingCollection = false;
    }

    [RelayCommand]
    private void CancelCollectionRename() => IsRenamingCollection = false;

    // ── state management ───────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddStateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewStateName)) return;

        var state = new State { Name = NewStateName.Trim() };
        Model.States.Add(state);
        States.Add(MakeStateItem(state));
        await _repository.UpdateAsync(Model);
        NewStateName = string.Empty;
    }

    private StateItemViewModel MakeStateItem(State state) =>
        new(state, _allClips,
            onSaveRename: async _ => await _repository.UpdateAsync(Model),
            onRemove: item =>
            {
                Model.States.Remove(item.Model);
                States.Remove(item);
                _ = _repository.UpdateAsync(Model);
            });
}
