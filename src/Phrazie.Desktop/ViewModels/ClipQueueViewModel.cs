using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

public partial class ClipQueueViewModel : ViewModelBase,
    IRecipient<ClipsChangedMessage>
{
    private readonly ISessionService  _session;
    private readonly IPlaybackService _playback;

    // Preserves source collection/state names + color across LoadClips rebuilds
    private readonly Dictionary<Guid, (string Collection, string State, string Color)> _clipMeta = new();

    public ObservableCollection<LiveClipItemViewModel> Clips { get; } = new();

    public ClipQueueViewModel(ISessionService session, IPlaybackService playback)
    {
        _session  = session;
        _playback = playback;

        WeakReferenceMessenger.Default.Register(this);

        LoadClips();
        UpdateActiveClip(_playback.CurrentClip);

        _session.SessionChanged += _ =>
            Dispatcher.UIThread.Post(LoadClips);

        _playback.ClipChanged += clip =>
            Dispatcher.UIThread.Post(() => UpdateActiveClip(clip));
    }

    // ── IRecipient: react to clip changes made in the Collections tab ──────

    public void Receive(ClipsChangedMessage message)
    {
        var currentStateId = _session.Current.CurrentState?.Id;
        if (message.Value.Id == currentStateId)
            Dispatcher.UIThread.Post(LoadClips);
    }

    // ── Add clip ───────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddClip()
    {
        var state = _session.Current.CurrentState;

        WeakReferenceMessenger.Default.Send(new OpenClipBrowserMessage(selected =>
        {
            if (state is null) return;
            foreach (var info in selected)
            {
                _clipMeta[info.Clip.Id] = (info.CollectionName, info.StateName, info.StateColor);
                state.Clips.Add(info.Clip);
                Clips.Add(new LiveClipItemViewModel(info.Clip, RequestRemove,
                              info.CollectionName, info.StateName, info.StateColor));
            }
            WeakReferenceMessenger.Default.Send(new ClipsChangedMessage(state));
        }));
    }

    // ── Internals ──────────────────────────────────────────────────────────

    private void LoadClips()
    {
        Clips.Clear();

        var defaultCollection = _session.Current.ActiveCollection?.Name ?? "—";
        var defaultState      = _session.Current.CurrentState?.Name      ?? "—";

        var defaultColor = _session.Current.CurrentState?.Color ?? "#443366";

        var clips = _session.Current.CurrentState?.Clips ?? [];
        foreach (var clip in clips)
        {
            var (col, st, color) = _clipMeta.TryGetValue(clip.Id, out var meta)
                ? meta
                : (defaultCollection, defaultState, defaultColor);
            Clips.Add(new LiveClipItemViewModel(clip, RequestRemove, col, st, color));
        }

    }

    private void UpdateActiveClip(Clip? clip)
    {
        foreach (var item in Clips)
            item.IsActive = item.Model.Id == clip?.Id;
    }

    private async void RequestRemove(LiveClipItemViewModel item)
    {
        item.Opacity = 0;
        await Task.Delay(220);
        _clipMeta.Remove(item.Model.Id);
        _session.Current.CurrentState?.Clips.Remove(item.Model);
        Clips.Remove(item);
        WeakReferenceMessenger.Default.Send(
            new ClipsChangedMessage(_session.Current.CurrentState ?? new State()));
    }
}
