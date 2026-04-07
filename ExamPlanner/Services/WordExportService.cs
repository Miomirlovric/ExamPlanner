using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Domain.Values;
using Microsoft.EntityFrameworkCore;
using Persistence;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Pic = DocumentFormat.OpenXml.Drawing.Pictures;
using WBorder = DocumentFormat.OpenXml.Wordprocessing.Border;

namespace ExamPlanner.Services;

public class WordExportService(IDbContextFactory<ExamPlannerDbContext> dbFactory) : IWordExportService
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

        var safeTitle = string.Concat(examTitle.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(FileSystem.CacheDirectory, $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

        using var wordDoc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
        var mainPart = wordDoc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = new Body();
        mainPart.Document.AppendChild(body);

        uint imageCounter = 1;
        bool isFirst = true;

        foreach (var Question in Questions)
        {
            if (!isFirst)
                body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
            isFirst = false;

            // Question text
            body.AppendChild(CreateTextParagraph(Question.Question));
            body.AppendChild(new Paragraph());

            // Graph image
            if (Question.GraphEntity?.File is not null && File.Exists(Question.GraphEntity.File.Path))
            {
                var imageBytes = await File.ReadAllBytesAsync(Question.GraphEntity.File.Path);
                body.AppendChild(CreateImageParagraph(mainPart, imageBytes, imageCounter++));
                body.AppendChild(new Paragraph());
            }

            // Empty answer rows
            foreach (var para in BuildAnswerRows(Question.QuestionTypeEnum))
                body.AppendChild(para);
        }

        body.AppendChild(new SectionProperties());
        mainPart.Document.Save();

        return filePath;
    }

    // ── Text ────────────────────────────────────────────────────────────────

    private static Paragraph CreateTextParagraph(string text)
    {
        return new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { After = "160" }),
            new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    // ── Image ───────────────────────────────────────────────────────────────

    private static Paragraph CreateImageParagraph(MainDocumentPart mainPart, byte[] imageBytes, uint id)
    {
        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (var ms = new MemoryStream(imageBytes))
            imagePart.FeedData(ms);

        var (imgW, imgH) = GetPngDimensions(imageBytes);
        const long targetWidthEmu = 5029200L; // ~5.5 inches
        long targetHeightEmu = imgW > 0
            ? (long)((double)targetWidthEmu * imgH / imgW)
            : 3000000L;

        var rid = mainPart.GetIdOfPart(imagePart);

        var drawing = new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = targetWidthEmu, Cy = targetHeightEmu },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties { Id = id, Name = $"Graph{id}" },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new Pic.Picture(
                            new Pic.NonVisualPictureProperties(
                                new Pic.NonVisualDrawingProperties { Id = 0U, Name = "graph.png" },
                                new Pic.NonVisualPictureDrawingProperties()),
                            new Pic.BlipFill(
                                new A.Blip { Embed = rid, CompressionState = A.BlipCompressionValues.Print },
                                new A.Stretch(new A.FillRectangle())),
                            new Pic.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = targetWidthEmu, Cy = targetHeightEmu }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }))
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
            )
            { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });

        return new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
            new Run(drawing));
    }

    private static (int width, int height) GetPngDimensions(byte[] bytes)
    {
        if (bytes.Length < 24) return (800, 600);
        int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        return (w > 0 && h > 0) ? (w, h) : (800, 600);
    }

    // ── Answer rows ─────────────────────────────────────────────────────────

    private static IEnumerable<Paragraph> BuildAnswerRows(QuestionTypeEnum questionType) =>
        questionType switch
        {
            QuestionTypeEnum.ANALIZA_GRAFA =>
            [
                AnswerLine([Text("Dijametar grafa:"), Box(), Text(".")]),
                AnswerLine([Text("Gustoća grafa:"), Box(), Text(".")]),
                AnswerLine([Text("Najveći stupanj ima vrh"), Box(), Text("te iznosi:"), Box(), Text(".")])
            ],
            QuestionTypeEnum.ANALIZA_CENTRALNOSTI =>
            [
                AnswerLine([Text("Najveću centralnost stupnja ima vrh"), Box(), Text("te iznosi:"), Box(), Text(".")]),
                AnswerLine([Text("Najveću centralnost međupoloženosti ima vrh"), Box(), Text("te iznosi:"), Box(), Text(".")]),
                AnswerLine([Text("Najveću centralnost bliskosti ima vrh"), Box(), Text("te iznosi:"), Box(), Text(".")])
            ],
            _ => []
        };

    private static Paragraph AnswerLine(IEnumerable<Run> runs)
    {
        var para = new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "200" }));
        foreach (var run in runs)
            para.AppendChild(run);
        return para;
    }

    private static Run Text(string value) =>
        new(new Text($" {value} ") { Space = SpaceProcessingModeValues.Preserve });

    private static Run Box() =>
        new(new RunProperties(
                new WBorder
                {
                    Val = BorderValues.Single,
                    Size = 6U,
                    Space = 1U,
                    Color = "000000"
                }),
            new Text("           ") { Space = SpaceProcessingModeValues.Preserve });
}
