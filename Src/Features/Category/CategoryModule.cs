using Splat;
using ReactiveUI;
using ProjektSlowkaRemasterd.Src.Infrastructure;
using ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.Manage;
using ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.AddCategory;

namespace ProjektSlowkaRemasterd.Src.Features.Category;

/// <summary>
/// Registers the Category feature views and view models in Splat.
/// </summary>
public class CategoryModule : IFeatureModule
{
    public void Register(IMutableDependencyResolver services)
    {
        services.Register(() => new ManageView(), typeof(IViewFor<ManageViewModel>));
        services.Register(() => new AddCategoryView(), typeof(IViewFor<AddCategoryViewModel>));
    }
}
