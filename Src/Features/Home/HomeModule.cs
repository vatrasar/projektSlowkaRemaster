using Splat;
using ReactiveUI;
using ProjektSlowkaRemasterd.Src.Infrastructure;
using ProjektSlowkaRemasterd.Src.Features.Home.UI.Screens.Home;

namespace ProjektSlowkaRemasterd.Src.Features.Home;

/// <summary>
/// Registers the Home feature view and view model mapping inside Splat.
/// </summary>
public class HomeModule : IFeatureModule
{
    public void Register(IMutableDependencyResolver services)
    {
        services.Register(() => new HomeView(), typeof(IViewFor<HomeViewModel>));
    }
}
