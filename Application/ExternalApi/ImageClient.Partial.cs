using System.Net.Http;
using System.Text;

namespace Application.ExternalApi;

public partial class ImageClient
{
    public async Task<byte[]> GetImageBytesAsync(GraphRequest request, CancellationToken cancellationToken = default)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var baseUrl = BaseUrl;
        if (!baseUrl.EndsWith('/')) baseUrl += '/';

        using var response = await _httpClient.PostAsync($"{baseUrl}graph/image", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
