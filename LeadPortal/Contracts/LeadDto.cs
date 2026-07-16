using System.Text.Json.Serialization;
using LeadPortal.Models;

namespace LeadPortal.Contracts;

/// <summary>
/// Public representation of a <see cref="Lead"/>. Wire names are pinned with
/// <see cref="JsonPropertyNameAttribute"/> so the API/SignalR contract is stable and
/// independent of serializer casing settings.
/// </summary>
public record LeadDto(
    [property: JsonPropertyName("id")]                Guid Id,
    [property: JsonPropertyName("firstName")]         string FirstName,
    [property: JsonPropertyName("lastName")]          string LastName,
    [property: JsonPropertyName("email")]             string Email,
    [property: JsonPropertyName("companyName")]       string CompanyName,
    [property: JsonPropertyName("budgetBand")]        string BudgetBand,
    [property: JsonPropertyName("estimatedValue")]    decimal EstimatedValue,
    [property: JsonPropertyName("localStatus")]       string LocalStatus,
    [property: JsonPropertyName("hubSpotSyncStatus")] string HubSpotSyncStatus,
    [property: JsonPropertyName("hubSpotContactId")]  string? HubSpotContactId,
    [property: JsonPropertyName("createdAt")]         DateTime CreatedAt)
{
    public static LeadDto From(Lead l) => new(
        l.Id,
        l.FirstName,
        l.LastName,
        l.Email,
        l.CompanyName,
        l.BudgetBand.ToString(),
        l.EstimatedValue,
        l.LocalStatus.ToString(),
        l.HubSpotSyncStatus.ToString(),
        l.HubSpotContactId,
        l.CreatedAt);
}
