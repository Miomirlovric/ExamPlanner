using System.Collections.ObjectModel;
using Application.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Base;
using ExamPlanner.Pages;
using ExamPlanner.Services;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace ExamPlanner.ViewModels;

public partial class ExamListViewModel(
    IDbContextFactory<ExamPlannerDbContext> dbFactory,
    INavigationService navigation,
    IStorageManager storageManager) : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ExamDisplayItem> _exams = [];

    public override async Task Start()
    {
        await LoadExamsAsync();
    }

    private async Task LoadExamsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var exams = await db.Exams
            .Include(e => e.Questions)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        Exams = new ObservableCollection<ExamDisplayItem>(
            exams.Select(e => new ExamDisplayItem
            {
                Id = e.Id,
                Title = e.Title,
                CreatedAt = e.CreatedAt,
                QuestionCount = e.Questions.Count
            }));
    }

    [RelayCommand]
    private async Task CreateExamAsync()
    {
        await navigation.NavigateToAsync(nameof(ExamEditorPage));
    }

    [RelayCommand]
    private async Task EditExamAsync(ExamDisplayItem? item)
    {
        if (item is null) return;
        await navigation.NavigateToAsync(nameof(ExamEditorPage),
            new Dictionary<string, object> { { "ExamId", item.Id } });
    }

    [RelayCommand]
    private async Task DeleteExamAsync(ExamDisplayItem? item)
    {
        if (item is null) return;

        var confirmed = await Shell.Current.DisplayAlert("Delete", $"Delete exam \"{item.Title}\"?", "Yes", "No");
        if (!confirmed) return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var exam = await db.Exams
            .Include(e => e.Questions)
                .ThenInclude(s => s.GraphEntity)
                    .ThenInclude(g => g.File)
            .FirstOrDefaultAsync(e => e.Id == item.Id);

        if (exam is not null)
        {
            foreach (var Question in exam.Questions)
            {
                if (Question.GraphEntity?.File is not null)
                    await storageManager.DeleteFileAsync(Question.GraphEntity.File.Path);

                if (Question.GraphEntity is not null)
                    db.Graphs.Remove(Question.GraphEntity);
            }

            db.Exams.Remove(exam);
            await db.SaveChangesAsync();
            Exams.Remove(item);
        }
    }
}

public class ExamDisplayItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int QuestionCount { get; set; }
}
