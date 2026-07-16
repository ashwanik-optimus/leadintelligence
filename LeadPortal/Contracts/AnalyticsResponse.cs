namespace LeadPortal.Contracts;

public record AnalyticsResponse(
    int TotalLeads,
    decimal TotalPipelineValue,
    int SyncedCount,
    int PendingCount,
    int FailedCount
);
