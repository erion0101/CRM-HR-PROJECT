using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HrManagement.Application.Features.Ai;
using UglyToad.PdfPig;

namespace HrManagement.Infrastructure.Ai;

internal sealed class LocalDocumentTextExtractor : IDocumentTextExtractor
{
    public bool Supports(string contentType, string fileName) =>
        contentType is "application/pdf" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
        Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase) ||
        Path.GetExtension(fileName).Equals(".docx", StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<ExtractedDocumentSection>> ExtractAsync(Stream stream, string contentType, string fileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (contentType == "application/pdf" || Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<IReadOnlyList<ExtractedDocumentSection>>(ExtractPdf(stream, cancellationToken));
        return Task.FromResult<IReadOnlyList<ExtractedDocumentSection>>(ExtractDocx(stream, cancellationToken));
    }

    private static IReadOnlyList<ExtractedDocumentSection> ExtractPdf(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        using var document = PdfDocument.Open(stream);
        var sections = new List<ExtractedDocumentSection>(document.NumberOfPages);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = Normalize(page.Text);
            if (text.Length > 0) sections.Add(new ExtractedDocumentSection(text, page.Number, null, null));
        }
        return sections;
    }

    private static IReadOnlyList<ExtractedDocumentSection> ExtractDocx(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var sections = new List<ExtractedDocumentSection>();
        var paragraphs = document.MainDocumentPart?.Document?.Body?.Descendants<Paragraph>() ?? [];
        var index = 0;
        string? heading = null;
        foreach (var paragraph in paragraphs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            index++;
            var text = Normalize(paragraph.InnerText);
            if (text.Length == 0) continue;
            var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            if (style?.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) == true) heading = text;
            sections.Add(new ExtractedDocumentSection(text, null, index, heading));
        }
        return sections;
    }

    private static string Normalize(string value) => string.Join(' ', value
        .Replace('\0', ' ')
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
