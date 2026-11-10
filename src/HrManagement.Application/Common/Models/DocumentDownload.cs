namespace HrManagement.Application.Common.Models;

public sealed record DocumentDownload(string FileName, string ContentType, long Length, Stream Content);
