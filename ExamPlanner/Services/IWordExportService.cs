namespace ExamPlanner.Services;

public interface IWordExportService
{
    Task<string> ExportExamAsync(Guid examId, string examTitle);
}
