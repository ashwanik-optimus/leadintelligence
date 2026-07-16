using LeadPortal.Models;

namespace LeadPortal.Data;

public static class Seed
{
    public static async Task RunAsync(AppDbContext db)
    {
        if (db.Leads.Any()) return;

        var leads = new[]
        {
            new Lead
            {
                FirstName = "Sarah", LastName = "Chen",
                Email = "s.chen@quantumtech.io", CompanyName = "Quantum Technologies",
                BudgetBand = BudgetBand.Over50k, EstimatedValue = 75_000,
                LocalStatus = LocalStatus.Validated, HubSpotSyncStatus = HubSpotSyncStatus.Synced,
                HubSpotContactId = "hs-mock-001", CreatedAt = DateTime.UtcNow.AddHours(-3)
            },
            new Lead
            {
                FirstName = "Marcus", LastName = "Rivera",
                Email = "m.rivera@blueridge-capital.com", CompanyName = "Blue Ridge Capital",
                BudgetBand = BudgetBand.From10kTo50k, EstimatedValue = 30_000,
                LocalStatus = LocalStatus.Validated, HubSpotSyncStatus = HubSpotSyncStatus.Synced,
                HubSpotContactId = "hs-mock-002", CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Lead
            {
                FirstName = "Priya", LastName = "Nair",
                Email = "priya.nair@novahealth.org", CompanyName = "Nova Health Systems",
                BudgetBand = BudgetBand.Under10k, EstimatedValue = 5_000,
                LocalStatus = LocalStatus.Validated, HubSpotSyncStatus = HubSpotSyncStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30)
            }
        };

        db.Leads.AddRange(leads);
        await db.SaveChangesAsync();
    }
}
