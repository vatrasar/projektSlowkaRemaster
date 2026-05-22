using ReactiveUI;
using Avalonia.ReactiveUI;

using System.Reactive.Disposables;
using ProjektSlowkaRemasterd.Src.Features.Home.UI.Screens.Home;

namespace ProjektSlowkaRemasterd.Src.Features.Home.UI.Screens.Home.ScreenComponents.ForecastCard;

/// <summary>
/// Component: ForecastCardView
/// Purpose: Displays a card with the day name and count of reviews.
/// Usage:
///   - Inputs: Bind to a ForecastItem ViewModel/DataContext.
///   - Bindings: DayTextBlock.Text -> ForecastItem.DayName, CountTextBlock.Text -> ForecastItem.Count
/// Key UI Elements: DayTextBlock, CountTextBlock
/// Used In: HomeView
/// </summary>
public partial class ForecastCardView : ReactiveUserControl<ForecastItem>
{
    public ForecastCardView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel,
                vm => vm.DayName,
                v => v.DayTextBlock.Text);

            this.OneWayBind(ViewModel,
                vm => vm.Count,
                v => v.CountTextBlock.Text,
                count => count.ToString());
        });
    }
}
