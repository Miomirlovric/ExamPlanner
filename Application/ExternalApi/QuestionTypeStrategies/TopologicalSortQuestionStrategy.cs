using Application.ExternalApi.Adapters;

using Domain.Entities;
using Domain.Values;

namespace Application.ExternalApi.QuestionTypeStrategies;

public sealed class TopologicalSortQuestionStrategy(IAnswerAdapter<TopologicalSortResponse> adapter) : IQuestionTypeStrategy
{
    public QuestionTypeEnum SupportedType => QuestionTypeEnum.TOPOLOSKO_SORTIRANJE;
    public bool NeedsWeights => false;
    public RandomGraphRequestGraph_type GraphType => RandomGraphRequestGraph_type.Dag;

    public async Task<QuestionAnswerData> BuildAnswersAsync(
        GraphEntity graph,
        IGraphAnalysisService api,
        CancellationToken ct = default)
    {
        var response = await api.GetTopologicalSortAsync(graph, ct);
        return new QuestionAnswerData(adapter.Adapt(response));
    }
}
