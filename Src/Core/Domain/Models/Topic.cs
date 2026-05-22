namespace ProjektSlowkaRemasterd.Src.Core.Domain.Models;

public class Topic
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
}
