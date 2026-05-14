using Domain.Entities;

namespace Application.Repositories;

public interface IExamRepository
{
    Task<IReadOnlyList<ExamEntity>> FindAsync(
        string? titleQuery = null,
        bool includeQuestions = false,
        CancellationToken ct = default);

    Task<ExamEntity?> GetByIdAsync(
        Guid id,
        bool includeQuestions = false,
        bool includeGraphFiles = false,
        CancellationToken ct = default);

    Task AddAsync(ExamEntity exam, CancellationToken ct = default);
    Task UpdateTitleAsync(Guid id, string title, CancellationToken ct = default);
    Task DeleteAsync(ExamEntity exam, CancellationToken ct = default);
}
