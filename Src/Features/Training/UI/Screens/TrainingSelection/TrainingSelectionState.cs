using System.Collections.Immutable;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;

namespace ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSelection;

using Category = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;

public record CategoryTrainingInfo(Category Category, int TotalCount, bool IsMarked);
public record TopicTrainingInfo(Topic Topic, int TotalCount);

/// <summary>
/// Immutable state record for the Training Selection screen.
/// </summary>
public record TrainingSelectionState
{
    public bool IsLoading { get; init; } = false;
    public ImmutableList<CategoryTrainingInfo> Categories { get; init; } = ImmutableList<CategoryTrainingInfo>.Empty;
    public ImmutableList<CategoryTrainingInfo> AllCategories { get; init; } = ImmutableList<CategoryTrainingInfo>.Empty;
    public CategoryTrainingInfo? SelectedCategory { get; init; } = null;
    public ImmutableList<TopicTrainingInfo> Topics { get; init; } = ImmutableList<TopicTrainingInfo>.Empty;
    public TopicTrainingInfo? SelectedTopic { get; init; } = null;
    public int ProblematicCount { get; init; } = 0;
    public bool FilterProblematic { get; init; } = false;
    public ImmutableHashSet<int> MarkedCategoryIds { get; init; } = ImmutableHashSet<int>.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}
