namespace Phrazie.Desktop.ViewModels;

/// <summary>Base class for every entry in the Settings master menu.</summary>
public abstract class SettingsSectionViewModel : ViewModelBase
{
    public abstract string SectionName        { get; }
    public abstract string SectionDescription { get; }
    public abstract string SectionIcon        { get; }
}
