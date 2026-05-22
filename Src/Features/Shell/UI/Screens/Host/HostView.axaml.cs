using ReactiveUI;
using Avalonia.ReactiveUI;
using ProjektSlowkaRemasterd.Src.Features.Shell.UI.Screens.Host;

namespace ProjektSlowkaRemasterd.Src.Features.Shell.UI.Screens.Host;

/// <summary>
/// Screen: HostView
/// Purpose: The core shell layout of the application containing the persistent sidebar navigation and a RoutedViewHost for sub-views.
/// Available Functionalities: Navigates between Home, Review, Training, Manage, Search, and Settings screens.
/// Key UI Elements: HomeButton, ReviewButton, TrainingButton, ManageButton, SearchButton, SettingsButton, ContentViewHost.
/// Navigation:
///   - Navigate From: MainWindowView (DataContext host)
///   - Navigate To: Sub-screens via HostScreen.Router
/// </summary>
public partial class HostView : ReactiveUserControl<HostViewModel>
{
    public HostView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel,
                vm => vm.HostScreen.Router,
                v => v.ContentViewHost.Router);

            this.BindCommand(ViewModel,
                vm => vm.NavigateHome,
                v => v.HomeButton);

            this.BindCommand(ViewModel,
                vm => vm.NavigateReview,
                v => v.ReviewButton);

            this.BindCommand(ViewModel,
                vm => vm.NavigateTraining,
                v => v.TrainingButton);

            this.BindCommand(ViewModel,
                vm => vm.NavigateManage,
                v => v.ManageButton);

            this.BindCommand(ViewModel,
                vm => vm.NavigateSearch,
                v => v.SearchButton);

            this.BindCommand(ViewModel,
                vm => vm.NavigateSettings,
                v => v.SettingsButton);
        });
    }
}
