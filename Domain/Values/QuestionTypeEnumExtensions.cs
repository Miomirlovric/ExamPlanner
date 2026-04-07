namespace Domain.Values;

public static class QuestionTypeEnumExtensions
{
    public static string ToNormalizedString(this QuestionTypeEnum type)
    {
        return type switch
        {
            QuestionTypeEnum.ANALIZA_CENTRALNOSTI => "Analiza centralnosti",
            QuestionTypeEnum.ANALIZA_GRAFA => "Analiza grafa",
            QuestionTypeEnum.CVRSTO_POVEZANE_KOMPONENTE => "Čvrsto povezane komponente",
            QuestionTypeEnum.TOPOLOSKO_SORTIRANJE => "Topološko sortiranje",
            QuestionTypeEnum.DIJKSTRA => "Dijkstrin algoritam",
            _ => type.ToString()
        };
    }
}
