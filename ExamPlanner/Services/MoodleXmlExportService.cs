using Application.Storage;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence;
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

    private static string BuildCdataContent(ExamQuestion question, string imageName)
    {
        var sb = new StringBuilder();
        sb.Append($"<p>{question.Question}</p>\n");
        sb.Append($"<p><img src=\"@@PLUGINFILE@@/{imageName}\"></p>\n");

        if (!string.IsNullOrEmpty(question.AnswerObject))
        {
            var answers = Newtonsoft.Json.JsonConvert.DeserializeObject<GenericQuestionAnswers>(question.AnswerObject);
            if (answers is not null)
                AppendAnswerLines(sb, answers);
        }

        return sb.ToString();
    }

    private static void AppendAnswerLines(StringBuilder sb, GenericQuestionAnswers answers)
    {
        foreach (var line in answers.Lines)
        {
            sb.Append("<p>");
            foreach (var seg in line.Segments)
            {
                if (seg.Type == SegmentType.Text)
                {
                    sb.Append(seg.Text);
                }
                else
                {
                    var moodleFormat = seg.CorrectAnswers.Length == 1
                        ? $"={seg.CorrectAnswers[0]}"
                        : string.Join("~", seg.CorrectAnswers.Select(a => $"%100%{a}"));
                    sb.Append($"{{1:SHORTANSWER_C:{moodleFormat}}}");
                }
            }
            sb.Append("</p>\n");
        }
    }

    private static string BuildVertexAnswers(IEnumerable<string> vertices)
        => string.Join("~", vertices.Select(v => $"%100%{v}"));

    private static void SaveXml(XElement quiz, string filePath)
    {
        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), quiz);
        doc.Save(filePath);
    }
}
