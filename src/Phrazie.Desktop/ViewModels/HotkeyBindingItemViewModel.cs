using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

public partial class HotkeyBindingItemViewModel : ObservableObject
{
    private readonly IHotkeyService _service;

    public HotkeyAction Action      { get; }
    public string       ActionLabel { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotRebinding))]
    private bool _isRebinding;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KeyDisplayName))]
    private string _keyName = string.Empty;

    public bool   IsNotRebinding  => !IsRebinding;
    public string KeyDisplayName  => KeyName switch
    {
        "Return" => "Enter",
        "Escape" => "Esc",
        "Back"   => "Backspace",
        "Prior"  => "PgUp",
        "Next"   => "PgDn",
        _        => KeyName,
    };

    private static readonly Dictionary<HotkeyAction, string> Labels = new()
    {
        [HotkeyAction.EmergencySwitch]  = "Emergency Switch",
        [HotkeyAction.ScheduleTrigger]  = "Schedule Trigger",
        [HotkeyAction.CancelTrigger]    = "Cancel Trigger",
        [HotkeyAction.GoToLive]         = "Go to Live",
        [HotkeyAction.GoToCollections]  = "Go to Collections",
    };

    public HotkeyBindingItemViewModel(HotkeyBinding binding, IHotkeyService service)
    {
        _service    = service;
        Action      = binding.Action;
        ActionLabel = Labels.TryGetValue(binding.Action, out var lbl) ? lbl : binding.Action.ToString();
        _keyName    = binding.KeyName;
    }

    [RelayCommand]
    private void StartRebind()
    {
        _service.StartRebind(Action);
        IsRebinding = true;
    }

    [RelayCommand]
    private void CancelRebind()
    {
        _service.CancelRebind();
        IsRebinding = false;
    }

    public void CompleteRebind(string newKey)
    {
        KeyName     = newKey;
        IsRebinding = false;
    }
}
