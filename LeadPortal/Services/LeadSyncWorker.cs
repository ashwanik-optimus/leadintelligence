using LeadPortal.Configuration;
using LeadPortal.Contracts;
using LeadPortal.Data;
using LeadPortal.Hubs;
using LeadPortal.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace LeadPortal.Services;

/// <summary>
/// Background consumer that syncs queued leads to the (mock) CRM with bounded retries,
/// persists the terminal status, and broadcasts <c>leadUpdated</c> to the dashboard.
/// Each lead is processed in its own DI scope so it never shares the request's DbContext.
/// </summary>
public class LeadSyncWorker(
    ILeadSyncQueue queue,
    IServiceScopeFactory scopeFactory,
    IHubContext<LeadsHub> hub,
    IOptions<HubSpotOptions> options,
    ILogger<LeadSyncWorker> logger) : BackgroundService
{
    private readonly HubSpotOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Lead sync worker started.");

        await foreach (var leadId in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await SyncAsync(leadId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // graceful shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error syncing lead {LeadId}", leadId);
            }
        }
    }

    private async Task SyncAsync(Guid leadId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hubSpot = scope.ServiceProvider.GetRequiredService<IHubSpotClient>();

        var lead = await db.Leads.FindAsync([leadId], cancellationToken);
        if (lead is null)
        {
            logger.LogWarning("Lead {LeadId} not found; skipping sync.", leadId);
            return;
        }

        HubSpotSyncResult? result = null;
        for (var attempt = 1; attempt <= _options.MaxSyncAttempts; attempt++)
        {
            try
            {
                result = await hubSpot.UpsertContactAsync(lead, cancellationToken);
                if (result.Success) break;
                logger.LogWarning("HubSpot sync attempt {Attempt}/{Max} failed for lead {LeadId}: {Error}",
                    attempt, _options.MaxSyncAttempts, leadId, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "HubSpot sync attempt {Attempt}/{Max} threw for lead {LeadId}",
                    attempt, _options.MaxSyncAttempts, leadId);
            }

            if (attempt < _options.MaxSyncAttempts)
                await Task.Delay(TimeSpan.FromMilliseconds(_options.RetryDelayMs * attempt), cancellationToken);
        }

        lead.HubSpotSyncStatus = result?.Success == true ? HubSpotSyncStatus.Synced : HubSpotSyncStatus.Failed;
        lead.HubSpotContactId = result?.ContactId;

        await db.SaveChangesAsync(cancellationToken);
        await hub.Clients.All.SendAsync("leadUpdated", LeadDto.From(lead), cancellationToken);

        logger.LogInformation("Lead {LeadId} sync finished: {Status}", leadId, lead.HubSpotSyncStatus);
    }
}
