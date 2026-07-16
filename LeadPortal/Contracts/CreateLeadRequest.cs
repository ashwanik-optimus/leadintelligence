using LeadPortal.Models;

namespace LeadPortal.Contracts;

/// <summary>
/// Payload accepted by <c>POST /api/leads</c>. Validation is performed centrally in
/// <see cref="Services.LeadService.Validate"/> so the rules live in one place.
/// </summary>
public class CreateLeadRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public BudgetBand? BudgetBand { get; set; }
}
