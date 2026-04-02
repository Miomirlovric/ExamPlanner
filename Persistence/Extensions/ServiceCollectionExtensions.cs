using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string dbPath)
    {
        services.AddDbContextFactory<ExamPlannerDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        return services;
    }
}
