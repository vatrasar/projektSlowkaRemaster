using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ProjektSlowkaRemasterd.Src.Infrastructure;
using ProjektSlowkaRemasterd.Src.Infrastructure.Navigation;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data;
using ProjektSlowkaRemasterd.Src.Features.Shell.UI.Screens.MainWindow;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace ProjektSlowkaRemasterd;

/// <summary>
/// Main application class coordinating startup, DI, and bootstrapping.
/// </summary>
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 1. Create MS ServiceCollection
        var services = new ServiceCollection();

        // 2. Add Infrastructure Services (Configuration, DbContext, Repositories)
        services.AddInfrastructureServices();

        // 3. Register MainWindow and MainWindowViewModel
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindowView>();

        // 4. Hook Splat to Microsoft.Extensions.DependencyInjection (must be called before Bootstrap)
        services.UseMicrosoftDependencyResolver();

        // 5. Run assembly scanning for Splat IFeatureModule registrations
        AppBootstrapper.Bootstrap(Locator.CurrentMutable);

        // 6. Build the IServiceProvider
        var serviceProvider = services.BuildServiceProvider();

        // 7. Make Splat use the built service provider
        serviceProvider.UseMicrosoftDependencyResolver();

        // Force ReactiveUI to use the Avalonia dispatcher scheduler for the main thread
        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

        // 8. Ensure database and schema exist
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NameOfAppDbContext>();
            dbContext.Database.EnsureCreated();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 9. Resolve MainWindow and assign its DataContext from DI
            var mainWindow = serviceProvider.GetRequiredService<MainWindowView>();
            mainWindow.DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}