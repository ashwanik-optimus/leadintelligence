using LeadPortal.Models;

namespace LeadPortal.Services;

// TODO: real HubSpot OAuth + upsert
// Steps to implement:
//   1. Register a private app in HubSpot Developer Sandbox → obtain access token.
//   2. Inject IHttpClientFactory; configure a named HttpClient with base URL
//      https://api.hubapi.com and Authorization: Bearer <token> header.
//   3. POST /crm/v3/objects/contacts with properties: email, firstname, lastname,
//      company, and a custom property for budget_band.
//   4. Use PATCH /crm/v3/objects/contacts/{id} for updates (upsert by email via
//      the idProperty=email query param).
//   5. Map the response id to HubSpotContactId and return HubSpotSyncResult.
//   6. Register RealHubSpotClient instead of MockHubSpotClient in Program.cs DI.
public class RealHubSpotClient : IHubSpotClient
{
    public Task<HubSpotSyncResult> UpsertContactAsync(Lead lead) =>
        throw new NotImplementedException("Replace with real HubSpot OAuth + CRM API calls.");
}
