namespace ExamPlanner.Services;

public interface IMoodleXmlExportService
{
    Task<string> ExportExamAsync(Guid examId, string examTitle);
    Task<string> ExportSectionAsync(Guid sectionId);
}
