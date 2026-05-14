using Application.Storage;

namespace Application.ExternalApi.Adapters;

internal static class AdapterHelpers
{
    public static AnswerLine Line(string prefix, string[] answers, string suffix)
        => new() { Segments = [Seg(prefix), Ph(answers), Seg(suffix)] };

    public static AnswerLine Line(string prefix, string[] first, string middle, string[] second, string suffix)
        => new() { Segments = [Seg(prefix), Ph(first), Seg(middle), Ph(second), Seg(suffix)] };

    public static AnswerSegment Seg(string text) => new() { Text = text, Type = SegmentType.Text };
    public static AnswerSegment Ph(string[] answers) => new() { CorrectAnswers = answers, Type = SegmentType.Placeholder };
}
