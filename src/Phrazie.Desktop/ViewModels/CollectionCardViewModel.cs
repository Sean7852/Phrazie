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

    public Collection Model { get; }
    public string Name => Model.Name;

    [ObservableProperty] private bool _isSettingsOpen;
    [ObservableProperty] private Bitmap? _coverImage;

    public CollectionCardViewModel(
        Collection model,
        IFilePickerService filePicker,
        ICollectionRepository repository,
        ISessionService session,
        Action<CollectionCardViewModel> onDelete)
    {
        Model       = model;
        _filePicker = filePicker;
        _repository = repository;
        _session    = session;
        _onDelete   = onDelete;

        if (!string.IsNullOrEmpty(model.CoverImagePath) && File.Exists(model.CoverImagePath))
            _coverImage = new Bitmap(model.CoverImagePath);
    }

    [RelayCommand]
    private void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

    [RelayCommand]
    private async Task SelectAsync() =>
        await _session.SetActiveCollectionAsync(Model);

    [RelayCommand]
    private async Task DeleteAsync()
    {
        await _repository.DeleteAsync(Model.Id);
        _onDelete(this);
    }

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
