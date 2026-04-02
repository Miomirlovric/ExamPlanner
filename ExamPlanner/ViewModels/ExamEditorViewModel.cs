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
    IGraphSectionService graphSectionService,
    IStorageManager storageManager) : ViewModelBase, IQueryAttributable
{
    private Guid _examId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isSaved;

    [ObservableProperty]
    private ObservableCollection<SectionDisplayItem> _sections = [];

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
            await LoadSectionsAsync();
    }

    private async Task LoadExamAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var exam = await db.Exams.FindAsync(_examId);
        if (exam is not null)
            Title = exam.Title;
    }

    private async Task LoadSectionsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var sections = await db.ExamSections
            .Include(s => s.GraphEntity)
                .ThenInclude(g => g.File)
            .Where(s => s.ExamEntityId == _examId)
            .ToListAsync();

        Sections = new ObservableCollection<SectionDisplayItem>(
            sections.Select(s => new SectionDisplayItem
            {
                Id = s.Id,
                Title = s.Title,
                QuestionType = s.QuestionTypeEnum.ToString(),
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
    private async Task AddSectionAsync()
    {
        await navigation.NavigateToAsync(nameof(SectionEditorPage),
            new Dictionary<string, object> { { "ExamId", _examId } });
    }

    [RelayCommand]
    private async Task EditSectionAsync(SectionDisplayItem? item)
    {
        if (item is null) return;
        await navigation.NavigateToAsync(nameof(SectionEditorPage),
            new Dictionary<string, object>
            {
                { "ExamId", _examId },
                { "SectionId", item.Id }
            });
    }

    [RelayCommand]
    private async Task DeleteSectionAsync(SectionDisplayItem? item)
    {
        if (item is null) return;

        var confirmed = await Shell.Current.DisplayAlertAsync("Delete", $"Delete section \"{item.Title}\"?", "Yes", "No");
        if (!confirmed) return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var section = await db.ExamSections.FindAsync(item.Id);
        if (section is not null)
        {
            db.ExamSections.Remove(section);
            await db.SaveChangesAsync();
        }
        Sections.Remove(item);
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
                var data = await graphSectionService.BuildSectionDataAsync(vertexCount, isDirected, questionType);

                var fileName = $"graph_{Guid.NewGuid()}.png";
                var imagePath = await storageManager.SaveFileAsync(data.ImageBytes, fileName);

                var fileEntity = new FileEntity { Id = Guid.NewGuid(), Name = fileName, Path = imagePath };
                data.Graph.FileId = fileEntity.Id;
                data.Graph.File = fileEntity;

                db.ExamSections.Add(new ExamSection
                {
                    Id = Guid.NewGuid(),
                    ExamEntityId = _examId,
                    GraphEntity = data.Graph,
                    Title = QuestionTextProvider.GetSectionTitle(questionType),
                    Question = QuestionTextProvider.GetQuestionText(questionType, isDirected),
                    QuestionTypeEnum = questionType,
                    AnswerObject = data.AnswerJson
                });
            }

            await db.SaveChangesAsync();
            await LoadSectionsAsync();
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
    private async Task ExportSectionMoodleXmlAsync(SectionDisplayItem? item)
    {
        if (item is null) return;

        IsBusy = true;
        try
        {
            var filePath = await moodleXmlExportService.ExportSectionAsync(item.Id);
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
    private ObservableCollection<LibrarySectionItem> _librarySections = [];

    [RelayCommand]
    private async Task ShowLibraryPopupAsync()
    {
        if (_examId == Guid.Empty) return;
        await LoadLibrarySectionsAsync();
        IsLibraryPopupVisible = true;
    }

    [RelayCommand]
    private void CancelLibraryPopup() => IsLibraryPopupVisible = false;

    [RelayCommand]
    private void ToggleLibraryItem(LibrarySectionItem? item)
    {
        if (item is not null)
            item.IsSelected = !item.IsSelected;
    }

    private async Task LoadLibrarySectionsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var sections = await db.ExamSections
            .Include(s => s.GraphEntity)
                .ThenInclude(g => g.File)
            .Include(s => s.ExamEntity)
            .Where(s => s.ExamEntityId != _examId)
            .ToListAsync();

        LibrarySections = new ObservableCollection<LibrarySectionItem>(
            sections.Select(s => new LibrarySectionItem
            {
                Id = s.Id,
                Title = s.Title,
                QuestionType = s.QuestionTypeEnum.ToString(),
                ExamTitle = s.ExamEntity?.Title ?? string.Empty,
                ImagePath = s.GraphEntity?.File?.Path
            }));
    }

    [RelayCommand]
    private async Task AddFromLibraryAsync()
    {
        var selected = LibrarySections.Where(s => s.IsSelected).ToList();
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
                var source = await db.ExamSections
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

                db.ExamSections.Add(new ExamSection
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
            await LoadSectionsAsync();
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

public class SectionDisplayItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public bool HasImage => !string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath);
}

public partial class LibrarySectionItem : ObservableObject
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
