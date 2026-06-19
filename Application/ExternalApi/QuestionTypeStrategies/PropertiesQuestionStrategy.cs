using Application.ExternalApi.Adapters;

using Domain.Entities;
using Domain.Values;

namespace Application.ExternalApi.QuestionTypeStrategies;

public sealed class PropertiesQuestionStrategy(IAnswerAdapter<PropertiesResponse> adapter) : IQuestionTypeStrategy
{
    public QuestionTypeEnum SupportedType => QuestionTypeEnum.ANALIZA_GRAFA;
    public bool NeedsWeights => false;
    public RandomGraphRequestGraph_type GraphType => RandomGraphRequestGraph_type.Properties;

    public async Task<QuestionAnswerData> BuildAnswersAsync(
        GraphEntity graph,
        IGraphAnalysisService api,
        CancellationToken ct = default)
    {
        var response = await api.GetPropertiesAsync(graph, ct);
        return new QuestionAnswerData(adapter.Adapt(response));
    }
}
