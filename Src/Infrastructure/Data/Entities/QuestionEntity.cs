using System;
using System.Collections.Generic;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

public class QuestionEntity
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string AnswerText { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? TopicId { get; set; }
    public int? SectionId { get; set; }
    public int StatisticsId { get; set; }
    public int? GroupId { get; set; }
    public QuestionStatus Status { get; set; }
    public bool IsProblematic { get; set; }
    public bool IsLastAdded { get; set; }
    public bool IsNotion { get; set; }
    public DateTime NextReview { get; set; }
    public int Interval { get; set; }

    public CategoryEntity? Category { get; set; }
    public TopicEntity? Topic { get; set; }
    public SectionEntity? Section { get; set; }
    public StatisticsEntity? Statistics { get; set; }
    public ICollection<MediaEntity> Media { get; set; } = new List<MediaEntity>();
}
