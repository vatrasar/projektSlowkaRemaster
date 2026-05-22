using System.Collections.Immutable;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
namespace ProjektSlowkaRemasterd.Src.Features.Question.UI.Screens.QuestionEditor;

using Category = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Category;


public record QuestionEditorState
{
    public int? QuestionId { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string AnswerText { get; init; } = string.Empty;
    
    public ImmutableList<Category> Categories { get; init; } = ImmutableList<Category>.Empty;
    public Category? SelectedCategory { get; init; }
    
    public ImmutableList<Topic> Topics { get; init; } = ImmutableList<Topic>.Empty;
    public Topic? SelectedTopic { get; init; }
    
    public ImmutableList<Section> Sections { get; init; } = ImmutableList<Section>.Empty;
    public Section? SelectedSection { get; init; }
    
    public string CustomSectionName { get; init; } = string.Empty;
    public bool IsNote { get; init; } = false;
    public bool IsProblematic { get; init; } = false;
    public int? GroupId { get; init; }
    
    public ImmutableList<string> QuestionImages { get; init; } = ImmutableList<string>.Empty;
    public ImmutableList<string> AnswerImages { get; init; } = ImmutableList<string>.Empty;
    
    public bool IsLoading { get; init; } = false;
    public string ErrorMessage { get; init; } = string.Empty;
    public string SuccessMessage { get; init; } = string.Empty;
    
    // For previewing the last added question
    public int? LastAddedId { get; init; }
    public string LastAddedQuestionText { get; init; } = string.Empty;
    public string LastAddedAnswerText { get; init; } = string.Empty;
}
