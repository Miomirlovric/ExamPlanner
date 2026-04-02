using Application.Settings;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace ExamPlanner;

public class DbContextFactory
{
    public static ExamPlannerDbContext Create()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, DatabaseSettings.DatabaseName);

        var options = new DbContextOptionsBuilder<ExamPlannerDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new ExamPlannerDbContext(options);
    }
}
