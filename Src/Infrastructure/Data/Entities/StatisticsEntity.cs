namespace ProjektSlowkaRemasterd.Src.Infrastructure.Data.Entities;

public class StatisticsEntity
{
    public int Id { get; set; }
    public int Failures { get; set; }

    public QuestionEntity? Question { get; set; }
}
