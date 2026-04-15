using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Phrazie.Core.Interfaces;
using Phrazie.Desktop.Services;
using Phrazie.Desktop.ViewModels;
using Phrazie.Desktop.Views;
using Phrazie.Engine.Mock;

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

        // ── Core interfaces → mock implementations ──────────────────────────
        services.AddSingleton<ICollectionRepository, MockCollectionRepository>();
        services.AddSingleton<IClipRepository,        MockClipRepository>();
        services.AddSingleton<IPlaybackService,       MockPlaybackService>();
        services.AddSingleton<ITriggerService,         MockTriggerService>();
        services.AddSingleton<ISessionService,         MockSessionService>();

        // ── Desktop services ─────────────────────────────────────────────────
        services.AddSingleton<IFilePickerService, AvaloniaFilePickerService>();

        // ── ViewModels ────────────────────────────────────────────────────────
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<CollectionsViewModel>();
        services.AddTransient<LivePerformanceViewModel>();

        return services.BuildServiceProvider();
    }
}
