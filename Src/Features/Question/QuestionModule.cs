using Splat;
using ReactiveUI;
using ProjektSlowkaRemasterd.Src.Infrastructure;
using ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.QuestionEditor;

namespace ProjektSlowkaRemasterd.Src.Features.Question;

public class QuestionModule : IFeatureModule
{
    public void Register(IMutableDependencyResolver services)
    {
        services.Register(() => new QuestionEditorView(), typeof(IViewFor<QuestionEditorViewModel>));
    }
}
