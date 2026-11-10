namespace HrManagement.Application.Features.Documents;

public static class DocumentContentTypes
{
    public static bool IsSupported(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) || extension.Equals(".docx", StringComparison.OrdinalIgnoreCase);
    }

    public static string FromFileName(string fileName) => Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
        ? "application/pdf"
        : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
}
