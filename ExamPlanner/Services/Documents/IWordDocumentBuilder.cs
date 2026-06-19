using Application.Storage;

namespace ExamPlanner.Services.Documents;

public interface IWordDocumentBuilder
{
    IWordDocumentBuilder Begin();
    IWordDocumentBuilder AddPageBreak();
    IWordDocumentBuilder AddQuestionText(string text);
    IWordDocumentBuilder AddGraphImage(byte[] pngBytes);
    IWordDocumentBuilder AddAnswerPlaceholders(GenericQuestionAnswers? answers, bool withSolutions = false);
    IWordDocumentBuilder AddBlankParagraph();
    Task SaveAsync(string filePath);
}
