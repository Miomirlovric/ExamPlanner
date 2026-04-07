namespace Domain.Values;

public static class QuestionTextProvider
{
    public static string GetQuestionText(QuestionTypeEnum type, bool isDirected) => type switch
    {
        QuestionTypeEnum.ANALIZA_CENTRALNOSTI => isDirected
            ? "Za usmjereni graf sa slike odredite vrhove s najvecim iznosima mjera centralnosti (zaokruzeeno na tri decimale, ako ima vise takvih vrhova navedite samo jednog, ne vise od toga)."
            : "Za neusmjereni graf sa slike odredite vrhove s najvecim iznosima mjera centralnosti (zaokruzeeno na tri decimale, ako ima vise takvih vrhova navedite samo jednog, ne vise od toga).",
        QuestionTypeEnum.ANALIZA_GRAFA => isDirected
            ? "Za usmjereni graf sa slike odredite dijametar, gustocu (zaokruzeeno na tri decimale) i najveci stupanj izmedu svih vrhova (ako ima vise vrhova s najvecim stupnjem navedite samo jednog, ne vise od toga)."
            : "Za neusmjereni graf sa slike odredite dijametar, gustocu (zaokruzeeno na tri decimale) i najveci stupanj izmedu svih vrhova (ako ima vise vrhova s najvecim stupnjem navedite samo jednog, ne vise od toga).",
        _ => string.Empty
    };

    public static string GetQuestionTitle(QuestionTypeEnum type) => type switch
    {
        QuestionTypeEnum.ANALIZA_CENTRALNOSTI => $"Centralnosti - {DateTime.Now:g}",
        QuestionTypeEnum.ANALIZA_GRAFA => $"Analiza grafa - {DateTime.Now:g}",
        _ => $"Question - {DateTime.Now:g}"
    };
}
