using Splat;
using ReactiveUI;
using ProjektSlowkaRemasterd.Src.Infrastructure;
using ProjektSlowkaRemasterd.Src.Features.Search.UI.Screens.Search;

namespace ProjektSlowkaRemasterd.Src.Features.Search;

/// <summary>
/// Registers the Search feature views and view models in Splat.
/// </summary>
public class SearchModule : IFeatureModule
{
    public void Register(IMutableDependencyResolver services)
    {
        services.Register(() => new SearchView(), typeof(IViewFor<SearchViewModel>));
    }
}
