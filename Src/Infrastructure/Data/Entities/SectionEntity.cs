using System.Collections.Generic;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

public class SectionEntity
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public string Name { get; set; } = string.Empty;

    public TopicEntity? Topic { get; set; }
    public ICollection<QuestionEntity> Questions { get; set; } = new List<QuestionEntity>();
}
