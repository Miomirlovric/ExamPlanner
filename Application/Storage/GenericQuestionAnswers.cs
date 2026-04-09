namespace Application.Storage;

public class GenericQuestionAnswers
{
    public List<AnswerLine> Lines { get; set; } = [];
}

public class AnswerLine
{
    public List<AnswerSegment> Segments { get; set; } = [];
}

public class AnswerSegment
{
    public string Text { get; set; } = string.Empty;
    public SegmentType Type { get; set; }
    public string[] CorrectAnswers { get; set; } = [];
    public string DisplayValue => string.Join(", ", CorrectAnswers);
}

public enum SegmentType
{
    Text,
    Placeholder,
}
