namespace LeadPortal.Models;

public class Lead
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public BudgetBand BudgetBand { get; set; }
    public decimal EstimatedValue { get; set; }
    public LocalStatus LocalStatus { get; set; }
    public HubSpotSyncStatus HubSpotSyncStatus { get; set; }
    public string? HubSpotContactId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
