using Splat;
using ReactiveUI;
using ProjektSlowkaRemasterd.Src.Infrastructure;
using ProjektSlowkaRemasterd.Src.Features.Settings.UI.Screens.Settings;

namespace ProjektSlowkaRemasterd.Src.Features.Settings;

/// <summary>
/// Registers the Settings feature view and view model mapping inside Splat.
/// </summary>
public class SettingsModule : IFeatureModule
{
    public void Register(IMutableDependencyResolver services)
    {
        services.Register(() => new SettingsView(), typeof(IViewFor<SettingsViewModel>));
    }
}
