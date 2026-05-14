using Application.ExternalApi.Adapters;

using Domain.Entities;
using Domain.Values;

namespace Application.ExternalApi.QuestionTypeStrategies;

public sealed class CentralitiesQuestionStrategy(IAnswerAdapter<CentralitiesResponse> adapter) : IQuestionTypeStrategy
{
    public QuestionTypeEnum SupportedType => QuestionTypeEnum.ANALIZA_CENTRALNOSTI;
    public bool NeedsWeights => false;
    public RandomGraphRequestGraph_type GraphType => RandomGraphRequestGraph_type.Default;

    public async Task<QuestionAnswerData> BuildAnswersAsync(
        GraphEntity graph,
        IGraphAnalysisService api,
        CancellationToken ct = default)
    {
        var response = await api.GetCentralitiesAsync(graph, ct);
        return new QuestionAnswerData(adapter.Adapt(response));
    }
}
