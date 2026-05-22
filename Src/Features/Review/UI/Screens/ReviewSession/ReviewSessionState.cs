using System.Collections.Immutable;

namespace ProjektSlowkaRemasterd.Src.Features.Review.UI.Screens.ReviewSession;

using Question = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Question;
using Media = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Media;

public record ReviewSessionState
{
    public bool IsLoading { get; init; } = false;
    public string CategoryName { get; init; } = string.Empty;
    public string TopicName { get; init; } = string.Empty;
    public int TotalQuestionsCount { get; init; } = 0;
    public int CurrentQuestionIndex { get; init; } = 0;
    public Question? CurrentQuestion { get; init; } = null;
    public bool IsAnswerVisible { get; init; } = false;
    public bool IsFinished { get; init; } = false;
    public string QuestionText { get; init; } = string.Empty;
    public string AnswerText { get; init; } = string.Empty;
    public ImmutableList<Media> QuestionMedia { get; init; } = ImmutableList<Media>.Empty;
    public ImmutableList<Media> AnswerMedia { get; init; } = ImmutableList<Media>.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public bool IsBidirectional { get; init; } = false;
    public string CurrentDirection { get; init; } = "Q->A"; // "Q->A" or "A->Q"
}
