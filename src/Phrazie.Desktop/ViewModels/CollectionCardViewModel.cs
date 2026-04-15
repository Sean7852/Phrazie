using Avalonia.Media.Imaging;
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

    public CollectionCardViewModel(
        Collection           model,
        ISessionService      session,
        Action<Collection>   onManage)
    {
        Model     = model;
        _session  = session;
        _onManage = onManage;

        if (!string.IsNullOrEmpty(model.CoverImagePath) && File.Exists(model.CoverImagePath))
            _coverImage = new Bitmap(model.CoverImagePath);
    }

    [RelayCommand]
    private async Task SelectAsync() =>
        await _session.SetActiveCollectionAsync(Model);

    [RelayCommand]
    private void Manage() => _onManage(Model);
}
