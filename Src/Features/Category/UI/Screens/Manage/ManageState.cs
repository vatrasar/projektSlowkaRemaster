using System.Collections.Immutable;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;

namespace ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.Manage;

public record ManageState
{
    public ImmutableList<Core.Domain.Models.Category> Categories { get; init; } = ImmutableList<Core.Domain.Models.Category>.Empty;
    public Core.Domain.Models.Category? SelectedCategory { get; init; }
    
    public ImmutableList<Topic> Topics { get; init; } = ImmutableList<Topic>.Empty;
    public Topic? SelectedTopic { get; init; }
    
    public ImmutableList<Section> Sections { get; init; } = ImmutableList<Section>.Empty;
    public Section? SelectedSection { get; init; }
    
    public ImmutableList<Core.Domain.Models.Question> Questions { get; init; } = ImmutableList<Core.Domain.Models.Question>.Empty;
    
    public bool IsEditingCategory { get; init; } = false;
    public string EditingCategoryName { get; init; } = string.Empty;
    public bool EditingCategoryDoubleSided { get; init; } = false;

    public string NewTopicName { get; init; } = string.Empty;
    public Topic? EditingTopic { get; init; }
    public string EditingTopicName { get; init; } = string.Empty;

    public string NewSectionName { get; init; } = string.Empty;
    public Section? EditingSection { get; init; }
    public string EditingSectionName { get; init; } = string.Empty;

    public bool ShowArchivedOnly { get; init; } = false;
    public bool IsLoading { get; init; } = false;
    public string ErrorMessage { get; init; } = string.Empty;
    public string SuccessMessage { get; init; } = string.Empty;
}
