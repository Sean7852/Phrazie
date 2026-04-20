using CommunityToolkit.Mvvm.Messaging.Messages;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

/// <summary>Broadcast whenever clips are added/removed from any state.</summary>
public sealed class ClipsChangedMessage : ValueChangedMessage<State>
{
    public ClipsChangedMessage(State state) : base(state) { }
}

/// <summary>Sent to ask MainWindowViewModel to show the clip browser modal.</summary>
public sealed class OpenClipBrowserMessage : ValueChangedMessage<Action<Clip>>
{
    public OpenClipBrowserMessage(Action<Clip> onSelected) : base(onSelected) { }
}
