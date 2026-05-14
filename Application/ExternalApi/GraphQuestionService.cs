using Application.ExternalApi.QuestionTypeStrategies;

using Domain.Entities;
using Domain.Values;

namespace Application.ExternalApi;

public class GraphQuestionservice(
    IGraphAnalysisService graphAnalysisService,
    IEnumerable<IQuestionTypeStrategy> strategies) : IGraphQuestionservice
{
    private readonly IReadOnlyDictionary<QuestionTypeEnum, IQuestionTypeStrategy> _strategies =
        strategies.ToDictionary(s => s.SupportedType);

    public async Task<GraphQuestionData> BuildQuestionDataAsync(
        int vertexCount,
        bool isDirected,
        QuestionTypeEnum questionType,
        CancellationToken cancellationToken = default)
    {
        if (!_strategies.TryGetValue(questionType, out var strategy))
            throw new InvalidOperationException($"No strategy registered for question type: {questionType}");

        var generated = await graphAnalysisService.GenerateRandomGraphAsync(
            vertexCount,
            isDirected,
            strategy.GraphType,
            strategy.NeedsWeights,
            cancellationToken);

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
        var answerData = await strategy.BuildAnswersAsync(graph, graphAnalysisService, cancellationToken);
        var answerJson = Newtonsoft.Json.JsonConvert.SerializeObject(answerData.Answers);

        return new GraphQuestionData(graph, imageBytes, answerJson, answerData.QuestionTextOverride);
    }
}
