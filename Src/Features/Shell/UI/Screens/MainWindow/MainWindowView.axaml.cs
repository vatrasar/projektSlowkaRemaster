using ReactiveUI;
using Avalonia.ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;
using ProjektSlowkaRemasterd.Src.Features.Shell.UI.Screens.MainWindow;

namespace ProjektSlowkaRemasterd.Src.Features.Shell.UI.Screens.MainWindow;

/// <summary>
/// Screen: MainWindowView
/// Purpose: The main entry window of the application hosting the static Shell layout (sidebar) and router.
/// Available Functionalities: Displays the primary content navigation region and wraps the host view.
/// Key UI Elements: HostViewControl
/// Navigation:
///   - Navigate From: Application Startup (App.axaml.cs)
///   - Navigate To: Displays HostView, which handles nested view routing.
/// </summary>
public partial class MainWindowView : ReactiveWindow<MainWindowViewModel>
{
    public MainWindowView()
    {
        InitializeComponent();

        this.WhenAnyValue(x => x.ViewModel)
            .Where(vm => vm != null)
            .Subscribe(Observer.Create<MainWindowViewModel?>(vm =>
            {
                HostViewControl.ViewModel = vm!.Host;
            }));
    }
}
