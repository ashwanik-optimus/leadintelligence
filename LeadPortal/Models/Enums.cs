namespace LeadPortal.Models;

public enum BudgetBand
{
    Under10k,
    From10kTo50k,
    Over50k
}

public enum LocalStatus
{
    Received,
    Validated,
    Failed
}

public enum HubSpotSyncStatus
{
    Pending,
    Synced,
    Failed
}
