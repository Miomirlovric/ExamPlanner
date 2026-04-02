using Domain.Entities;
using Domain.Values;

namespace Application.ExternalApi;

public record GraphSectionData(GraphEntity Graph, byte[] ImageBytes, string AnswerJson);

public interface IGraphSectionService
{
    Task<GraphSectionData> BuildSectionDataAsync(
        int vertexCount,
        bool isDirected,
        QuestionTypeEnum questionType,
        CancellationToken cancellationToken = default);
}
