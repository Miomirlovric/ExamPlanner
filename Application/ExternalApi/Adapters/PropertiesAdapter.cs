using System.Globalization;

using Application.Storage;

namespace Application.ExternalApi.Adapters;

public sealed class PropertiesAdapter : IAnswerAdapter<PropertiesResponse>
{
    public GenericQuestionAnswers Adapt(PropertiesResponse response)
    {
        var p = response.Properties;
        var diameter = Newtonsoft.Json.JsonConvert.SerializeObject(p.Diameter).Trim('"');
        return new GenericQuestionAnswers
        {
            Lines =
            [
                AdapterHelpers.Line("Dijametar grafa: ", [diameter], "."),
                AdapterHelpers.Line("Gustoća grafa: ", [p.Density.ToString("F3", CultureInfo.InvariantCulture)], "."),
                AdapterHelpers.Line("Najveći stupanj ima vrh ", p.Max_degree.Vertices.ToArray(),
                     " te iznosi: ", [p.Max_degree.Value.ToString()], "."),
            ]
        };
    }
}
