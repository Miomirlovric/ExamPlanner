using System.Collections.ObjectModel;

using Application.Repositories;
using Application.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ExamPlanner.Base;
using ExamPlanner.Pages;
using ExamPlanner.Services;

namespace ExamPlanner.ViewModels;

public partial class ExamListViewModel(
    IExamRepository examRepository,
    INavigationService navigation,
    IStorageManager storageManager) : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ExamDisplayItem> _exams = [];

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    partial void OnSearchQueryChanged(string value)
    {
        _ = LoadExamsAsync();
    }

    public override async Task Start()
    {
        await LoadExamsAsync();
    }

    private async Task LoadExamsAsync()
    {
        var exams = await examRepository.FindAsync(
            titleQuery: SearchQuery,
            includeQuestions: true);

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

        var exam = await examRepository.GetByIdAsync(item.Id, includeQuestions: true, includeGraphFiles: true);
        if (exam is null) return;

        // Delete graph files from disk before removing the exam record.
        foreach (var question in exam.Questions)
        {
            if (question.GraphEntity?.File is not null)
                await storageManager.DeleteFileAsync(question.GraphEntity.File.Path);
        }

        await examRepository.DeleteAsync(exam);
        Exams.Remove(item);
    }
}

public class ExamDisplayItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int QuestionCount { get; set; }
}
