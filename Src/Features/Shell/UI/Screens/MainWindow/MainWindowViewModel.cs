using ReactiveUI;
using ProjektSlowkaRemasterd.Src.Core.Mvvm;
using ProjektSlowkaRemasterd.Src.Features.Shell.UI.Screens.Host;

namespace ProjektSlowkaRemasterd.Src.Features.Shell.UI.Screens.MainWindow;

/// <summary>
/// The main window view model, acting as the IScreen routing owner.
/// </summary>
public class MainWindowViewModel : ViewModelBase, IScreen
{
    public RoutingState Router { get; }
    
    public HostViewModel Host { get; }

    public MainWindowViewModel()
    {
        Router = new RoutingState();
        Host = new HostViewModel(this);
    }
}
