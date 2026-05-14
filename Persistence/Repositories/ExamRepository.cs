using Application.Repositories;

using Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class ExamRepository(IDbContextFactory<ExamPlannerDbContext> dbFactory) : IExamRepository
{
    public async Task<IReadOnlyList<ExamEntity>> FindAsync(
        string? titleQuery = null,
        bool includeQuestions = false,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        IQueryable<ExamEntity> query = db.Exams.AsQueryable();

        if (includeQuestions)
            query = query.Include(e => e.Questions);

        if (!string.IsNullOrWhiteSpace(titleQuery))
            query = query.Where(e => e.Title.Contains(titleQuery));

        query = query.OrderByDescending(e => e.CreatedAt);
        return await query.ToListAsync(ct);
    }

    public async Task<ExamEntity?> GetByIdAsync(
        Guid id,
        bool includeQuestions = false,
        bool includeGraphFiles = false,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        IQueryable<ExamEntity> query = db.Exams.AsQueryable();
        if (includeQuestions)
        {
            var withQuestions = query.Include(e => e.Questions);
            query = includeGraphFiles
                ? withQuestions
                    .ThenInclude(s => s.GraphEntity)
                    .ThenInclude(g => g.File)
                : withQuestions;
        }
        return await query.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task AddAsync(ExamEntity exam, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.Exams.Add(exam);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateTitleAsync(Guid id, string title, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var exam = await db.Exams.FindAsync([id], ct);
        if (exam is null) return;
        exam.Title = title;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(ExamEntity exam, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Reattach + cascade. Caller is responsible for any external file cleanup.
        db.Attach(exam);
        foreach (var q in exam.Questions)
        {
            if (q.GraphEntity is not null)
                db.Graphs.Remove(q.GraphEntity);
        }
        db.Exams.Remove(exam);
        await db.SaveChangesAsync(ct);
    }
}
