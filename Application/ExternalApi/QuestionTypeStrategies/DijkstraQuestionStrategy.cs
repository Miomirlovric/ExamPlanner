using Application.ExternalApi.Adapters;

using Domain.Entities;
using Domain.Values;

namespace Application.ExternalApi.QuestionTypeStrategies;

public sealed class DijkstraQuestionStrategy(IAnswerAdapter<ShortestPathsResponse> adapter) : IQuestionTypeStrategy
{
    public QuestionTypeEnum SupportedType => QuestionTypeEnum.DIJKSTRA;
    public bool NeedsWeights => true;
    public RandomGraphRequestGraph_type GraphType => RandomGraphRequestGraph_type.Default;

    public async Task<QuestionAnswerData> BuildAnswersAsync(
        GraphEntity graph,
        IGraphAnalysisService api,
        CancellationToken ct = default)
    {
        var source = graph.GraphRelations
            .SelectMany(r => new[] { r.A, r.B })
            .Distinct()
            .First();

        var response = await api.GetShortestPathsAsync(graph, source, ct);
        var answers = adapter.Adapt(response);

        var questionTextOverride =
            $"Za neusmjereni graf sa slike odredite najkraće putove od početnog vrha {source} " +
            $"do svih vrhova, njima odgovarajuće udaljenosti i put do najudaljenijeg vrha od vrha {source}.";

        return new QuestionAnswerData(answers, questionTextOverride);
    }
}
