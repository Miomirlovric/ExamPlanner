using Application.Storage;

namespace Application.ExternalApi.Adapters;

public sealed class TopologicalSortAdapter : IAnswerAdapter<TopologicalSortResponse>
{
    public GenericQuestionAnswers Adapt(TopologicalSortResponse response)
    {
        IEnumerable<IEnumerable<string>> allOrderings =
            response.Orders is { Count: > 0 }
                ? response.Orders
                : [response.Order];

        var correctAnswers = allOrderings
            .Select(o => string.Join("", o))
            .Distinct()
            .ToArray();

        return new GenericQuestionAnswers
        {
            Lines =
            [
                AdapterHelpers.Line("Niz topološki sortiranih vrhova je ", correctAnswers, "."),
            ]
        };
    }
}
