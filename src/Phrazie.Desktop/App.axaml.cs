using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Phrazie.Core.Interfaces;
using Phrazie.Desktop.Database;
using Phrazie.Desktop.Services;
using Phrazie.Desktop.ViewModels;
using Phrazie.Desktop.Views;
using Phrazie.Engine.Mock;
using Supabase;

namespace Phrazie.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = BuildServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };

            desktop.MainWindow.Closed += (_, _) =>
                Services.GetRequiredService<ProjectionService>().Close();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        // ── Supabase client ────────────────────────────────────────────────────
        var supabase = new Client(
            SupabaseConfig.Url,
            SupabaseConfig.AnonKey,
            new SupabaseOptions { AutoRefreshToken = true, AutoConnectRealtime = false });
        supabase.InitializeAsync().GetAwaiter().GetResult();
        services.AddSingleton(supabase);

        // ── Auth services ──────────────────────────────────────────────────────
        services.AddSingleton<ISessionStore, SessionStore>();
        services.AddSingleton<IAuthService,  AuthService>();

        // ── Local database ─────────────────────────────────────────────────────
        services.AddSingleton<LocalDatabase>();

        // ── Core interfaces → SQLite implementations ───────────────────────────
        services.AddSingleton<ICollectionRepository, SqliteCollectionRepository>();
        services.AddSingleton<IClipRepository,        SqliteClipRepository>();
        services.AddSingleton<VideoPlaybackService>();
        services.AddSingleton<IPlaybackService>(sp => sp.GetRequiredService<VideoPlaybackService>());
        services.AddSingleton<IBeatClock,             BeatClock>();
        services.AddSingleton<ITriggerService,         MockTriggerService>();
        services.AddSingleton<ISessionService,         MockSessionService>();
        services.AddSingleton<IHotkeyService,          MockHotkeyService>();

        // ── Desktop services ───────────────────────────────────────────────────
        services.AddSingleton<IFilePickerService,    AvaloniaFilePickerService>();
        services.AddSingleton<TransitionPoolService>();
        services.AddSingleton<ProjectionService>();

        // ── ViewModels ─────────────────────────────────────────────────────────
        services.AddSingleton<LoginViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<CollectionsViewModel>();
        services.AddTransient<LivePerformanceViewModel>();

        var provider = services.BuildServiceProvider();

        // ── Restore persisted session ──────────────────────────────────────────
        // SetSession decodes the JWT, refreshes it if expired, and populates
        // CurrentUser — no full Session graph serialization needed.
        var tokens = SupabaseSessionHandler.Load();
        if (tokens is not null)
        {
            try
            {
                // Run on thread pool to avoid deadlocking the Avalonia sync context
                var restored = Task.Run(() =>
                    supabase.Auth.SetSession(
                        tokens.Value.AccessToken,
                        tokens.Value.RefreshToken,
                        false)).GetAwaiter().GetResult();

                if (restored?.User is not null)
                {
                    if (restored.AccessToken is not null && restored.RefreshToken is not null)
                        SupabaseSessionHandler.Save(restored.AccessToken, restored.RefreshToken);

                    var store = provider.GetRequiredService<ISessionStore>();
                    store.SetUser(new Phrazie.Core.Models.AuthUser
                    {
                        Id    = restored.User.Id    ?? string.Empty,
                        Email = restored.User.Email ?? string.Empty,
                    });
                }
            }
            catch
            {
                // Token invalid or network error — user will be asked to sign in
                SupabaseSessionHandler.Destroy();
            }
        }

        return provider;
    }
}
