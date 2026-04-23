using Domain.Entities;

namespace Application.ExternalApi;

public class GraphAnalysisService(
    CentralitiesClient centralitiesClient,
    PropertiesClient propertiesClient,
    GenerateClient generateClient,
    SortClient sortClient,
    SccClient sccClient,
    PathsClient pathsClient,
    ImageClient imageClient) : IGraphAnalysisService
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

    public Task<TopologicalSortResponse> GetTopologicalSortAsync(GraphEntity graph, CancellationToken cancellationToken = default)
    {
        var request = MapToGraphRequest(graph);
        return sortClient.PostAsync(request, cancellationToken);
    }

    public Task<StronglyConnectedComponentsResponse> GetStronglyConnectedComponentsAsync(GraphEntity graph, CancellationToken cancellationToken = default)
    {
        var request = MapToGraphRequest(graph);
        return sccClient.PostAsync(request, cancellationToken);
    }

    public Task<GenerateGraphResponse> GenerateRandomGraphAsync(int vertexCount, bool directed, RandomGraphRequestGraph_type graphType = RandomGraphRequestGraph_type.Default, bool includeWeights = false, CancellationToken cancellationToken = default)
    {
        var request = new RandomGraphRequest { Vertex_count = vertexCount, Directed = directed, Graph_type = graphType, Include_weights = includeWeights };
        return generateClient.PostAsync(request, cancellationToken);
    }

    public Task<ShortestPathsResponse> GetShortestPathsAsync(GraphEntity graph, string source, CancellationToken cancellationToken = default)
    {
        var edges = graph.GraphRelations.Select(r => new Edge
        {
            Source = r.A,
            Target = r.B,
            Weight = r.Weight.HasValue ? r.Weight.Value : 0
        }).ToList();
        var request = new ShortestPathsRequest
        {
            Edges = edges,
            Source = source
        };
        return pathsClient.PostAsync(request, cancellationToken);
    }

    public Task<byte[]> GetGraphImageBytesAsync(GraphEntity graph, CancellationToken cancellationToken = default)
    {
        var request = MapToGraphRequest(graph);
        return imageClient.GetImageBytesAsync(request, cancellationToken);
    }

    private static GraphRequest MapToGraphRequest(GraphEntity graph)
    {
        var edges = graph.GraphRelations.Select(r => new Edge
        {
            Source = r.A,
            Target = r.B,
            Weight = r.Weight.HasValue ? r.Weight.Value : 0
        }).ToList();

        return new GraphRequest
        {
            Edges = edges,
            Directed = graph.IsDirected
        };
    }
}
