using LeadPortal.Configuration;
using LeadPortal.Models;
using Microsoft.Extensions.Options;

namespace LeadPortal.Services;

/// <summary>
/// Mock CRM client that simulates HubSpot latency and transient failures.
/// All tunable behaviour comes from <see cref="HubSpotOptions"/>.
/// </summary>
public class MockHubSpotClient(IOptions<HubSpotOptions> options, ILogger<MockHubSpotClient> logger) : IHubSpotClient
{
    private readonly HubSpotOptions _options = options.Value;

    public async Task<HubSpotSyncResult> UpsertContactAsync(Lead lead)
    {
        var delay = Random.Shared.Next(_options.MinLatencyMs, _options.MaxLatencyMs + 1);
        await Task.Delay(delay);

        if (Random.Shared.NextDouble() < _options.FailureRate)
        {
            logger.LogWarning("Mock HubSpot sync failed for lead {LeadId}", lead.Id);
            return new HubSpotSyncResult(false, null, "Mock simulated transient error");
        }

        var contactId = $"{_options.MockContactIdPrefix}{lead.Id.ToString()[..8]}";
        logger.LogInformation("Mock HubSpot synced lead {LeadId} → contact {ContactId}", lead.Id, contactId);
        return new HubSpotSyncResult(true, contactId, null);
    }
}
