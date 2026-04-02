using System.Text;
using Domain.Entities;

namespace Application.ExternalApi;

public class GraphAnalysisService(
    CentralitiesClient centralitiesClient,
    PropertiesClient propertiesClient,
    GenerateClient generateClient,
    IHttpClientFactory httpClientFactory,
    ExternalApiSettings apiSettings) : IGraphAnalysisService
{
    public Task<CentralitiesResponse> GetCentralitiesAsync(GraphEntity graph, CancellationToken cancellationToken = default)
    {
        var request = MapToGraphRequest(graph);
        return centralitiesClient.PostAsync(request, cancellationToken);
    }

    public Task<PropertiesResponse> GetPropertiesAsync(GraphEntity graph, CancellationToken cancellationToken = default)
    {
        var request = MapToGraphRequest(graph);
        return propertiesClient.PostAsync(request, cancellationToken);
    }

    public Task<GenerateGraphResponse> GenerateRandomGraphAsync(int vertexCount, bool directed, CancellationToken cancellationToken = default)
    {
        var request = new RandomGraphRequest { Vertex_count = vertexCount, Directed = directed };
        return generateClient.PostAsync(request, cancellationToken);
    }

    public async Task<byte[]> GetGraphImageBytesAsync(GraphEntity graph, CancellationToken cancellationToken = default)
    {
        using var client = httpClientFactory.CreateClient(nameof(ImageClient));
        var request = MapToGraphRequest(graph);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var baseUrl = apiSettings.BaseUrl;
        if (!baseUrl.EndsWith('/')) baseUrl += '/';

        using var response = await client.PostAsync($"{baseUrl}graph/image", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private static GraphRequest MapToGraphRequest(GraphEntity graph)
    {
        var edges = graph.GraphRelations.Select(r => new Edge
        {
            Source = r.A,
            Target = r.B
        }).ToList();

        return new GraphRequest
        {
            Edges = edges,
            Directed = graph.IsDirected
        };
    }
}
