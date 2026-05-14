using Application.Storage;

namespace Application.ExternalApi.Adapters;

public sealed class SccAdapter : IAnswerAdapter<StronglyConnectedComponentsResponse>
{
    public GenericQuestionAnswers Adapt(StronglyConnectedComponentsResponse response)
    {
        var count = response.Count.ToString();
        var largestVertices = string.Join("", response.Largest.Vertices.OrderBy(v => v));
        return new GenericQuestionAnswers
        {
            Lines =
            [
                AdapterHelpers.Line("Broj čvrsto povezanih komponenti: ", [count], "."),
                AdapterHelpers.Line("Vrhovi najveće komponente (abecednim redom bez razmaka): ", [largestVertices], "."),
            ]
        };
    }
}
