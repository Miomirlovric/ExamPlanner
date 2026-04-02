using Application.Extensions;
using Application.Settings;
using Application.Storage;
using ExamPlanner.Pages;
using ExamPlanner.Services;
using ExamPlanner.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;
using Persistence.Extensions;

namespace ExamPlanner
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, DatabaseSettings.DatabaseName);
            builder.Services.AddPersistence(dbPath);

            builder.Services.AddApplicationServices("http://127.0.0.1:8000/");

            // Services
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<IStorageManager, StorageManager>();
            builder.Services.AddTransient<IWordExportService, WordExportService>();
            builder.Services.AddTransient<IMoodleXmlExportService, MoodleXmlExportService>();

            // ViewModels
            builder.Services.AddTransient<ExamListViewModel>();
            builder.Services.AddTransient<ExamEditorViewModel>();
            builder.Services.AddTransient<SectionEditorViewModel>();

            // Pages
            builder.Services.AddTransient<ExamListPage>();
            builder.Services.AddTransient<ExamEditorPage>();
            builder.Services.AddTransient<SectionEditorPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Ensure database is created
            var dbFactory = app.Services.GetRequiredService<IDbContextFactory<ExamPlannerDbContext>>();
            using (var db = dbFactory.CreateDbContext())
            {
                db.Database.Migrate();
            }

            return app;
        }
    }
}
