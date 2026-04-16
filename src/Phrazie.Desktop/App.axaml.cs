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
        services.AddSingleton<IPlaybackService,       VideoPlaybackService>();
        services.AddSingleton<ITriggerService,         MockTriggerService>();
        services.AddSingleton<ISessionService,         MockSessionService>();
        services.AddSingleton<IHotkeyService,          MockHotkeyService>();

        // ── Desktop services ───────────────────────────────────────────────────
        services.AddSingleton<IFilePickerService, AvaloniaFilePickerService>();

        // ── ViewModels ─────────────────────────────────────────────────────────
        services.AddSingleton<LoginViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<CollectionsViewModel>();
        services.AddTransient<LivePerformanceViewModel>();

        return services.BuildServiceProvider();
    }
}
