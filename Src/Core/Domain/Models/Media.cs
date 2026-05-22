using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;

namespace ProjektSlowkaRemasterd.Src.Core.Domain.Models;

public class Media
{
    public int Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public int QuestionId { get; set; }
    public MediaStatus Status { get; set; }
}
