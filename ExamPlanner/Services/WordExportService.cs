using Application.Repositories;
using Application.Storage;

using ExamPlanner.Services.Documents;

using Newtonsoft.Json;

namespace ExamPlanner.Services;

public class WordExportService(
    IQuestionRepository questionRepository,
    IWordDocumentBuilder docBuilder) : IWordExportService
{
    public async Task<string> ExportExamAsync(Guid examId, string examTitle)
    {
        var questions = (await questionRepository.FindInExamAsync(
                examId,
                includeGraphFile: true))
            .OrderBy(s => s.Title)
            .ToList();

        var safeTitle = string.Concat(examTitle.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(FileSystem.CacheDirectory, $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

        docBuilder.Begin();
        bool isFirst = true;

        foreach (var question in questions)
        {
            if (!isFirst)
                docBuilder.AddPageBreak();
            isFirst = false;

            docBuilder
                .AddQuestionText(question.Question)
                .AddBlankParagraph();

            if (question.GraphEntity?.File is not null && File.Exists(question.GraphEntity.File.Path))
            {
                var imageBytes = await File.ReadAllBytesAsync(question.GraphEntity.File.Path);
                docBuilder
                    .AddGraphImage(imageBytes)
                    .AddBlankParagraph();
            }

            var answers = string.IsNullOrEmpty(question.AnswerObject)
                ? null
                : JsonConvert.DeserializeObject<GenericQuestionAnswers>(question.AnswerObject);
            docBuilder.AddAnswerPlaceholders(answers);
        }

        await docBuilder.SaveAsync(filePath);
        return filePath;
    }
}
