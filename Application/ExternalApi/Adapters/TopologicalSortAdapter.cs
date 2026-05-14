using Application.Storage;

namespace Application.ExternalApi.Adapters;

public sealed class TopologicalSortAdapter : IAnswerAdapter<TopologicalSortResponse>
{
    public GenericQuestionAnswers Adapt(TopologicalSortResponse response)
    {
        var order = string.Join("", response.Order);
        return new GenericQuestionAnswers
        {
            Lines =
            [
                AdapterHelpers.Line("Niz topološki sortiranih vrhova je ", [order], "."),
            ]
        };
    }
}
