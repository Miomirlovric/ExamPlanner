using Domain.Entities;

namespace Application.ExternalApi;

public interface IGraphAnalysisService
{
    Task<CentralitiesResponse> GetCentralitiesAsync(GraphEntity graph, CancellationToken cancellationToken = default);
    Task<PropertiesResponse> GetPropertiesAsync(GraphEntity graph, CancellationToken cancellationToken = default);
    Task<GenerateGraphResponse> GenerateRandomGraphAsync(int vertexCount, bool directed, CancellationToken cancellationToken = default);
    Task<byte[]> GetGraphImageBytesAsync(GraphEntity graph, CancellationToken cancellationToken = default);
}
