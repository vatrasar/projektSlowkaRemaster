using Splat;
using ReactiveUI;
using ProjektSlowkaRemasterd.Src.Infrastructure;
using ProjektSlowkaRemasterd.Src.Features.Review.UI.Screens.ReviewSelection;
using ProjektSlowkaRemasterd.Src.Features.Review.UI.Screens.ReviewSession;

namespace ProjektSlowkaRemasterd.Src.Features.Review;

/// <summary>
/// Registers the Review feature views and view models in Splat.
/// </summary>
public class ReviewModule : IFeatureModule
{
    public void Register(IMutableDependencyResolver services)
    {
        services.Register(() => new ReviewSelectionView(), typeof(IViewFor<ReviewSelectionViewModel>));
        services.Register(() => new ReviewSessionView(), typeof(IViewFor<ReviewSessionViewModel>));
    }
}

