namespace ProjektSlowkaRemasterd.Src.Core.Domain.Models;

public class Section
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public string Name { get; set; } = string.Empty;
}
