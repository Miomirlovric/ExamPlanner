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

namespace ExamPlanner.ViewModels;

public partial class SectionEditorViewModel(
    IDbContextFactory<ExamPlannerDbContext> dbFactory,
    IGraphAnalysisService graphService,
    IStorageManager storageManager,
    INavigationService navigation) : ViewModelBase, IQueryAttributable
{
    private Guid _examId;
    private Guid _sectionId;
    private CentralitiesResponse? _centralitiesResponse;
    private PropertiesResponse? _propertiesResponse;
    private byte[]? _imageBytes;

    // Input properties
    [ObservableProperty]
    private string _sourceVertex = string.Empty;

    [ObservableProperty]
    private string _targetVertex = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuestionText))]
    private bool _isDirected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuestionText))]
    [NotifyPropertyChangedFor(nameof(IsCentralityQuestion))]
    [NotifyPropertyChangedFor(nameof(IsGraphPropertiesQuestion))]
    [NotifyPropertyChangedFor(nameof(IsDirectedEnabled))]
    private QuestionTypeEnum _selectedQuestionType = QuestionTypeEnum.ANALIZA_CENTRALNOSTI;

    public List<QuestionTypeEnum> QuestionTypes { get; } = Enum.GetValues<QuestionTypeEnum>().ToList();

    public bool IsCentralityQuestion => SelectedQuestionType == QuestionTypeEnum.ANALIZA_CENTRALNOSTI;
    public bool IsGraphPropertiesQuestion => SelectedQuestionType == QuestionTypeEnum.ANALIZA_GRAFA;

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

    public string QuestionText => QuestionTextProvider.GetQuestionText(SelectedQuestionType, IsDirected);

    // Centrality results
    [ObservableProperty]
    private string _degreeVertices = string.Empty;

    [ObservableProperty]
    private string _degreeValue = string.Empty;

    [ObservableProperty]
    private string _betweennessVertices = string.Empty;

    [ObservableProperty]
    private string _betweennessValue = string.Empty;

    [ObservableProperty]
    private string _closenessVertices = string.Empty;

    [ObservableProperty]
    private string _closenessValue = string.Empty;

    // Graph properties results
    [ObservableProperty]
    private string _diameterValue = string.Empty;

    [ObservableProperty]
    private string _densityValue = string.Empty;

    [ObservableProperty]
    private string _maxDegreeVertices = string.Empty;

    [ObservableProperty]
    private string _maxDegreeValue = string.Empty;

    private bool IsEditing => _sectionId != Guid.Empty;

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

        if (query.TryGetValue("SectionId", out var secVal))
        {
            _sectionId = secVal switch
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
            await LoadExistingSectionAsync();
    }

    private async Task LoadExistingSectionAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var section = await db.ExamSections
            .Include(s => s.GraphEntity)
                .ThenInclude(g => g.GraphRelations)
            .Include(s => s.GraphEntity)
                .ThenInclude(g => g.File)
            .FirstOrDefaultAsync(s => s.Id == _sectionId);

        if (section is null) return;

        SelectedQuestionType = section.QuestionTypeEnum;
        IsDirected = section.GraphEntity.IsDirected;

        Edges.Clear();
        foreach (var rel in section.GraphEntity.GraphRelations)
            Edges.Add(new EdgeItem(rel.A, rel.B));

        // Load stored image
        if (section.GraphEntity.File is not null && File.Exists(section.GraphEntity.File.Path))
        {
            var bytes = await storageManager.LoadFileAsync(section.GraphEntity.File.Path);
            _imageBytes = bytes;
            GraphImageSource = ImageSource.FromStream(() => new MemoryStream(bytes));
        }

        // Load stored answer based on question type
        if (!string.IsNullOrEmpty(section.AnswerObject))
        {
            if (section.QuestionTypeEnum == QuestionTypeEnum.ANALIZA_CENTRALNOSTI)
            {
                _centralitiesResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<CentralitiesResponse>(section.AnswerObject);
                if (_centralitiesResponse is not null)
                    PopulateCentralityFields(_centralitiesResponse);
            }
            else if (section.QuestionTypeEnum == QuestionTypeEnum.ANALIZA_GRAFA)
            {
                _propertiesResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<PropertiesResponse>(section.AnswerObject);
                if (_propertiesResponse is not null)
                    PopulatePropertiesFields(_propertiesResponse);
            }
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

        Edges.Add(new EdgeItem(source, target));
        SourceVertex = string.Empty;
        TargetVertex = string.Empty;
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

                var generated = await graphService.GenerateRandomGraphAsync(count, IsDirected);

                Edges.Clear();
                foreach (var edge in generated.Edges)
                    Edges.Add(new EdgeItem(edge.Source, edge.Target));
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
                    B = e.Target
                }).ToList()
            };

            _imageBytes = await graphService.GetGraphImageBytesAsync(graph);
            GraphImageSource = ImageSource.FromStream(() => new MemoryStream(_imageBytes));

            if (SelectedQuestionType == QuestionTypeEnum.ANALIZA_CENTRALNOSTI)
            {
                _centralitiesResponse = await graphService.GetCentralitiesAsync(graph);
                _propertiesResponse = null;
                PopulateCentralityFields(_centralitiesResponse);
            }
            else
            {
                _propertiesResponse = await graphService.GetPropertiesAsync(graph);
                _centralitiesResponse = null;
                PopulatePropertiesFields(_propertiesResponse);
            }

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

    private void PopulateCentralityFields(CentralitiesResponse response)
    {
        var c = response.Centralities;
        DegreeVertices = string.Join(", ", c.Degree.Vertices);
        DegreeValue = c.Degree.Value.ToString("F3");
        BetweennessVertices = string.Join(", ", c.Betweenness.Vertices);
        BetweennessValue = c.Betweenness.Value.ToString("F3");
        ClosenessVertices = string.Join(", ", c.Closeness.Vertices);
        ClosenessValue = c.Closeness.Value.ToString("F3");
    }

    private void PopulatePropertiesFields(PropertiesResponse response)
    {
        var p = response.Properties;
        DiameterValue = Newtonsoft.Json.JsonConvert.SerializeObject(p.Diameter).Trim('"');
        DensityValue = p.Density.ToString("F3");
        MaxDegreeVertices = string.Join(", ", p.Max_degree.Vertices);
        MaxDegreeValue = p.Max_degree.Value.ToString();
    }

    private string SerializeAnswerObject() => SelectedQuestionType switch
    {
        QuestionTypeEnum.ANALIZA_CENTRALNOSTI => Newtonsoft.Json.JsonConvert.SerializeObject(_centralitiesResponse),
        QuestionTypeEnum.ANALIZA_GRAFA => Newtonsoft.Json.JsonConvert.SerializeObject(_propertiesResponse),
        _ => string.Empty
    };

    private string GenerateSectionTitle() => QuestionTextProvider.GetSectionTitle(SelectedQuestionType);

    [RelayCommand]
    private async Task SaveAsync()
    {
        if ((_centralitiesResponse is null && _propertiesResponse is null) || _imageBytes is null) return;

        IsBusy = true;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();

            if (IsEditing)
            {
                await UpdateExistingSectionAsync(db);
            }
            else
            {
                await CreateNewSectionAsync(db);
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

    private async Task CreateNewSectionAsync(ExamPlannerDbContext db)
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
                B = e.Target
            }).ToList()
        };

        var section = new ExamSection
        {
            Id = Guid.NewGuid(),
            ExamEntityId = _examId,
            GraphEntity = graphEntity,
            Title = GenerateSectionTitle(),
            Question = QuestionText,
            QuestionTypeEnum = SelectedQuestionType,
            AnswerObject = SerializeAnswerObject()
        };

        db.ExamSections.Add(section);
    }

    private async Task UpdateExistingSectionAsync(ExamPlannerDbContext db)
    {
        var section = await db.ExamSections
            .Include(s => s.GraphEntity)
                .ThenInclude(g => g.File)
            .FirstOrDefaultAsync(s => s.Id == _sectionId);

        if (section is null) return;

        // Update image file
        var fileName = section.GraphEntity.File?.Name ?? $"graph_{Guid.NewGuid()}.png";
        var imagePath = await storageManager.SaveFileAsync(_imageBytes!, fileName);

        if (section.GraphEntity.File is not null)
        {
            section.GraphEntity.File.Path = imagePath;
        }
        else
        {
            var fileEntity = new FileEntity { Id = Guid.NewGuid(), Name = fileName, Path = imagePath };
            section.GraphEntity.FileId = fileEntity.Id;
            section.GraphEntity.File = fileEntity;
        }

        await db.GraphRelations
            .Where(r => r.GraphEntityId == section.GraphEntity.Id)
            .ExecuteDeleteAsync();

        foreach (var edge in Edges)
        {
            db.GraphRelations.Add(new GraphRelation
            {
                Id = Guid.NewGuid(),
                A = edge.Source,
                B = edge.Target,
                GraphEntityId = section.GraphEntity.Id
            });
        }

        section.GraphEntity.IsDirected = IsDirected;
        section.QuestionTypeEnum = SelectedQuestionType;
        section.Question = QuestionText;
        section.AnswerObject = SerializeAnswerObject();
    }
}

public class EdgeItem(string source, string target)
{
    public string Source { get; } = source;
    public string Target { get; } = target;
    public string Display => $"{Source} — {Target}";
}
