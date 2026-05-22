using System;
using System.Collections.Generic;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

public class CategoryEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Reverse { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<TopicEntity> Topics { get; set; } = new List<TopicEntity>();
    public ICollection<QuestionEntity> Questions { get; set; } = new List<QuestionEntity>();
}
