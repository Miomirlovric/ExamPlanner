using Application.Repositories;
using Application.Storage;

using Domain.Entities;

using ExamPlanner.Services.Documents;

using Newtonsoft.Json;

namespace ExamPlanner.Services;

public class WordExportService(
    IQuestionRepository questionRepository,
    IWordDocumentBuilder docBuilder) : IWordExportService
{
    public async Task<WordExportResult> ExportExamAsync(Guid examId, string examTitle)
    {
        var questions = (await questionRepository.FindInExamAsync(
                examId,
                includeGraphFile: true))
            .OrderBy(s => s.Title)
            .ToList();

        var safeTitle = string.Concat(examTitle.Split(Path.GetInvalidFileNameChars()));
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        var blankPath = Path.Combine(FileSystem.CacheDirectory, $"{safeTitle}_{timestamp}.docx");
        var solutionsPath = Path.Combine(FileSystem.CacheDirectory, $"{safeTitle}_{timestamp}_rjesenja.docx");

        await BuildDocumentAsync(questions, blankPath, withSolutions: false);
        await BuildDocumentAsync(questions, solutionsPath, withSolutions: true);

        return new WordExportResult(blankPath, solutionsPath);
    }

    private async Task BuildDocumentAsync(IReadOnlyList<ExamQuestion> questions, string filePath, bool withSolutions)
    {
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
            docBuilder.AddAnswerPlaceholders(answers, withSolutions);
        }

        await docBuilder.SaveAsync(filePath);
    }
}
