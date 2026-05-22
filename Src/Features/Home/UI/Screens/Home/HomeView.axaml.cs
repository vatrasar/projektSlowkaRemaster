using ReactiveUI;
using Avalonia.ReactiveUI;

using System.Reactive.Disposables;
using ProjektSlowkaRemasterd.Src.Features.Home.UI.Screens.Home;

namespace ProjektSlowkaRemasterd.Src.Features.Home.UI.Screens.Home;

/// <summary>
/// Screen: HomeView
/// Purpose: Displays the welcome screen and a 7-day spaced repetition review count forecast.
/// Available Functionalities: Automatically loads the review forecast counts for the next 7 days.
/// Key UI Elements: ReviewsItemsControl
/// Navigation:
///   - Navigate From: HostView (default view loaded on startup)
///   - Navigate To: Through sidebar to other views
/// </summary>
public partial class HomeView : ReactiveUserControl<HomeViewModel>
{
    public HomeView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel,
                vm => vm.State.WeeklyReviews,
                v => v.ReviewsItemsControl.ItemsSource);
        });
    }
}
