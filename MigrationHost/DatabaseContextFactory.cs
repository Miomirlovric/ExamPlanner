using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Persistence;

namespace MigrationHost;

public class DatabaseContextFactory : IDesignTimeDbContextFactory<ExamPlannerDbContext>
{
    public ExamPlannerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ExamPlannerDbContext>()
            .UseSqlite("Data Source=ExamPlanner.db")
            .Options;

        return new ExamPlannerDbContext(options);
    }
}
