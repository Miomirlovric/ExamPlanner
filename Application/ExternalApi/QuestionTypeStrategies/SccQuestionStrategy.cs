using Application.ExternalApi.Adapters;

using Domain.Entities;
using Domain.Values;

namespace Application.ExternalApi.QuestionTypeStrategies;

public sealed class SccQuestionStrategy(IAnswerAdapter<StronglyConnectedComponentsResponse> adapter) : IQuestionTypeStrategy
{
    public QuestionTypeEnum SupportedType => QuestionTypeEnum.CVRSTO_POVEZANE_KOMPONENTE;
    public bool NeedsWeights => false;
    public RandomGraphRequestGraph_type GraphType => RandomGraphRequestGraph_type.Scc;

    public async Task<QuestionAnswerData> BuildAnswersAsync(
        GraphEntity graph,
        IGraphAnalysisService api,
        CancellationToken ct = default)
    {
        var response = await api.GetStronglyConnectedComponentsAsync(graph, ct);
        return new QuestionAnswerData(adapter.Adapt(response));
    }
}
