using System.Globalization;

using Application.Storage;

namespace Application.ExternalApi.Adapters;

public sealed class CentralitiesAdapter : IAnswerAdapter<CentralitiesResponse>
{
    public GenericQuestionAnswers Adapt(CentralitiesResponse response)
    {
        var c = response.Centralities;
        return new GenericQuestionAnswers
        {
            Lines =
            [
                AdapterHelpers.Line("Najveću centralnost stupnja ima vrh ", c.Degree.Vertices.ToArray(),
                     " te iznosi: ", [c.Degree.Value.ToString("F3", CultureInfo.InvariantCulture)], "."),
                AdapterHelpers.Line("Najveću centralnost međupoloženosti ima vrh ", c.Betweenness.Vertices.ToArray(),
                     " te iznosi: ", [c.Betweenness.Value.ToString("F3", CultureInfo.InvariantCulture)], "."),
                AdapterHelpers.Line("Najveću centralnost bliskosti ima vrh ", c.Closeness.Vertices.ToArray(),
                     " te iznosi: ", [c.Closeness.Value.ToString("F3", CultureInfo.InvariantCulture)], "."),
            ]
        };
    }
}
