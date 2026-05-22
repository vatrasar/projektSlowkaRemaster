using System;
using System.Linq;
using System.Reflection;
using Splat;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Navigation;

public static class AppBootstrapper
{
    /// <summary>
    /// Discovers all classes implementing IFeatureModule in the executing assembly
    /// and registers them into Splat's dependency resolver.
    /// </summary>
    /// <param name="services">Splat mutable dependency resolver</param>
    public static void Bootstrap(IMutableDependencyResolver services)
    {
        var moduleTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IFeatureModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in moduleTypes)
        {
            var module = (IFeatureModule)Activator.CreateInstance(type)!;
            module.Register(services);
        }
    }
}
