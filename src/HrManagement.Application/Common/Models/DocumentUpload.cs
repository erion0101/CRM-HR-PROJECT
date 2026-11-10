namespace HrManagement.Application.Common.Models;

public sealed record DocumentUpload(string FileName, string ContentType, long Length, Stream Content, string DocumentType);
