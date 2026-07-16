using LeadPortal.Models;

namespace LeadPortal.Services;

public record HubSpotSyncResult(bool Success, string? ContactId, string? ErrorMessage);

public interface IHubSpotClient
{
    Task<HubSpotSyncResult> UpsertContactAsync(Lead lead);
}
