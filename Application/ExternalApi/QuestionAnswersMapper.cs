using Application.Storage;
using System.Globalization;

namespace Application.ExternalApi;

public static class QuestionAnswersMapper
{
    public static GenericQuestionAnswers FromCentralities(CentralitiesResponse response)
    {
        var c = response.Centralities;
        return new GenericQuestionAnswers
        {
            Lines =
            [
                Line("Najveću centralnost stupnja ima vrh ", c.Degree.Vertices.ToArray(),
                     " te iznosi: ", [c.Degree.Value.ToString("F3", CultureInfo.InvariantCulture)], "."),
                Line("Najveću centralnost međupoloženosti ima vrh ", c.Betweenness.Vertices.ToArray(),
                     " te iznosi: ", [c.Betweenness.Value.ToString("F3", CultureInfo.InvariantCulture)], "."),
                Line("Najveću centralnost bliskosti ima vrh ", c.Closeness.Vertices.ToArray(),
                     " te iznosi: ", [c.Closeness.Value.ToString("F3", CultureInfo.InvariantCulture)], "."),
            ]
        };
    }

    public static GenericQuestionAnswers FromProperties(PropertiesResponse response)
    {
        var p = response.Properties;
        var diameter = Newtonsoft.Json.JsonConvert.SerializeObject(p.Diameter).Trim('"');
        return new GenericQuestionAnswers
        {
            Lines =
            [
                Line("Dijametar grafa: ", [diameter], "."),
                Line("Gustoća grafa: ", [p.Density.ToString("F3", CultureInfo.InvariantCulture)], "."),
                Line("Najveći stupanj ima vrh ", p.Max_degree.Vertices.ToArray(),
                     " te iznosi: ", [p.Max_degree.Value.ToString()], "."),
            ]
        };
    }

    public static GenericQuestionAnswers FromTopologicalSort(TopologicalSortResponse response)
    {
        var order = string.Join("", response.Order);
        return new GenericQuestionAnswers
        {
            Lines =
            [
                Line("Niz topološki sortiranih vrhova je ", [order], "."),
            ]
        };
    }

    public static GenericQuestionAnswers FromScc(StronglyConnectedComponentsResponse response)
    {
        var count = response.Count.ToString();
        var largestVertices = string.Join("", response.Largest.Vertices.OrderBy(v => v));
        return new GenericQuestionAnswers
        {
            Lines =
            [
                Line("Broj čvrsto povezanih komponenti: ", [count], "."),
                Line("Vrhovi najveće komponente (abecednim redom bez razmaka): ", [largestVertices], "."),
            ]
        };
    }

    public static GenericQuestionAnswers FromShortestPaths(ShortestPathsResponse response)
    {
        var lines = new System.Collections.Generic.List<AnswerLine>();
        foreach (var entry in response.Paths.Where(p => p.Vertex != response.Source))
        {
            lines.Add(Line($"do vrha {entry.Vertex}: ",
                [entry.Distance?.ToString("F2", CultureInfo.InvariantCulture) ?? "N/A"], ","));
        }
        var farthestPath = string.Join("", response.Farthest_path);
        lines.Add(Line("Put do najudaljenijeg vrha jest ", [farthestPath], "."));
        return new GenericQuestionAnswers { Lines = lines };
    }

    private static AnswerLine Line(string prefix, string[] answers, string suffix)
        => new() { Segments = [Seg(prefix), Ph(answers), Seg(suffix)] };

    private static AnswerLine Line(string prefix, string[] first, string middle, string[] second, string suffix)
        => new() { Segments = [Seg(prefix), Ph(first), Seg(middle), Ph(second), Seg(suffix)] };

    private static AnswerSegment Seg(string text) => new() { Text = text, Type = SegmentType.Text };
    private static AnswerSegment Ph(string[] answers) => new() { CorrectAnswers = answers, Type = SegmentType.Placeholder };
}
