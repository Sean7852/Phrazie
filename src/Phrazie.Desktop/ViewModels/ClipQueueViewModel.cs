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

        WeakReferenceMessenger.Default.Send(new OpenClipBrowserMessage(clip =>
        {
            if (state is null) return;
            var collectionName = _session.Current.ActiveCollection?.Name ?? "—";
            state.Clips.Add(clip);
            Clips.Add(new LiveClipItemViewModel(clip, RequestRemove, collectionName, state.Name));
            WeakReferenceMessenger.Default.Send(new ClipsChangedMessage(state));
        }));
    }

    // ── Internals ──────────────────────────────────────────────────────────

    private void LoadClips()
    {
        Clips.Clear();

        var collectionName = _session.Current.ActiveCollection?.Name ?? "—";
        var stateName      = _session.Current.CurrentState?.Name ?? "—";

        var clips = _session.Current.CurrentState?.Clips ?? [];
        foreach (var clip in clips)
            Clips.Add(new LiveClipItemViewModel(clip, RequestRemove, collectionName, stateName));

        if (Clips.Count == 0)
            SeedFakeClips();
    }

    private void SeedFakeClips()
    {
        var fakes = new[]
        {
            ("Violet Grid",   "2:14", "Geometry Pack", "DROP"),
            ("Neon Tunnel",   "1:48", "Geometry Pack", "BUILD"),
            ("Pulse Wave",    "3:02", "Synthwave Vol2", "DROP"),
            ("Fractal Storm", "2:33", "Synthwave Vol2", "BREAK"),
            ("Mirror City",   "1:55", "Urban Textures", "BUILD"),
        };

        foreach (var (name, dur, col, state) in fakes)
        {
            var clip = new Clip
            {
                DisplayName = name,
                Duration    = TimeSpan.ParseExact(dur, @"m\:ss", null),
            };
            Clips.Add(new LiveClipItemViewModel(clip, RequestRemove, col, state));
        }

        if (Clips.Count > 0)
            Clips[0].IsActive = true;
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
        _session.Current.CurrentState?.Clips.Remove(item.Model);
        Clips.Remove(item);
        WeakReferenceMessenger.Default.Send(
            new ClipsChangedMessage(_session.Current.CurrentState ?? new State()));
    }
}
