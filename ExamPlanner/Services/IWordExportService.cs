namespace ExamPlanner.Services;

public interface IWordExportService
{
    Task<WordExportResult> ExportExamAsync(Guid examId, string examTitle);
}

public record WordExportResult(string BlankPath, string SolutionsPath);
