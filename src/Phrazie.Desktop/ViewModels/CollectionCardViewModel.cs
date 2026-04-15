using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;
using Phrazie.Desktop.Services;

namespace Phrazie.Desktop.ViewModels;

public partial class CollectionCardViewModel : ViewModelBase
{
    private readonly IFilePickerService _filePicker;
    private readonly ICollectionRepository _repository;
    private readonly ISessionService _session;
    private readonly Action<CollectionCardViewModel> _onDelete;
    private readonly Action<Collection> _onManage;

    public Collection Model { get; }

    // Name is manually notified after rename so the card header updates
    public string Name => Model.Name;

    [ObservableProperty] private bool    _isSettingsOpen;
    [ObservableProperty] private Bitmap? _coverImage;

    // ── inline rename ──────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isRenaming;
    [ObservableProperty] private string _renameInput = string.Empty;

    public CollectionCardViewModel(
        Collection model,
        IFilePickerService filePicker,
        ICollectionRepository repository,
        ISessionService session,
        Action<CollectionCardViewModel> onDelete,
        Action<Collection> onManage)
    {
        Model       = model;
        _filePicker = filePicker;
        _repository = repository;
        _session    = session;
        _onDelete   = onDelete;
        _onManage   = onManage;

        if (!string.IsNullOrEmpty(model.CoverImagePath) && File.Exists(model.CoverImagePath))
            _coverImage = new Bitmap(model.CoverImagePath);
    }

    // ── settings toggle ────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
        if (!IsSettingsOpen) IsRenaming = false;
    }

    // ── select / delete ────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SelectAsync() =>
        await _session.SetActiveCollectionAsync(Model);

    [RelayCommand]
    private async Task DeleteAsync()
    {
        await _repository.DeleteAsync(Model.Id);
        _onDelete(this);
    }

    // ── manage states ──────────────────────────────────────────────────────

    [RelayCommand]
    private void Manage()
    {
        IsSettingsOpen = false;
        _onManage(Model);
    }

    // ── rename ─────────────────────────────────────────────────────────────

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
        await _repository.UpdateAsync(Model);
        OnPropertyChanged(nameof(Name));
        IsRenaming = false;
    }

    [RelayCommand]
    private void CancelRename()
    {
        IsRenaming  = false;
        RenameInput = string.Empty;
    }

    // ── cover image ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task UploadCoverImageAsync()
    {
        var path = await _filePicker.PickImageAsync();
        if (path is null) return;

        Model.CoverImagePath = path;
        CoverImage?.Dispose();
        CoverImage = new Bitmap(path);

        await _repository.UpdateAsync(Model);
        IsSettingsOpen = false;
    }
}
