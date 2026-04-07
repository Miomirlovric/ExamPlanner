using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Base;
using ExamPlanner.Pages;
using ExamPlanner.Services;
using Application.ExternalApi;
using Application.Storage;
using Domain.Entities;
using Domain.Values;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace ExamPlanner.ViewModels;

public partial class ExamEditorViewModel(
    IDbContextFactory<ExamPlannerDbContext> dbFactory,
    INavigationService navigation,
    IWordExportService wordExportService,
    IMoodleXmlExportService moodleXmlExportService,
    IGraphQuestionservice graphQuestionservice,
    IStorageManager storageManager) : ViewModelBase, IQueryAttributable
{
    private Guid _examId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isSaved;

    [ObservableProperty]
    private ObservableCollection<QuestionDisplayItem> _Questions = [];

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    partial void OnSearchQueryChanged(string value)
    {
        _ = LoadQuestionsAsync();
    }

    [ObservableProperty]
    private bool _isGeneratePopupVisible;

    [ObservableProperty]
    private double _generateCountValue = 3;

    private double _generateVertexCountValue = 5;
    public double GenerateVertexCountValue
    {
        get => _generateVertexCountValue;
        set => SetProperty(ref _generateVertexCountValue, value);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGenerateDirectedEnabled))]
    private QuestionTypeEnum _generateQuestionType = QuestionTypeEnum.ANALIZA_CENTRALNOSTI;

    [ObservableProperty]
    private bool _generateIsDirected;

    public bool IsGenerateDirectedEnabled
        => QuestionTypeConstraints.GetForcedDirected(GenerateQuestionType) is null;

    partial void OnGenerateQuestionTypeChanged(QuestionTypeEnum value)
    {
        var forced = QuestionTypeConstraints.GetForcedDirected(value);
        if (forced.HasValue)
            GenerateIsDirected = forced.Value;
    }

    public List<QuestionTypeEnum> QuestionTypes { get; } = Enum.GetValues<QuestionTypeEnum>().ToList();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ExamId", out var value))
        {
            _examId = value switch
            {
                Guid g => g,
                string s => Guid.Parse(s),
                _ => Guid.Empty
            };
        }
    }

    public override async Task Initialize()
    {
        if (_examId != Guid.Empty)
        {
            await LoadExamAsync();
            IsSaved = true;
        }
    }

    public override async Task Start()
    {
        if (IsSaved)
            await LoadQuestionsAsync();
    }

    private async Task LoadExamAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var exam = await db.Exams.FindAsync(_examId);
        if (exam is not null)
            Title = exam.Title;
    }

    private async Task LoadQuestionsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.ExamQuestions
            .Include(s => s.GraphEntity)
                .ThenInclude(g => g.File)
            .Where(s => s.ExamEntityId == _examId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            query = query.Where(s => s.Title.Contains(SearchQuery) || s.Question.Contains(SearchQuery));
        }

        var dbQuestions = await query.ToListAsync();

        Questions = new ObservableCollection<QuestionDisplayItem>(
            dbQuestions.Select(s => new QuestionDisplayItem
            {
                Id = s.Id,
                Title = s.Title,
                QuestionType = s.QuestionTypeEnum.ToNormalizedString(),
                ImagePath = s.GraphEntity?.File?.Path
            }));
    }

    [RelayCommand]
    private async Task SaveExamAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            await Shell.Current.DisplayAlert("Validation", "Title is required.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();

            if (_examId == Guid.Empty)
            {
                var exam = new ExamEntity
                {
                    Id = Guid.NewGuid(),
                    Title = Title
                };
                db.Exams.Add(exam);
                await db.SaveChangesAsync();
                _examId = exam.Id;
                IsSaved = true;
            }
            else
            {
                var exam = await db.Exams.FindAsync(_examId);
                if (exam is not null)
                {
                    exam.Title = Title;
                    await db.SaveChangesAsync();
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddQuestionAsync()
    {
        await navigation.NavigateToAsync(nameof(QuestionEditorPage),
            new Dictionary<string, object> { { "ExamId", _examId } });
    }

    [RelayCommand]
    private async Task EditQuestionAsync(QuestionDisplayItem? item)
    {
        if (item is null) return;
        await navigation.NavigateToAsync(nameof(QuestionEditorPage),
            new Dictionary<string, object>
            {
                { "ExamId", _examId },
                { "QuestionId", item.Id }
            });
    }

    [RelayCommand]
    private async Task DeleteQuestionAsync(QuestionDisplayItem? item)
    {
        if (item is null) return;

        var confirmed = await Shell.Current.DisplayAlertAsync("Delete", $"Delete Question \"{item.Title}\"?", "Yes", "No");
        if (!confirmed) return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var Question = await db.ExamQuestions.FindAsync(item.Id);
        if (Question is not null)
        {
            db.ExamQuestions.Remove(Question);
            await db.SaveChangesAsync();
        }
        Questions.Remove(item);
    }

    [RelayCommand]
    private async Task ExportWordAsync()
    {
        if (_examId == Guid.Empty) return;

        IsBusy = true;
        try
        {
            var filePath = await wordExportService.ExportExamAsync(_examId, Title);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Export {Title}",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Export Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    [RelayCommand]
    private async Task ExportMoodleXmlAsync()
    {
        if (_examId == Guid.Empty) return;

        IsBusy = true;
        try
        {
            var filePath = await moodleXmlExportService.ExportExamAsync(_examId, Title);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Export {Title}",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Export Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ShowGeneratePopup()
    {
        GenerateCountValue = 3;
        GenerateVertexCountValue = 5;
        GenerateQuestionType = QuestionTypeEnum.ANALIZA_CENTRALNOSTI;
        IsGeneratePopupVisible = true;
    }

    [RelayCommand]
    private void CancelGeneratePopup() => IsGeneratePopupVisible = false;

    [RelayCommand]
    private async Task GenerateQuestionsAsync()
    {
        var count = Math.Max(1, (int)GenerateCountValue);
        var vertexCount = Math.Max(2, (int)GenerateVertexCountValue);
        var questionType = GenerateQuestionType;
        IsGeneratePopupVisible = false;
        IsBusy = true;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();

            for (int i = 0; i < count; i++)
            {
                var isDirected = GenerateIsDirected;
                var data = await graphQuestionservice.BuildQuestionDataAsync(vertexCount, isDirected, questionType);

                var fileName = $"graph_{Guid.NewGuid()}.png";
                var imagePath = await storageManager.SaveFileAsync(data.ImageBytes, fileName);

                var fileEntity = new FileEntity { Id = Guid.NewGuid(), Name = fileName, Path = imagePath };
                data.Graph.FileId = fileEntity.Id;
                data.Graph.File = fileEntity;

                db.ExamQuestions.Add(new ExamQuestion
                {
                    Id = Guid.NewGuid(),
                    ExamEntityId = _examId,
                    GraphEntity = data.Graph,
                    Title = QuestionTextProvider.GetQuestionTitle(questionType),
                    Question = QuestionTextProvider.GetQuestionText(questionType, isDirected),
                    QuestionTypeEnum = questionType,
                    AnswerObject = data.AnswerJson
                });
            }

            await db.SaveChangesAsync();
            await LoadQuestionsAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportQuestionMoodleXmlAsync(QuestionDisplayItem? item)
    {
        if (item is null) return;

        IsBusy = true;
        try
        {
            var filePath = await moodleXmlExportService.ExportQuestionAsync(item.Id);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Export {item.Title}",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Export Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ObservableProperty]
    private bool _isLibraryPopupVisible;

    [ObservableProperty]
    private ObservableCollection<LibraryQuestionItem> _libraryQuestions = [];

    [ObservableProperty]
    private string _librarySearchQuery = string.Empty;

    partial void OnLibrarySearchQueryChanged(string value)
    {
        _ = LoadLibraryQuestionsAsync();
    }

    [RelayCommand]
    private async Task ShowLibraryPopupAsync()
    {
        if (_examId == Guid.Empty) return;
        await LoadLibraryQuestionsAsync();
        IsLibraryPopupVisible = true;
    }

    [RelayCommand]
    private void CancelLibraryPopup() => IsLibraryPopupVisible = false;

    [RelayCommand]
    private void ToggleLibraryItem(LibraryQuestionItem? item)
    {
        if (item is not null)
            item.IsSelected = !item.IsSelected;
    }

    private async Task LoadLibraryQuestionsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.ExamQuestions
            .Include(s => s.GraphEntity)
                .ThenInclude(g => g.File)
            .Include(s => s.ExamEntity)
            .Where(s => s.ExamEntityId != _examId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(LibrarySearchQuery))
        {
            query = query.Where(s => s.Title.Contains(LibrarySearchQuery) || s.Question.Contains(LibrarySearchQuery));
        }

        var dbQuestions = await query.ToListAsync();

        LibraryQuestions = new ObservableCollection<LibraryQuestionItem>(
            dbQuestions.Select(s => new LibraryQuestionItem
            {
                Id = s.Id,
                Title = s.Title,
                QuestionType = s.QuestionTypeEnum.ToNormalizedString(),
                ExamTitle = s.ExamEntity?.Title ?? string.Empty,
                ImagePath = s.GraphEntity?.File?.Path
            }));
    }

    [RelayCommand]
    private async Task AddFromLibraryAsync()
    {
        var selected = LibraryQuestions.Where(s => s.IsSelected).ToList();
        if (selected.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Selection", "Select at least one question.", "OK");
            return;
        }

        IsLibraryPopupVisible = false;
        IsBusy = true;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            foreach (var item in selected)
            {
                var source = await db.ExamQuestions
                    .Include(s => s.GraphEntity)
                        .ThenInclude(g => g.GraphRelations)
                    .Include(s => s.GraphEntity)
                        .ThenInclude(g => g.File)
                    .FirstOrDefaultAsync(s => s.Id == item.Id);

                if (source?.GraphEntity?.File is null) continue;
                if (!File.Exists(source.GraphEntity.File.Path)) continue;

                var imageBytes = await storageManager.LoadFileAsync(source.GraphEntity.File.Path);
                var newFileName = $"graph_{Guid.NewGuid()}.png";
                var newImagePath = await storageManager.SaveFileAsync(imageBytes, newFileName);

                var newFile = new FileEntity { Id = Guid.NewGuid(), Name = newFileName, Path = newImagePath };
                var newGraph = new GraphEntity
                {
                    IsDirected = source.GraphEntity.IsDirected,
                    FileId = newFile.Id,
                    File = newFile,
                    GraphRelations = source.GraphEntity.GraphRelations.Select(r => new GraphRelation
                    {
                        Id = Guid.NewGuid(),
                        A = r.A,
                        B = r.B,
                        Weight = r.Weight
                    }).ToList()
                };

                db.ExamQuestions.Add(new ExamQuestion
                {
                    Id = Guid.NewGuid(),
                    ExamEntityId = _examId,
                    GraphEntity = newGraph,
                    Title = source.Title,
                    Question = source.Question,
                    QuestionTypeEnum = source.QuestionTypeEnum,
                    AnswerObject = source.AnswerObject
                });
            }

            await db.SaveChangesAsync();
            await LoadQuestionsAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class QuestionDisplayItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public bool HasImage => !string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath);
}

public partial class LibraryQuestionItem : ObservableObject
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string QuestionType { get; init; } = string.Empty;
    public string ExamTitle { get; init; } = string.Empty;
    public string? ImagePath { get; init; }
    public bool HasImage => !string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath);

    [ObservableProperty]
    private bool _isSelected;
}
