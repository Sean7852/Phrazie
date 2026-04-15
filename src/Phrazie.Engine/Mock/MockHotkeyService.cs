using Phrazie.Core.Enums;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Engine.Mock;

/// <summary>
/// In-memory hotkey service with sensible defaults.
/// Replace with a persistent implementation (JSON / registry) in Phase 5.
/// </summary>
public sealed class MockHotkeyService : IHotkeyService
{
    private HotkeyAction? _pendingRebind;

    private readonly List<HotkeyBinding> _bindings =
    [
        new HotkeyBinding { Action = HotkeyAction.EmergencySwitch,  KeyName = "Space"  },
        new HotkeyBinding { Action = HotkeyAction.ScheduleTrigger,  KeyName = "Return" },
        new HotkeyBinding { Action = HotkeyAction.CancelTrigger,    KeyName = "Escape" },
        new HotkeyBinding { Action = HotkeyAction.GoToLive,         KeyName = "F2"     },
        new HotkeyBinding { Action = HotkeyAction.GoToCollections,  KeyName = "F1"     },
    ];

    public IReadOnlyList<HotkeyBinding> Bindings => _bindings.AsReadOnly();

    public event Action<HotkeyAction>?         ActionTriggered;
    public event Action<HotkeyAction, string>? RebindCompleted;

    public void SetBinding(HotkeyAction action, string keyName)
    {
        var existing = _bindings.FirstOrDefault(b => b.Action == action);
        if (existing is not null) existing.KeyName = keyName;
        else _bindings.Add(new HotkeyBinding { Action = action, KeyName = keyName });
    }

    public void StartRebind(HotkeyAction action) => _pendingRebind = action;

    public void CancelRebind() => _pendingRebind = null;

    public HotkeyAction? HandleKeyPress(string keyName)
    {
        if (_pendingRebind is { } action)
        {
            _pendingRebind = null;
            SetBinding(action, keyName);
            RebindCompleted?.Invoke(action, keyName);
            return null;
        }

        var match = _bindings.FirstOrDefault(b => b.KeyName == keyName);
        if (match is null) return null;
        ActionTriggered?.Invoke(match.Action);
        return match.Action;
    }
}
