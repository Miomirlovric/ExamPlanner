using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Base;
using ExamPlanner.Services;
using Application.ExternalApi;
using Application.Storage;
using Domain.Entities;
using Domain.Values;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Newtonsoft.Json;

namespace ExamPlanner.ViewModels;

public partial class QuestionEditorViewModel(
    IDbContextFactory<ExamPlannerDbContext> dbFactory,
    IGraphAnalysisService graphService,
    IStorageManager storageManager,
    INavigationService navigation) : ViewModelBase, IQueryAttributable
{
    private Guid _examId;
    private Guid _QuestionId;
    private GenericQuestionAnswers? _genericAnswers;
    private byte[]? _imageBytes;

    // Input properties
    [ObservableProperty]
    private string _sourceVertex = string.Empty;

    [ObservableProperty]
    private string _targetVertex = string.Empty;

    [ObservableProperty]
    private string _weightInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuestionText))]
    private bool _isDirected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuestionText))]
    [NotifyPropertyChangedFor(nameof(IsDirectedEnabled))]
    private QuestionTypeEnum _selectedQuestionType = QuestionTypeEnum.ANALIZA_CENTRALNOSTI;

    public List<QuestionTypeEnum> QuestionTypes { get; } = Enum.GetValues<QuestionTypeEnum>().ToList();

    public ObservableCollection<AnswerLine> AnswerLines { get; } = [];

    public bool IsDirectedEnabled
        => QuestionTypeConstraints.GetForcedDirected(SelectedQuestionType) is null;

    partial void OnSelectedQuestionTypeChanged(QuestionTypeEnum value)
    {
        var forced = QuestionTypeConstraints.GetForcedDirected(value);
        if (forced.HasValue)
            IsDirected = forced.Value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsManualInput))]
    [NotifyPropertyChangedFor(nameof(IsRandomInput))]
    private bool _useRandomGeneration;

    [ObservableProperty]
    private string _vertexCount = string.Empty;

    public bool IsManualInput => !UseRandomGeneration;
    public bool IsRandomInput => UseRandomGeneration;

    public ObservableCollection<EdgeItem> Edges { get; } = [];

    // Result properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInputVisible))]
    private bool _isGenerated;

    public bool IsInputVisible => !IsGenerated;

    [ObservableProperty]
    private ImageSource? _graphImageSource;
    [ObservableProperty]
    private string _questionText = string.Empty;

    private bool IsEditing => _QuestionId != Guid.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ExamId", out var examVal))
        {
            _examId = examVal switch
            {
                Guid g => g,
                string s => Guid.Parse(s),
                _ => Guid.Empty
            };
        }

        if (query.TryGetValue("QuestionId", out var secVal))
        {
            _QuestionId = secVal switch
            {
                Guid g => g,
                string s => Guid.Parse(s),
                _ => Guid.Empty
            };
        }
    }

    public override async Task Initialize()
    {
        if (IsEditing)
            await LoadExistingQuestionAsync();
    }

    private async Task LoadExistingQuestionAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var Question = await db.ExamQuestions
            .Include(s => s.GraphEntity)
                .ThenInclude(g => g.GraphRelations)
            .Include(s => s.GraphEntity)
                .ThenInclude(g => g.File)
            .FirstOrDefaultAsync(s => s.Id == _QuestionId);

        if (Question is null) return;

        QuestionText = Question.Question;

        SelectedQuestionType = Question.QuestionTypeEnum;
        IsDirected = Question.GraphEntity.IsDirected;

        Edges.Clear();
        foreach (var rel in Question.GraphEntity.GraphRelations)
            Edges.Add(new EdgeItem(rel.A, rel.B, rel.Weight ?? 0));

        // Load stored image
        if (Question.GraphEntity.File is not null && File.Exists(Question.GraphEntity.File.Path))
        {
            var bytes = await storageManager.LoadFileAsync(Question.GraphEntity.File.Path);
            _imageBytes = bytes;
            GraphImageSource = ImageSource.FromStream(() => new MemoryStream(bytes));
        }

        if (!string.IsNullOrEmpty(Question.AnswerObject))
        {
            _genericAnswers = JsonConvert.DeserializeObject<GenericQuestionAnswers>(Question.AnswerObject);
            if (_genericAnswers is not null)
                PopulateAnswerLines(_genericAnswers);
        }

        IsGenerated = true;
    }

    [RelayCommand]
    private void AddEdge()
    {
        var source = SourceVertex?.Trim().ToUpperInvariant();
        var target = TargetVertex?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            return;

        int.TryParse(_weightInput?.Trim(), out var weight);
        Edges.Add(new EdgeItem(source, target, weight));
        SourceVertex = string.Empty;
        TargetVertex = string.Empty;
        _weightInput = string.Empty;
        OnPropertyChanged("WeightInput");
    }

    [RelayCommand]
    private void RemoveEdge(EdgeItem? item)
    {
        if (item is not null)
            Edges.Remove(item);
    }

    [RelayCommand]
    private void EditGraph()
    {
        IsGenerated = false;
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        IsBusy = true;
        try
        {
            if (UseRandomGeneration)
            {
                if (!int.TryParse(VertexCount, out var count) || count < 2)
                {
                    await Shell.Current.DisplayAlert("Validation", "Enter a valid vertex count (minimum 2).", "OK");
                    return;
                }

                var needsWeights = SelectedQuestionType == QuestionTypeEnum.DIJKSTRA;
                var generated = await graphService.GenerateRandomGraphAsync(count, IsDirected, GetGraphType(SelectedQuestionType), needsWeights);

                Edges.Clear();
                foreach (var edge in generated.Edges)
                    Edges.Add(new EdgeItem(edge.Source, edge.Target, edge.Weight ?? 0));
            }

            if (Edges.Count == 0)
            {
                await Shell.Current.DisplayAlert("Validation", "Add at least one edge.", "OK");
                return;
            }

            var graph = new GraphEntity
            {
                IsDirected = IsDirected,
                GraphRelations = Edges.Select(e => new GraphRelation
                {
                    Id = Guid.NewGuid(),
                    A = e.Source,
                    B = e.Target,
                    Weight = e.Weight > 0 ? e.Weight : 0
                }).ToList()
            };

            _imageBytes = await graphService.GetGraphImageBytesAsync(graph);
            GraphImageSource = ImageSource.FromStream(() => new MemoryStream(_imageBytes));

            if (SelectedQuestionType == QuestionTypeEnum.DIJKSTRA)
            {
                var source = graph.GraphRelations.Select(r => r.A).First();
                var shortestPaths = await graphService.GetShortestPathsAsync(graph, source);
                _genericAnswers = QuestionAnswersMapper.FromShortestPaths(shortestPaths);
                QuestionText = QuestionTextProvider.GetQuestionText(SelectedQuestionType, IsDirected)
                    .Replace("početnog vrha", $"početnog vrha {source}");
            }
            else
            {
                _genericAnswers = SelectedQuestionType switch
                {
                    QuestionTypeEnum.ANALIZA_CENTRALNOSTI =>
                        QuestionAnswersMapper.FromCentralities(await graphService.GetCentralitiesAsync(graph)),
                    QuestionTypeEnum.ANALIZA_GRAFA =>
                        QuestionAnswersMapper.FromProperties(await graphService.GetPropertiesAsync(graph)),
                    QuestionTypeEnum.TOPOLOSKO_SORTIRANJE =>
                        QuestionAnswersMapper.FromTopologicalSort(await graphService.GetTopologicalSortAsync(graph)),
                    QuestionTypeEnum.CVRSTO_POVEZANE_KOMPONENTE =>
                        QuestionAnswersMapper.FromScc(await graphService.GetStronglyConnectedComponentsAsync(graph)),
                    _ => throw new InvalidOperationException($"Unsupported question type: {SelectedQuestionType}")
                };
            }
            PopulateAnswerLines(_genericAnswers);

            IsGenerated = true;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void PopulateAnswerLines(GenericQuestionAnswers answers)
    {
        AnswerLines.Clear();
        foreach (var line in answers.Lines)
            AnswerLines.Add(line);
    }

    private string SerializeAnswerObject() => JsonConvert.SerializeObject(_genericAnswers);

    private string GenerateQuestionTitle() => QuestionTextProvider.GetQuestionTitle(SelectedQuestionType);

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_genericAnswers is null || _imageBytes is null) return;

        IsBusy = true;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();

            if (IsEditing)
            {
                await UpdateExistingQuestionAsync(db);
            }
            else
            {
                await CreateNewQuestionAsync(db);
            }

            await db.SaveChangesAsync();
            await navigation.GoBackAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateNewQuestionAsync(ExamPlannerDbContext db)
    {
        var fileName = $"graph_{Guid.NewGuid()}.png";
        var imagePath = await storageManager.SaveFileAsync(_imageBytes!, fileName);

        var fileEntity = new FileEntity
        {
            Id = Guid.NewGuid(),
            Name = fileName,
            Path = imagePath
        };

        var graphEntity = new GraphEntity
        {
            IsDirected = IsDirected,
            FileId = fileEntity.Id,
            File = fileEntity,
            GraphRelations = Edges.Select(e => new GraphRelation
            {
                Id = Guid.NewGuid(),
                A = e.Source,
                B = e.Target,
                Weight = e.Weight > 0 ? e.Weight : 0
            }).ToList()
        };

        var Question = new ExamQuestion
        {
            Id = Guid.NewGuid(),
            ExamEntityId = _examId,
            GraphEntity = graphEntity,
            Title = GenerateQuestionTitle(),
            Question = QuestionText,
            QuestionTypeEnum = SelectedQuestionType,
            AnswerObject = SerializeAnswerObject()
        };

        db.ExamQuestions.Add(Question);
    }

    private async Task UpdateExistingQuestionAsync(ExamPlannerDbContext db)
    {
        var Question = await db.ExamQuestions
            .Include(s => s.GraphEntity)
                .ThenInclude(g => g.File)
            .FirstOrDefaultAsync(s => s.Id == _QuestionId);

        if (Question is null) return;

        // Update image file
        var fileName = Question.GraphEntity.File?.Name ?? $"graph_{Guid.NewGuid()}.png";
        var imagePath = await storageManager.SaveFileAsync(_imageBytes!, fileName);

        if (Question.GraphEntity.File is not null)
        {
            Question.GraphEntity.File.Path = imagePath;
        }
        else
        {
            var fileEntity = new FileEntity { Id = Guid.NewGuid(), Name = fileName, Path = imagePath };
            Question.GraphEntity.FileId = fileEntity.Id;
            Question.GraphEntity.File = fileEntity;
        }

        await db.GraphRelations
            .Where(r => r.GraphEntityId == Question.GraphEntity.Id)
            .ExecuteDeleteAsync();

        foreach (var edge in Edges)
        {
            db.GraphRelations.Add(new GraphRelation
            {
                Id = Guid.NewGuid(),
                A = edge.Source,
                B = edge.Target,
                Weight = edge.Weight > 0 ? edge.Weight : 0,
                GraphEntityId = Question.GraphEntity.Id
            });
        }

        Question.GraphEntity.IsDirected = IsDirected;
        Question.QuestionTypeEnum = SelectedQuestionType;
        Question.Question = QuestionText;
        Question.AnswerObject = SerializeAnswerObject();
    }

    private static RandomGraphRequestGraph_type GetGraphType(QuestionTypeEnum type) => type switch
    {
        QuestionTypeEnum.TOPOLOSKO_SORTIRANJE => RandomGraphRequestGraph_type.Dag,
        QuestionTypeEnum.CVRSTO_POVEZANE_KOMPONENTE => RandomGraphRequestGraph_type.Scc,
        _ => RandomGraphRequestGraph_type.Default
    };
}

public class EdgeItem(string source, string target, double weight = 0)
{
    public string Source { get; } = source;
    public string Target { get; } = target;
    public double Weight { get; } = weight;
    public string Display => $"{Source} — {Target}  (w: {Weight})";
}
