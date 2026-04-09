using Domain.Entities;

namespace Application.ExternalApi;

public interface IGraphAnalysisService
{
    Task<CentralitiesResponse> GetCentralitiesAsync(GraphEntity graph, CancellationToken cancellationToken = default);
    Task<PropertiesResponse> GetPropertiesAsync(GraphEntity graph, CancellationToken cancellationToken = default);
    Task<TopologicalSortResponse> GetTopologicalSortAsync(GraphEntity graph, CancellationToken cancellationToken = default);
    Task<StronglyConnectedComponentsResponse> GetStronglyConnectedComponentsAsync(GraphEntity graph, CancellationToken cancellationToken = default);
    Task<GenerateGraphResponse> GenerateRandomGraphAsync(int vertexCount, bool directed, RandomGraphRequestGraph_type graphType = RandomGraphRequestGraph_type.Default, CancellationToken cancellationToken = default);
    Task<byte[]> GetGraphImageBytesAsync(GraphEntity graph, CancellationToken cancellationToken = default);
}
