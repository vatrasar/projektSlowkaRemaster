using System.Collections.Immutable;
using CategoryModel = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;
using TopicModel = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Topic;
using SectionModel = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Section;

namespace ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.BulkImport;

public record BulkImportState
{
    public bool IsLoading { get; init; }
    public ImmutableList<CategoryModel> Categories { get; init; } = ImmutableList<CategoryModel>.Empty;
    public CategoryModel? SelectedCategory { get; init; }
    public ImmutableList<TopicModel> Topics { get; init; } = ImmutableList<TopicModel>.Empty;
    public TopicModel? SelectedTopic { get; init; }
    public ImmutableList<SectionModel> Sections { get; init; } = ImmutableList<SectionModel>.Empty;
    public SectionModel? SelectedSection { get; init; }
    public string ImportText { get; init; } = string.Empty;
    public string SelectedFilePath { get; init; } = string.Empty;
    public string SuccessMessage { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}
