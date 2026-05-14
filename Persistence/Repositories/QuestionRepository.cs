using Application.Repositories;

using Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class QuestionRepository(IDbContextFactory<ExamPlannerDbContext> dbFactory) : IQuestionRepository
{
    public Task<IReadOnlyList<ExamQuestion>> FindInExamAsync(
        Guid examId,
        string? titleOrBodyQuery = null,
        bool includeGraphFile = false,
        bool includeGraphRelations = false,
        bool includeExam = false,
        CancellationToken ct = default)
        => RunQueryAsync(
            q => q.Where(s => s.ExamEntityId == examId),
            titleOrBodyQuery,
            includeGraphFile, includeGraphRelations, includeExam, ct);

    public Task<IReadOnlyList<ExamQuestion>> FindNotInExamAsync(
        Guid excludedExamId,
        string? titleOrBodyQuery = null,
        bool includeGraphFile = false,
        bool includeGraphRelations = false,
        bool includeExam = false,
        CancellationToken ct = default)
        => RunQueryAsync(
            q => q.Where(s => s.ExamEntityId != excludedExamId),
            titleOrBodyQuery,
            includeGraphFile, includeGraphRelations, includeExam, ct);

    private async Task<IReadOnlyList<ExamQuestion>> RunQueryAsync(
        Func<IQueryable<ExamQuestion>, IQueryable<ExamQuestion>> filter,
        string? titleOrBodyQuery,
        bool includeGraphFile,
        bool includeGraphRelations,
        bool includeExam,
        CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        IQueryable<ExamQuestion> query = db.ExamQuestions.AsQueryable();

        if (includeGraphFile)
            query = query.Include(s => s.GraphEntity).ThenInclude(g => g.File);
        if (includeGraphRelations)
            query = query.Include(s => s.GraphEntity).ThenInclude(g => g.GraphRelations);
        if (includeExam)
            query = query.Include(s => s.ExamEntity);

        query = filter(query);

        if (!string.IsNullOrWhiteSpace(titleOrBodyQuery))
            query = query.Where(s => s.Title.Contains(titleOrBodyQuery) || s.Question.Contains(titleOrBodyQuery));

        return await query.ToListAsync(ct);
    }

    public async Task<ExamQuestion?> GetByIdAsync(
        Guid id,
        bool includeGraphFile = false,
        bool includeGraphRelations = false,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        IQueryable<ExamQuestion> query = db.ExamQuestions.AsQueryable();

        if (includeGraphFile)
            query = query.Include(s => s.GraphEntity).ThenInclude(g => g.File);
        if (includeGraphRelations)
            query = query.Include(s => s.GraphEntity).ThenInclude(g => g.GraphRelations);

        return await query.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task AddAsync(ExamQuestion question, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.ExamQuestions.Add(question);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ExamQuestion question, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.ExamQuestions.Update(question);
        if (question.GraphEntity is not null)
        {
            db.Graphs.Update(question.GraphEntity);
            if (question.GraphEntity.File is not null)
                db.Files.Update(question.GraphEntity.File);
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var question = await db.ExamQuestions.FindAsync([id], ct);
        if (question is null) return;
        db.ExamQuestions.Remove(question);
        await db.SaveChangesAsync(ct);
    }

    public async Task ReplaceGraphRelationsAsync(
        int graphEntityId,
        IEnumerable<GraphRelation> newRelations,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.GraphRelations
            .Where(r => r.GraphEntityId == graphEntityId)
            .ExecuteDeleteAsync(ct);
        foreach (var rel in newRelations)
        {
            rel.GraphEntityId = graphEntityId;
            db.GraphRelations.Add(rel);
        }
        await db.SaveChangesAsync(ct);
    }
}
