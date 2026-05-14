using Domain.Entities;

namespace Application.Repositories;

public interface IQuestionRepository
{
    Task<IReadOnlyList<ExamQuestion>> FindInExamAsync(
        Guid examId,
        string? titleOrBodyQuery = null,
        bool includeGraphFile = false,
        bool includeGraphRelations = false,
        bool includeExam = false,
        CancellationToken ct = default);

    Task<IReadOnlyList<ExamQuestion>> FindNotInExamAsync(
        Guid excludedExamId,
        string? titleOrBodyQuery = null,
        bool includeGraphFile = false,
        bool includeGraphRelations = false,
        bool includeExam = false,
        CancellationToken ct = default);

    Task<ExamQuestion?> GetByIdAsync(
        Guid id,
        bool includeGraphFile = false,
        bool includeGraphRelations = false,
        CancellationToken ct = default);

    Task AddAsync(ExamQuestion question, CancellationToken ct = default);
    Task UpdateAsync(ExamQuestion question, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task ReplaceGraphRelationsAsync(int graphEntityId, IEnumerable<GraphRelation> newRelations, CancellationToken ct = default);
}
