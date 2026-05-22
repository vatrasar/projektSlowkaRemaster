using Splat;

namespace ProjektSlowkaRemasterd.Src.Infrastructure;

public interface IFeatureModule
{
    void Register(IMutableDependencyResolver services);
}
