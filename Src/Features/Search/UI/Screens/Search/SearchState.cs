using System.Collections.Immutable;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;

namespace ProjektSlowkaRemasterd.Src.Features.Search.UI.Screens.Search;

using Category = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;
using Question = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Question;

public record SearchResultItem(
    Question Question,
    string CategoryName,
    string TopicName,
    string SectionName,
    string StatusText
);

/// <summary>
/// Immutable state record for the Search screen.
/// </summary>
public record SearchState
{
    public bool IsLoading { get; init; } = false;
    public ImmutableList<Category> Categories { get; init; } = ImmutableList<Category>.Empty;
    public Category? SelectedCategory { get; init; } = null;
    public ImmutableList<Topic> Topics { get; init; } = ImmutableList<Topic>.Empty;
    public Topic? SelectedTopic { get; init; } = null;
    public ImmutableList<Section> Sections { get; init; } = ImmutableList<Section>.Empty;
    public Section? SelectedSection { get; init; } = null;
    public ImmutableList<string> Statuses { get; init; } = ImmutableList<string>.Empty;
    public string SelectedStatus { get; init; } = "All";
    public string SearchText { get; init; } = string.Empty;
    public ImmutableList<SearchResultItem> Results { get; init; } = ImmutableList<SearchResultItem>.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}
