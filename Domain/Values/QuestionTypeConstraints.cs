namespace Domain.Values;

public static class QuestionTypeConstraints
{
    public static readonly IReadOnlyList<QuestionTypeEnum> DirectedOnly =
    [
        QuestionTypeEnum.TOPOLOSKO_SORTIRANJE,
        QuestionTypeEnum.CVRSTO_POVEZANE_KOMPONENTE,
    ];

    public static readonly IReadOnlyList<QuestionTypeEnum> UndirectedOnly =
    [
        QuestionTypeEnum.DIJKSTRA,
    ];

    public static bool? GetForcedDirected(QuestionTypeEnum type)
    {
        if (DirectedOnly.Contains(type)) return true;
        if (UndirectedOnly.Contains(type)) return false;
        return null;
    }
}
