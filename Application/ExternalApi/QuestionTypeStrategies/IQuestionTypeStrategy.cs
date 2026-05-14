using Application.Storage;

using Domain.Entities;
using Domain.Values;

namespace Application.ExternalApi.QuestionTypeStrategies;

public interface IQuestionTypeStrategy
{
    QuestionTypeEnum SupportedType { get; }

    bool NeedsWeights { get; }

    RandomGraphRequestGraph_type GraphType { get; }

    Task<QuestionAnswerData> BuildAnswersAsync(
        GraphEntity graph,
        IGraphAnalysisService api,
        CancellationToken ct = default);
}

public sealed record QuestionAnswerData(GenericQuestionAnswers Answers, string? QuestionTextOverride = null);
