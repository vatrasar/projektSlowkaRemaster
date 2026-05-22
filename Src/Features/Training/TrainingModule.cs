using ReactiveUI;
using Splat;
using ProjektSlowkaRemasterd.Src.Infrastructure;
using ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSelection;
using ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSession;

namespace ProjektSlowkaRemasterd.Src.Features.Training;

public class TrainingModule : IFeatureModule
{
    public void Register(IMutableDependencyResolver services)
    {
        services.Register(() => new TrainingSelectionView(), typeof(IViewFor<TrainingSelectionViewModel>));
        services.Register(() => new TrainingSessionView(), typeof(IViewFor<TrainingSessionViewModel>));
    }
}
