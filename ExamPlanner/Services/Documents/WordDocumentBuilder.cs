using Application.Storage;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Pic = DocumentFormat.OpenXml.Drawing.Pictures;
using WBorder = DocumentFormat.OpenXml.Wordprocessing.Border;

namespace ExamPlanner.Services.Documents;

public sealed class WordDocumentBuilder : IWordDocumentBuilder
{
    private readonly List<Action<MainDocumentPart, Body>> _operations = [];
    private uint _imageCounter = 1;

    public IWordDocumentBuilder Begin()
    {
        // Operations are queued and replayed in SaveAsync, so the document
        // is only opened once we know the destination file path.
        _operations.Clear();
        _imageCounter = 1;
        return this;
    }

    public IWordDocumentBuilder AddPageBreak()
    {
        _operations.Add((_, body) =>
            body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page }))));
        return this;
    }

    public IWordDocumentBuilder AddQuestionText(string text)
    {
        _operations.Add((_, body) => body.AppendChild(CreateTextParagraph(text)));
        return this;
    }

    public IWordDocumentBuilder AddBlankParagraph()
    {
        _operations.Add((_, body) => body.AppendChild(new Paragraph()));
        return this;
    }

    public IWordDocumentBuilder AddGraphImage(byte[] pngBytes)
    {
        _operations.Add((mainPart, body) =>
        {
            body.AppendChild(CreateImageParagraph(mainPart, pngBytes, _imageCounter++));
        });
        return this;
    }

    public IWordDocumentBuilder AddAnswerPlaceholders(GenericQuestionAnswers? answers, bool withSolutions = false)
    {
        if (answers is null) return this;
        _operations.Add((_, body) =>
        {
            foreach (var line in answers.Lines)
            {
                body.AppendChild(BuildAnswerLine(line, withSolutions));
            }
        });
        return this;
    }

    public Task SaveAsync(string filePath)
    {
        using var doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = new Body();
        mainPart.Document.AppendChild(body);

        foreach (var op in _operations)
            op(mainPart, body);

        body.AppendChild(new SectionProperties());
        mainPart.Document.Save();
        return Task.CompletedTask;
    }

    private static Paragraph CreateTextParagraph(string text)
    {
        return new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "160" }),
            new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

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

    private static Paragraph BuildAnswerLine(AnswerLine line, bool withSolutions)
    {
        var para = new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "200" }));
        foreach (var seg in line.Segments)
        {
            Run run;
            if (seg.Type == SegmentType.Placeholder)
                run = withSolutions ? SolutionText(seg.DisplayValue) : Box();
            else
                run = Text(seg.Text);
            para.AppendChild(run);
        }
        return para;
    }

    private static Run Text(string value) =>
        new(new Text($" {value} ") { Space = SpaceProcessingModeValues.Preserve });

    private static Run SolutionText(string value) =>
        new(new RunProperties(new Bold()),
            new Text($" {value} ") { Space = SpaceProcessingModeValues.Preserve });

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
