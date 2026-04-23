using Domain.Entities;
using Domain.Values;

namespace Application.ExternalApi;

public record GraphQuestionData(GraphEntity Graph, byte[] ImageBytes, string AnswerJson, string? QuestionTextOverride = null);

public interface IGraphQuestionservice
{
    Task<GraphQuestionData> BuildQuestionDataAsync(
        int vertexCount,
        bool isDirected,
        QuestionTypeEnum questionType,
        CancellationToken cancellationToken = default);
}
