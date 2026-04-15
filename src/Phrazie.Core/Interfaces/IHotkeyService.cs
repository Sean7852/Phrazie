using Phrazie.Core.Enums;
using Phrazie.Core.Models;

namespace Phrazie.Core.Interfaces;

public interface IHotkeyService
{
    IReadOnlyList<HotkeyBinding> Bindings { get; }

    void SetBinding(HotkeyAction action, string keyName);

    /// <summary>Arms the service to capture the very next key press as a new binding.</summary>
    void StartRebind(HotkeyAction action);

    /// <summary>Discards any pending rebind.</summary>
    void CancelRebind();

    /// <summary>
    /// Called by the host window on every KeyDown event.
    /// If a rebind is pending, captures the key, fires RebindCompleted, and returns null.
    /// Otherwise matches against existing bindings, fires ActionTriggered, and returns the action.
    /// </summary>
    HotkeyAction? HandleKeyPress(string keyName);

    event Action<HotkeyAction>?         ActionTriggered;
    event Action<HotkeyAction, string>? RebindCompleted;
}
