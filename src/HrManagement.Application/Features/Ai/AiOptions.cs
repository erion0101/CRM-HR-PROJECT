namespace HrManagement.Application.Features.Ai;

public sealed class AiOptions
{
    public const string Section = "Ai";
    public bool Enabled { get; set; }
    public bool RealDataApproved { get; set; }
    public string Provider { get; set; } = "OpenAI";
    public string ChatModel { get; set; } = "gpt-6.1-sol";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public int EmbeddingDimensions { get; set; } = 1536;
    public string ReasoningEffort { get; set; } = "low";
    public string OrganizationTimeZone { get; set; } = "Europe/Belgrade";
    public int RequestTimeoutSeconds { get; set; } = 45;
    /// <summary>Timeout for background work such as onboarding quiz generation, which produces much longer output.</summary>
    public int BackgroundRequestTimeoutSeconds { get; set; } = 180;
    public int MaxToolRounds { get; set; } = 4;
    public int MaxToolCallsPerTurn { get; set; } = 6;
    public int MaxStructuredRows { get; set; } = 100;
    public int MaxRetrievalChunks { get; set; } = 8;
    public int MaxInputTokens { get; set; } = 16_000;
    public int MaxOutputTokens { get; set; } = 1_500;
    public int ConversationRetentionDays { get; set; } = 90;
    public int RequestsPerMinutePerUser { get; set; } = 10;
    public int RequestsPerDayPerUser { get; set; } = 50;
    public int RequestsPerDayPerOrganization { get; set; } = 500;
    public int ConcurrentRequestsPerUser { get; set; } = 2;
    public decimal MonthlyBudgetUsd { get; set; } = 50m;
    public decimal InputCostPerMillionUsd { get; set; }
    public decimal OutputCostPerMillionUsd { get; set; }
}
