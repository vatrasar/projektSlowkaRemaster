using System.Collections.Immutable;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;

namespace ProjektSlowkaRemasterd.Src.Features.Review.UI.Screens.ReviewSelection;

public record CategoryReviewInfo(Core.Domain.Models.Category Category, int DueCount);
public record TopicReviewInfo(Topic Topic, int DueCount);

/// <summary>
/// Immutable state record for the Review Selection screen.
/// </summary>
public record ReviewSelectionState
{
    public bool IsLoading { get; init; } = false;
    public ImmutableList<CategoryReviewInfo> Categories { get; init; } = ImmutableList<CategoryReviewInfo>.Empty;
    public CategoryReviewInfo? SelectedCategory { get; init; } = null;
    public ImmutableList<TopicReviewInfo> Topics { get; init; } = ImmutableList<TopicReviewInfo>.Empty;
    public TopicReviewInfo? SelectedTopic { get; init; } = null;
    public int AllInCategoryCount { get; init; } = 0;
    public string ErrorMessage { get; init; } = string.Empty;
}
