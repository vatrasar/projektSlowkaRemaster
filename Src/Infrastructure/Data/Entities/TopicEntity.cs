using System.Collections.Generic;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

public class TopicEntity
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;

    public CategoryEntity? Category { get; set; }
    public ICollection<SectionEntity> Sections { get; set; } = new List<SectionEntity>();
    public ICollection<QuestionEntity> Questions { get; set; } = new List<QuestionEntity>();
}
