using System.Collections.Immutable;

namespace ProjektSlowkaRemasterd.Src.Features.Home.UI.Screens.Home;

/// <summary>
/// Represents a daily review count forecast item.
/// </summary>
public record ForecastItem(string DayName, int Count);

/// <summary>
/// Immutable state record for the Home screen.
/// </summary>
public record HomeState
{
    public ImmutableList<ForecastItem> WeeklyReviews { get; init; } = ImmutableList<ForecastItem>.Empty;
    public bool IsLoading { get; init; } = false;
}
