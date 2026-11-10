namespace HrManagement.Domain;

public enum DocumentAiIndexStatus
{
    AwaitingApproval,
    Queued,
    Processing,
    Processed,
    NeedsOcr,
    NotEligible,
    Failed
}
