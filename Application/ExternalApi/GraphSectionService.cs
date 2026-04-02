using Domain.Entities;
using Domain.Values;

namespace Application.ExternalApi;

public class GraphSectionService(IGraphAnalysisService graphAnalysisService) : IGraphSectionService
{
    public async Task<GraphSectionData> BuildSectionDataAsync(
        int vertexCount,
        bool isDirected,
        QuestionTypeEnum questionType,
        CancellationToken cancellationToken = default)
    {
        var generated = await graphAnalysisService.GenerateRandomGraphAsync(vertexCount, isDirected, cancellationToken);

        var graph = new GraphEntity
        {
            IsDirected = isDirected,
            GraphRelations = generated.Edges.Select(e => new GraphRelation
            {
                Id = Guid.NewGuid(),
                A = e.Source,
                B = e.Target
            }).ToList()
        };

        var imageBytes = await graphAnalysisService.GetGraphImageBytesAsync(graph, cancellationToken);

        var answerJson = questionType switch
        {
            QuestionTypeEnum.ANALIZA_CENTRALNOSTI =>
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    await graphAnalysisService.GetCentralitiesAsync(graph, cancellationToken)),
            QuestionTypeEnum.ANALIZA_GRAFA =>
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    await graphAnalysisService.GetPropertiesAsync(graph, cancellationToken)),
            _ => throw new InvalidOperationException($"Unsupported question type: {questionType}")
        };

        return new GraphSectionData(graph, imageBytes, answerJson);
    }
}
