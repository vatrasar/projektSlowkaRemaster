using System;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;

namespace ProjektSlowkaRemasterd.Src.Core.Domain.Models;

public class Question
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string AnswerText { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? TopicId { get; set; }
    public int? SectionId { get; set; }
    public int StatisticsId { get; set; }
    public int? GroupId { get; set; }
    public QuestionStatus Status { get; set; } = QuestionStatus.UNCHECKED;
    public bool IsProblematic { get; set; }
    public bool IsLastAdded { get; set; }
    public bool IsNotion { get; set; }
    public DateTime NextReview { get; set; } = DateTime.Today;
    public int Interval { get; set; } = 1;
}
