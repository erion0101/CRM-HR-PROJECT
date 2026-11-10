namespace HrManagement.Infrastructure.Files.Options;

public sealed class ClamAvOptions
{
    public const string Section = "ClamAv";
    public string Host { get; set; } = "clamav";
    public int Port { get; set; } = 3310;
    public int TimeoutSeconds { get; set; } = 30;
}
