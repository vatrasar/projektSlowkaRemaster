using Splat;
using ProjektSlowkaRemasterd.Src.Infrastructure;

namespace ProjektSlowkaRemasterd.Src.Features.LaTeX;

public class LaTeXModule : IFeatureModule
{
    public void Register(IMutableDependencyResolver services)
    {
        // Register views and view models here
        // services.Register(() => new MyView(), typeof(IViewFor<MyViewModel>));
    }
}
