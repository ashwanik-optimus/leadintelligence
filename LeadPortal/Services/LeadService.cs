using LeadPortal.Configuration;
using LeadPortal.Contracts;
using LeadPortal.Data;
using LeadPortal.Hubs;
using LeadPortal.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeadPortal.Services;

public class LeadService(
    AppDbContext db,
    IHubSpotClient hubSpot,
    IHubContext<LeadsHub> hub,
    IServiceScopeFactory scopeFactory,
    IOptions<LeadValidationOptions> validationOptions,
    IOptions<BudgetValuationOptions> valuationOptions,
    ILogger<LeadService> logger)
{
    private readonly HashSet<string> _blockedDomains =
        new(validationOptions.Value.BlockedEmailDomains, StringComparer.OrdinalIgnoreCase);
    private readonly BudgetValuationOptions _valuation = valuationOptions.Value;

    public (bool valid, Dictionary<string, string[]> errors) Validate(CreateLeadRequest req)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(req.FirstName))
            errors["firstName"] = ["First name is required."];
        if (string.IsNullOrWhiteSpace(req.LastName))
            errors["lastName"] = ["Last name is required."];
        if (string.IsNullOrWhiteSpace(req.CompanyName))
            errors["companyName"] = ["Company name is required."];

        if (string.IsNullOrWhiteSpace(req.Email))
        {
            errors["email"] = ["Email is required."];
        }
        else
        {
            var atIdx = req.Email.IndexOf('@');
            if (atIdx < 1 || atIdx == req.Email.Length - 1)
            {
                errors["email"] = ["Invalid email address."];
            }
            else
            {
                var domain = req.Email[(atIdx + 1)..];
                if (_blockedDomains.Contains(domain))
                    errors["email"] = ["Please use a corporate email address."];
            }
        }

        if (req.BudgetBand is null)
            errors["budgetBand"] = ["Budget band is required."];

        return (errors.Count == 0, errors);
    }

    public async Task<Lead> CreateAsync(CreateLeadRequest req)
    {
        var lead = new Lead
        {
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            Email = req.Email.Trim().ToLowerInvariant(),
            CompanyName = req.CompanyName.Trim(),
            BudgetBand = req.BudgetBand!.Value,
            EstimatedValue = _valuation.ValueFor(req.BudgetBand!.Value),
            LocalStatus = LocalStatus.Validated,
            HubSpotSyncStatus = HubSpotSyncStatus.Pending
        };

        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        await hub.Clients.All.SendAsync("leadCreated", ToDto(lead));
        logger.LogInformation("Lead created {LeadId}", lead.Id);

        _ = SyncToHubSpotAsync(lead.Id);

        return lead;
    }

    private async Task SyncToHubSpotAsync(Guid leadId)
    {
        // Fire-and-forget: this runs after the HTTP request (and its DI scope) has
        // ended, so it must own a fresh scope instead of the disposed request-scoped
        // DbContext captured by this service.
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var lead = await db.Leads.FindAsync(leadId);
        if (lead is null) return;

        try
        {
            var result = await hubSpot.UpsertContactAsync(lead);
            lead.HubSpotSyncStatus = result.Success ? HubSpotSyncStatus.Synced : HubSpotSyncStatus.Failed;
            lead.HubSpotContactId = result.ContactId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HubSpot sync exception for lead {LeadId}", leadId);
            lead.HubSpotSyncStatus = HubSpotSyncStatus.Failed;
        }

        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("leadUpdated", ToDto(lead));
    }

    public async Task<IEnumerable<object>> GetAllAsync() =>
        await db.Leads.OrderByDescending(l => l.CreatedAt)
                      .Select(l => (object)ToDto(l))
                      .ToListAsync();

    public async Task<AnalyticsResponse> GetAnalyticsAsync()
    {
        var leads = await db.Leads.ToListAsync();
        return new AnalyticsResponse(
            TotalLeads: leads.Count,
            TotalPipelineValue: leads.Sum(l => l.EstimatedValue),
            SyncedCount: leads.Count(l => l.HubSpotSyncStatus == HubSpotSyncStatus.Synced),
            PendingCount: leads.Count(l => l.HubSpotSyncStatus == HubSpotSyncStatus.Pending),
            FailedCount: leads.Count(l => l.HubSpotSyncStatus == HubSpotSyncStatus.Failed)
        );
    }

    private static object ToDto(Lead l) => new
    {
        id = l.Id,
        firstName = l.FirstName,
        lastName = l.LastName,
        email = l.Email,
        companyName = l.CompanyName,
        budgetBand = l.BudgetBand.ToString(),
        estimatedValue = l.EstimatedValue,
        localStatus = l.LocalStatus.ToString(),
        hubSpotSyncStatus = l.HubSpotSyncStatus.ToString(),
        hubSpotContactId = l.HubSpotContactId,
        createdAt = l.CreatedAt
    };
}
