namespace HrManagement.Infrastructure.Files.Options;

public sealed class StorageOptions
{
    public const string Section = "Storage";
    public string RootPath { get; set; } = "/data";
    public long MaxUploadBytes { get; set; } = 26_214_400;
}
