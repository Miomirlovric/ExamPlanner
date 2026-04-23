using Application.Storage;
using Domain.Entities;
using Domain.Values;

namespace Application.ExternalApi;

public class GraphQuestionservice(IGraphAnalysisService graphAnalysisService) : IGraphQuestionservice
{
    public async Task<GraphQuestionData> BuildQuestionDataAsync(
        int vertexCount,
        bool isDirected,
        QuestionTypeEnum questionType,
        CancellationToken cancellationToken = default)
    {
        var graphType = GetGraphType(questionType);
        var generated = await graphAnalysisService.GenerateRandomGraphAsync(vertexCount, isDirected, graphType, NeedsWeights(questionType), cancellationToken);

        var graph = new GraphEntity
        {
            IsDirected = isDirected,
            GraphRelations = generated.Edges.Select(e => new GraphRelation
            {
                Id = Guid.NewGuid(),
                A = e.Source,
                B = e.Target,
                Weight = e.Weight
            }).ToList()
        };

        var imageBytes = await graphAnalysisService.GetGraphImageBytesAsync(graph, cancellationToken);

        string? questionTextOverride = null;
        GenericQuestionAnswers answers;
        if (questionType == QuestionTypeEnum.DIJKSTRA)
        {
            var source = graph.GraphRelations
                .SelectMany(r => new[] { r.A, r.B })
                .Distinct()
                .First();
            var shortestPaths = await graphAnalysisService.GetShortestPathsAsync(graph, source, cancellationToken);
            answers = QuestionAnswersMapper.FromShortestPaths(shortestPaths);
            questionTextOverride = $"Za neusmjereni graf sa slike odredite najkraće putove od početnog vrha {source} do svih vrhova, njima odgovarajuće udaljenosti i put do najudaljenijeg vrha od vrha {source}.";
        }
        else
        {
            answers = questionType switch
            {
                QuestionTypeEnum.ANALIZA_CENTRALNOSTI =>
                    QuestionAnswersMapper.FromCentralities(
                        await graphAnalysisService.GetCentralitiesAsync(graph, cancellationToken)),
                QuestionTypeEnum.ANALIZA_GRAFA =>
                    QuestionAnswersMapper.FromProperties(
                        await graphAnalysisService.GetPropertiesAsync(graph, cancellationToken)),
                QuestionTypeEnum.TOPOLOSKO_SORTIRANJE =>
                    QuestionAnswersMapper.FromTopologicalSort(
                        await graphAnalysisService.GetTopologicalSortAsync(graph, cancellationToken)),
                QuestionTypeEnum.CVRSTO_POVEZANE_KOMPONENTE =>
                    QuestionAnswersMapper.FromScc(
                        await graphAnalysisService.GetStronglyConnectedComponentsAsync(graph, cancellationToken)),
                _ => throw new InvalidOperationException($"Unsupported question type: {questionType}")
            };
        }
        var answerJson = Newtonsoft.Json.JsonConvert.SerializeObject(answers);

        return new GraphQuestionData(graph, imageBytes, answerJson, questionTextOverride);
    }

    private static RandomGraphRequestGraph_type GetGraphType(QuestionTypeEnum type) => type switch
    {
        QuestionTypeEnum.TOPOLOSKO_SORTIRANJE => RandomGraphRequestGraph_type.Dag,
        QuestionTypeEnum.CVRSTO_POVEZANE_KOMPONENTE => RandomGraphRequestGraph_type.Scc,
        _ => RandomGraphRequestGraph_type.Default
    };

    private static bool NeedsWeights(QuestionTypeEnum type) => type == QuestionTypeEnum.DIJKSTRA;
}
