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
    IHubContext<LeadsHub> hub,
    ILeadSyncQueue syncQueue,
    IOptions<LeadValidationOptions> validationOptions,
    IOptions<BudgetValuationOptions> valuationOptions,
    ILogger<LeadService> logger)
{
    private readonly LeadValidationOptions _validation = validationOptions.Value;
    private readonly HashSet<string> _blockedDomains =
        new(validationOptions.Value.BlockedEmailDomains, StringComparer.OrdinalIgnoreCase);
    private readonly BudgetValuationOptions _valuation = valuationOptions.Value;

    public (bool valid, Dictionary<string, string[]> errors) Validate(CreateLeadRequest req)
    {
        var errors = new Dictionary<string, string[]>();

        ValidateRequiredText(errors, "firstName", req.FirstName, "First name");
        ValidateRequiredText(errors, "lastName", req.LastName, "Last name");
        ValidateRequiredText(errors, "companyName", req.CompanyName, "Company name");

        if (string.IsNullOrWhiteSpace(req.Email))
        {
            errors["email"] = ["Email is required."];
        }
        else if (req.Email.Length > _validation.MaxEmailLength)
        {
            errors["email"] = [$"Email must be {_validation.MaxEmailLength} characters or fewer."];
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
        else if (!Enum.IsDefined(req.BudgetBand.Value))
            errors["budgetBand"] = ["Unknown budget band."];

        return (errors.Count == 0, errors);
    }

    private void ValidateRequiredText(
        Dictionary<string, string[]> errors, string key, string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors[key] = [$"{label} is required."];
        else if (value.Length > _validation.MaxFieldLength)
            errors[key] = [$"{label} must be {_validation.MaxFieldLength} characters or fewer."];
    }

    public async Task<LeadDto> CreateAsync(CreateLeadRequest req, CancellationToken cancellationToken = default)
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
        await db.SaveChangesAsync(cancellationToken);

        var dto = LeadDto.From(lead);
        await hub.Clients.All.SendAsync("leadCreated", dto, cancellationToken);
        logger.LogInformation("Lead created {LeadId}", lead.Id);

        // Hand off CRM sync to the background worker (retries + own DI scope).
        await syncQueue.EnqueueAsync(lead.Id, cancellationToken);

        return dto;
    }

    public async Task<IReadOnlyList<LeadDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Leads
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => LeadDto.From(l))
            .ToListAsync(cancellationToken);

    public async Task<AnalyticsResponse> GetAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        // Aggregate in the database rather than materializing every row.
        var stats = await db.Leads
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalLeads = g.Count(),
                TotalPipelineValue = g.Sum(l => l.EstimatedValue),
                Synced = g.Count(l => l.HubSpotSyncStatus == HubSpotSyncStatus.Synced),
                Pending = g.Count(l => l.HubSpotSyncStatus == HubSpotSyncStatus.Pending),
                Failed = g.Count(l => l.HubSpotSyncStatus == HubSpotSyncStatus.Failed)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return stats is null
            ? new AnalyticsResponse(0, 0, 0, 0, 0)
            : new AnalyticsResponse(stats.TotalLeads, stats.TotalPipelineValue, stats.Synced, stats.Pending, stats.Failed);
    }
}
