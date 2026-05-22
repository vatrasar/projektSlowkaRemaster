using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;

namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

public class MediaEntity
{
    public int Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public int QuestionId { get; set; }
    public MediaStatus Status { get; set; }

    public QuestionEntity? Question { get; set; }
}
