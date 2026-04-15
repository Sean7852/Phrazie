using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Phrazie.Core.Interfaces;

namespace Phrazie.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public ObservableCollection<SettingsSectionViewModel> Sections { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedSection))]
    private SettingsSectionViewModel? _selectedSection;

    public bool HasSelectedSection => SelectedSection is not null;

    public SettingsViewModel(IHotkeyService hotkeyService)
    {
        Sections.Add(new HotkeyMappingViewModel(hotkeyService));
        // Future sections: Display, MIDI, Audio, Account…
        SelectedSection = Sections[0];
    }
}
