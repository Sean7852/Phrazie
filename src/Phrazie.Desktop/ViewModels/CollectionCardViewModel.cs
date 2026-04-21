using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

public partial class CollectionCardViewModel : ViewModelBase
{
    private readonly ISessionService        _session;
    private readonly Action<Collection>     _onManage;

    public Collection Model { get; }
    public string     Name  => Model.Name;

    [ObservableProperty] private Bitmap? _coverImage;
    [ObservableProperty] private bool    _isActive;

    public CollectionCardViewModel(
        Collection           model,
        ISessionService      session,
        Action<Collection>   onManage)
    {
        Model     = model;
        _session  = session;
        _onManage = onManage;

        _isActive = _session.Current.ActiveCollection?.Id == model.Id;

        _session.SessionChanged += s =>
            Dispatcher.UIThread.Post(() =>
                IsActive = s.ActiveCollection?.Id == model.Id);

        if (!string.IsNullOrEmpty(model.CoverImagePath) && File.Exists(model.CoverImagePath))
            _coverImage = new Bitmap(model.CoverImagePath);
    }

    [RelayCommand]
    private async Task SelectAsync() =>
        await _session.SetActiveCollectionAsync(Model);

    [RelayCommand]
    private void Manage() => _onManage(Model);
}
