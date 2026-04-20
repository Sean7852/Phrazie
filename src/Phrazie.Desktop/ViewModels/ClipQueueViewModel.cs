using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Phrazie.Core.Interfaces;
using Phrazie.Core.Models;

namespace Phrazie.Desktop.ViewModels;

public partial class ClipQueueViewModel : ViewModelBase
{
    private readonly ISessionService  _session;
    private readonly IPlaybackService _playback;

    public ObservableCollection<LiveClipItemViewModel> Clips { get; } = new();

    [ObservableProperty] private string _stateName = "—";

    public ClipQueueViewModel(ISessionService session, IPlaybackService playback)
    {
        _session  = session;
        _playback = playback;

        LoadClips();
        UpdateActiveClip(_playback.CurrentClip);

        _session.SessionChanged += s =>
            Dispatcher.UIThread.Post(() => { LoadClips(); StateName = s.CurrentState?.Name ?? "—"; });

        _playback.ClipChanged += clip =>
            Dispatcher.UIThread.Post(() => UpdateActiveClip(clip));
    }

    private void LoadClips()
    {
        Clips.Clear();
        StateName = _session.Current.CurrentState?.Name ?? "—";

        var clips = _session.Current.CurrentState?.Clips ?? [];
        foreach (var clip in clips)
            Clips.Add(new LiveClipItemViewModel(clip, RequestRemove));

        // Seed fake data when the real session has nothing
        if (Clips.Count == 0)
            SeedFakeClips();
    }

    private void SeedFakeClips()
    {
        StateName = "DROP";

        var fakes = new[]
        {
            ("Violet Grid",    "2:14"),
            ("Neon Tunnel",    "1:48"),
            ("Pulse Wave",     "3:02"),
            ("Fractal Storm",  "2:33"),
            ("Mirror City",    "1:55"),
        };

        foreach (var (name, dur) in fakes)
        {
            var clip = new Clip
            {
                DisplayName = name,
                Duration    = TimeSpan.ParseExact(dur, @"m\:ss", null),
            };
            var item = new LiveClipItemViewModel(clip, RequestRemove);
            Clips.Add(item);
        }

        // Mark the first one as currently playing
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
    }
}
