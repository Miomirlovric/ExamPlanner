using System.Globalization;

using Application.Storage;

namespace Application.ExternalApi.Adapters;

public sealed class ShortestPathsAdapter : IAnswerAdapter<ShortestPathsResponse>
{
    public GenericQuestionAnswers Adapt(ShortestPathsResponse response)
    {
        var lines = new List<AnswerLine>();
        foreach (var entry in response.Paths.Where(p => p.Vertex != response.Source))
        {
            lines.Add(AdapterHelpers.Line(
                $"do vrha {entry.Vertex}: ",
                [entry.Distance?.ToString("F2", CultureInfo.InvariantCulture) ?? "N/A"],
                ","));
        }
        var farthestPath = string.Join("", response.Farthest_path);
        lines.Add(AdapterHelpers.Line("Put do najudaljenijeg vrha jest ", [farthestPath], "."));
        return new GenericQuestionAnswers { Lines = lines };
    }
}
