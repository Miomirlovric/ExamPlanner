using Application.ExternalApi;
using Domain.Entities;
using Domain.Values;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace ExamPlanner.Services;

public class MoodleXmlExportService(IDbContextFactory<ExamPlannerDbContext> dbFactory) : IMoodleXmlExportService
{
    public async Task<string> ExportExamAsync(Guid examId, string examTitle)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var Questions = await db.ExamQuestions
            .Include(s => s.GraphEntity)
                .ThenInclude(g => g.File)
            .Where(s => s.ExamEntityId == examId)
            .OrderBy(s => s.Title)
            .ToListAsync();

        var quiz = new XElement("quiz");
        foreach (var Question in Questions)
            quiz.Add(BuildQuestionElement(Question));

        var safeTitle = string.Concat(examTitle.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(FileSystem.CacheDirectory, $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
        SaveXml(quiz, filePath);
        return filePath;
    }

    public async Task<string> ExportQuestionAsync(Guid QuestionId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var Question = await db.ExamQuestions
            .Include(s => s.GraphEntity)
                .ThenInclude(g => g.File)
            .FirstOrDefaultAsync(s => s.Id == QuestionId);

        if (Question is null) throw new InvalidOperationException("Question not found.");

        var quiz = new XElement("quiz", BuildQuestionElement(Question));
        var safeTitle = string.Concat(Question.Title.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(FileSystem.CacheDirectory, $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
        SaveXml(quiz, filePath);
        return filePath;
    }

    private static XElement BuildQuestionElement(ExamQuestion Question)
    {
        var imageName = $"graph_{Question.Id}.png";
        var imageBase64 = GetImageBase64(Question.GraphEntity?.File?.Path);
        var cdataContent = BuildCdataContent(Question, imageName);

        var questionTextEl = new XElement("questiontext",
            new XAttribute("format", "html"),
            new XElement("text", new XCData(cdataContent)));

        if (imageBase64 is not null)
            questionTextEl.Add(new XElement("file",
                new XAttribute("name", imageName),
                new XAttribute("path", "/"),
                new XAttribute("encoding", "base64"),
                imageBase64));

        var feedbackEl = new XElement("generalfeedback",
            new XAttribute("format", "html"),
            new XElement("text", ""));

        return new XElement("question",
            new XAttribute("type", "cloze"),
            new XElement("name", new XElement("text", Question.Title)),
            questionTextEl,
            feedbackEl,
            new XElement("penalty", "0.3333333"),
            new XElement("hidden", "0"),
            new XElement("idnumber", ""));
    }

    private static string? GetImageBase64(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        return Convert.ToBase64String(File.ReadAllBytes(path));
    }

    private static string BuildCdataContent(ExamQuestion Question, string imageName)
    {
        var sb = new StringBuilder();
        sb.Append($"<p>{Question.Question}</p>\n");
        sb.Append($"<p><img src=\"@@PLUGINFILE@@/{imageName}\"></p>\n");

        switch (Question.QuestionTypeEnum)
        {
            case QuestionTypeEnum.ANALIZA_GRAFA:
                AppendGrafaAnswers(sb, Question.AnswerObject);
                break;
            case QuestionTypeEnum.ANALIZA_CENTRALNOSTI:
                AppendCentralnostiAnswers(sb, Question.AnswerObject);
                break;
        }

        return sb.ToString();
    }

    private static void AppendGrafaAnswers(StringBuilder sb, string? answerJson)
    {
        if (string.IsNullOrEmpty(answerJson)) return;
        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<PropertiesResponse>(answerJson);
        if (response is null) return;

        var p = response.Properties;
        var vertexAnswers = BuildVertexAnswers(p.Max_degree.Vertices);
        var density = p.Density.ToString("F3", CultureInfo.InvariantCulture);

        sb.Append($"<p>Dijametar grafa: {{1:SHORTANSWER_C:={p.Diameter}}}.</p>\n");
        sb.Append($"<p>Gustoca grafa: {{1:SHORTANSWER_C:={density}}}.</p>\n");
        sb.Append($"<p>Najveci stupanj ima vrh {{1:SHORTANSWER_C:{vertexAnswers}}} te iznosi: {{1:SHORTANSWER_C:={p.Max_degree.Value}}}.</p>\n");
    }

    private static void AppendCentralnostiAnswers(StringBuilder sb, string? answerJson)
    {
        if (string.IsNullOrEmpty(answerJson)) return;
        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<CentralitiesResponse>(answerJson);
        if (response is null) return;

        var c = response.Centralities;
        var degreeVertices = BuildVertexAnswers(c.Degree.Vertices);
        var betweennessVertices = BuildVertexAnswers(c.Betweenness.Vertices);
        var closenessVertices = BuildVertexAnswers(c.Closeness.Vertices);
        var degreeVal = c.Degree.Value.ToString("F3", CultureInfo.InvariantCulture);
        var betweennessVal = c.Betweenness.Value.ToString("F3", CultureInfo.InvariantCulture);
        var closenessVal = c.Closeness.Value.ToString("F3", CultureInfo.InvariantCulture);

        sb.Append($"<p>Najvecu centralnost stupnja ima vrh {{1:SHORTANSWER_C:{degreeVertices}}} te iznosi: {{1:SHORTANSWER_C:={degreeVal}}}.</p>\n");
        sb.Append($"<p>Najvecu centralnost medupoloženosti ima vrh {{1:SHORTANSWER_C:{betweennessVertices}}} te iznosi: {{1:SHORTANSWER_C:={betweennessVal}}}.</p>\n");
        sb.Append($"<p>Najvecu centralnost bliskosti ima vrh {{1:SHORTANSWER_C:{closenessVertices}}} te iznosi: {{1:SHORTANSWER_C:={closenessVal}}}.</p>\n");
    }

    private static string BuildVertexAnswers(IEnumerable<string> vertices)
        => string.Join("~", vertices.Select(v => $"%100%{v}"));

    private static void SaveXml(XElement quiz, string filePath)
    {
        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), quiz);
        doc.Save(filePath);
    }
}
