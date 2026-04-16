using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Media.Imaging;
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
    private readonly Action                _goBack;
    private IReadOnlyList<Clip>            _allClips = [];

    public Collection Model { get; }

    // ── header display ─────────────────────────────────────────────────────
    public string CollectionName        => Model.Name;
    public string CollectionDescription => Model.Description;

    // ── cover image ────────────────────────────────────────────────────────
    [ObservableProperty] private Bitmap? _coverImage;

    // ── inline name editing ────────────────────────────────────────────────
    [ObservableProperty] private bool   _isEditingName;
    [ObservableProperty] private string _editName = string.Empty;

    // ── inline description editing ─────────────────────────────────────────
    [ObservableProperty] private bool   _isEditingDescription;
    [ObservableProperty] private string _editDescription = string.Empty;

    // ── add state ──────────────────────────────────────────────────────────
    [ObservableProperty] private string _newStateName  = string.Empty;
    [ObservableProperty] private bool   _isAddingState = false;

    // ── clip import ────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImportedClips))]
    private int _importedClipCount;
    public bool HasImportedClips => ImportedClipCount > 0;

    // ── state edit modal ───────────────────────────────────────────────────
    private StateItemViewModel? _editingState;
    public  StateItemViewModel? EditingState
    {
        get => _editingState;
        private set
        {
            SetProperty(ref _editingState, value);
            OnPropertyChanged(nameof(IsStateEditOpen));
        }
    }
    public bool IsStateEditOpen => EditingState is not null;

    [ObservableProperty] private string _stateEditName  = string.Empty;
    [ObservableProperty] private string _stateEditColor = "#FF4444";

    public static IReadOnlyList<string> PresetColors { get; } =
    [
        "#FF3D3D", "#FF7A00", "#FFCC00", "#33CC66",
        "#00CCEE", "#3388FF", "#9966FF", "#FF44AA",
        "#FFFFFF", "#888888",
    ];

    public ObservableCollection<StateItemViewModel> States { get; } = new();

    public CollectionDetailViewModel(
        Collection            model,
        ICollectionRepository repository,
        Action                goBack)
    {
        Model       = model;
        _repository = repository;
        _goBack     = goBack;

        if (!string.IsNullOrEmpty(model.CoverImagePath) && File.Exists(model.CoverImagePath))
            _coverImage = new Bitmap(model.CoverImagePath);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var clipRepo = App.Services.GetRequiredService<IClipRepository>();
        _allClips    = await clipRepo.GetAllAsync();

        States.Clear();
        foreach (var state in Model.States)
            States.Add(MakeStateItem(state));
    }

    // ── back navigation ────────────────────────────────────────────────────

    [RelayCommand]
    private void GoBack() => _goBack();

    // ── cover image (direct pick + save) ──────────────────────────────────

    [RelayCommand]
    private async Task PickCoverImageDirectAsync()
    {
        var filePicker = App.Services.GetRequiredService<IFilePickerService>();
        var path = await filePicker.PickImageAsync();
        if (path is null) return;

        // Copy to app-owned storage so the cover survives if the original is deleted.
        var coversDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Phrazie", "covers");
        Directory.CreateDirectory(coversDir);

        var ext      = Path.GetExtension(path);
        var destPath = Path.Combine(coversDir, $"{Model.Id}{ext}");
        File.Copy(path, destPath, overwrite: true);

        Model.CoverImagePath = destPath;
        CoverImage?.Dispose();
        CoverImage = new Bitmap(destPath);
        await _repository.UpdateAsync(Model);
    }

    // ── inline name editing ────────────────────────────────────────────────

    [RelayCommand]
    private void StartEditName()
    {
        EditName       = Model.Name;
        IsEditingName  = true;
    }

    [RelayCommand]
    private async Task CommitNameAsync()
    {
        if (!IsEditingName) return;
        IsEditingName = false;          // flip immediately — guards against double-fire
        var trimmed = EditName.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
            Model.Name = trimmed;
        await _repository.UpdateAsync(Model);
        OnPropertyChanged(nameof(CollectionName));
    }

    [RelayCommand]
    private void CancelEditName()
    {
        IsEditingName = false;
    }

    // ── inline description editing ─────────────────────────────────────────

    [RelayCommand]
    private void StartEditDescription()
    {
        EditDescription       = Model.Description;
        IsEditingDescription  = true;
    }

    [RelayCommand]
    private async Task CommitDescriptionAsync()
    {
        if (!IsEditingDescription) return;
        IsEditingDescription = false;   // flip immediately — guards against double-fire
        Model.Description = EditDescription.Trim();
        await _repository.UpdateAsync(Model);
        OnPropertyChanged(nameof(CollectionDescription));
    }

    [RelayCommand]
    private void CancelEditDescription()
    {
        IsEditingDescription = false;
    }

    // ── delete collection ──────────────────────────────────────────────────

    [RelayCommand]
    private async Task DeleteCollectionAsync()
    {
        await _repository.DeleteAsync(Model.Id);
        _goBack();
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
                DisplayName = System.IO.Path.GetFileNameWithoutExtension(path),
                Duration    = TimeSpan.Zero
            };
            await clipRepo.AddAsync(clip);
            added++;
        }

        ImportedClipCount = added;
        await LoadAsync();
    }

    // ── state management ───────────────────────────────────────────────────

    [RelayCommand]
    private void ShowAddStateInput()
    {
        NewStateName  = string.Empty;
        IsAddingState = true;
    }

    [RelayCommand]
    private void CancelAddState()
    {
        NewStateName  = string.Empty;
        IsAddingState = false;
    }

    [RelayCommand]
    private async Task AddStateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewStateName)) return;

        var state = new State { Name = NewStateName.Trim(), Color = "#9966FF" };
        Model.States.Add(state);
        States.Add(MakeStateItem(state));
        await _repository.UpdateAsync(Model);
        NewStateName  = string.Empty;
        IsAddingState = false;
    }

    public void MoveState(StateItemViewModel source, StateItemViewModel target)
    {
        int fromIdx = States.IndexOf(source);
        int toIdx   = States.IndexOf(target);
        if (fromIdx < 0 || toIdx < 0 || fromIdx == toIdx) return;

        States.Move(fromIdx, toIdx);

        var modelItem = Model.States[fromIdx];
        Model.States.RemoveAt(fromIdx);
        Model.States.Insert(toIdx, modelItem);

        _ = _repository.UpdateAsync(Model);
    }

    // ── state edit modal ───────────────────────────────────────────────────

    internal void BeginEditState(StateItemViewModel vm)
    {
        StateEditName  = vm.Name;
        StateEditColor = vm.Color;
        EditingState   = vm;
    }

    [RelayCommand]
    private async Task SaveStateEditAsync()
    {
        if (EditingState is null) return;
        EditingState.ApplyEdit(StateEditName, StateEditColor);
        await _repository.UpdateAsync(Model);
        EditingState = null;
    }

    [RelayCommand]
    private void CancelStateEdit()
    {
        EditingState = null;
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private StateItemViewModel MakeStateItem(State state) =>
        new(state, _allClips,
            onSaveRename: async _ => await _repository.UpdateAsync(Model),
            onRemove: item =>
            {
                Model.States.Remove(item.Model);
                States.Remove(item);
                _ = _repository.UpdateAsync(Model);
            },
            onEdit: item => BeginEditState(item));
}
