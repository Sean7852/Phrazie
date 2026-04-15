using System.Collections.ObjectModel;
using Phrazie.Core.Interfaces;

namespace Phrazie.Desktop.ViewModels;

public sealed class HotkeyMappingViewModel : SettingsSectionViewModel
{
    private readonly IHotkeyService _service;

    public override string SectionName        => "Hotkey Mapping";
    public override string SectionDescription => "Customize keyboard shortcuts for live controls.";
    public override string SectionIcon        => "⌨";

    public ObservableCollection<HotkeyBindingItemViewModel> Bindings { get; } = new();

    public HotkeyMappingViewModel(IHotkeyService service)
    {
        _service = service;
        Reload();

        service.RebindCompleted += (action, key) =>
        {
            foreach (var item in Bindings)
                if (item.Action == action) item.CompleteRebind(key);
        };
    }

    private void Reload()
    {
        Bindings.Clear();
        foreach (var binding in _service.Bindings)
            Bindings.Add(new HotkeyBindingItemViewModel(binding, _service));
    }
}
