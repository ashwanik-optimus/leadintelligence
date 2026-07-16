# Lead Intelligence Portal

A lightweight **Lead Distribution Portal** — public web form, real-time internal dashboard, and ASP.NET Core backend with a mocked HubSpot CRM integration.

## Tech stack

| Layer | Choice |
|---|---|
| Backend | .NET 10 · ASP.NET Core Minimal API |
| Database | EF Core 10 + SQLite (file-based, zero setup) |
| Real-time | SignalR (`/hub/leads`) |
| Frontend | Plain HTML/CSS/JS — no build step |

## Running the app

```bash
cd LeadPortal
dotnet run
```

The app binds on **http://localhost:5100** by default (see `Properties/launchSettings.json`).

| URL | Purpose |
|---|---|
| `http://localhost:5100/` | Public lead submission form |
| `http://localhost:5100/dashboard.html` | Internal real-time dashboard |

On first run EF Core automatically creates `leads.db` (SQLite) and seeds three sample leads so the dashboard is never empty.

## API reference

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/leads` | Submit a new lead |
| `GET` | `/api/leads` | All leads, newest first |
| `GET` | `/api/analytics` | Counts + pipeline value |
| `GET` | `/api/integration/status` | HubSpot connection health |
| `GET` | `/health` | Liveness/readiness probe (checks DB connectivity) |
| WS | `/hub/leads` | SignalR hub — events: `leadCreated`, `leadUpdated` |

### `POST /api/leads` payload

```json
{
  "firstName":   "Jane",
  "lastName":    "Smith",
  "email":       "jane@yourcompany.com",
  "companyName": "Acme Corp",
  "budgetBand":  "Over50k"
}
```

`budgetBand` values: `Under10k` · `From10kTo50k` · `Over50k`

Corporate email only — the rejected free domains are configurable (see `LeadValidation.BlockedEmailDomains`).

## Configuration

All tunable behaviour lives in `appsettings.json` (overridable per-environment and via
environment variables using the `Section__Key` convention — e.g. `HubSpot__FailureRate`).
No behavioural values are hard-coded; each section is bound to a strongly-typed options
class under `Configuration/` and `HubSpot` is validated on startup.

| Section | Key(s) | Purpose |
|---|---|---|
| `ConnectionStrings` | `Default` | SQLite location. On Azure this is set to a persistent path (`Data Source=/home/data/leads.db`). |
| `HubSpot` | `Mode`, `StatusLabel`, `MinLatencyMs`, `MaxLatencyMs`, `FailureRate`, `MockContactIdPrefix`, `MaxSyncAttempts`, `RetryDelayMs`, `BaseUrl`, `AccessToken` | Integration mode + mock behaviour + retry policy + real-client credentials. |
| `LeadValidation` | `BlockedEmailDomains`, `MaxFieldLength`, `MaxEmailLength` | Corporate-email rule + input length caps. |
| `BudgetValuation` | `Under10k`, `From10kTo50k`, `Over50k` | Estimated pipeline value per budget band. |
| `Seeding` | `Enabled` | Whether to seed sample leads when the DB is empty. |
| `Cors` | `AllowedOrigins` | Cross-origin allow-list (empty ⇒ same-origin only). |

Example environment-variable override (no code or file changes):

```bash
HubSpot__FailureRate=0        # never fail the mock sync
BudgetValuation__Over50k=100000
Seeding__Enabled=false
```

## Security

| Control | Implementation |
|---|---|
| Transport | HTTPS-only enforced at Azure App Service; HTTP → HTTPS 301. |
| Response headers | CSP, `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy` (`Middleware/SecurityHeadersMiddleware`). |
| Rate limiting | Per-IP fixed window on `POST /api/leads` (5/min) via `AddRateLimiter`. |
| Client IP | `ForwardedHeaders` honours `X-Forwarded-For` behind Azure's proxy. |
| CORS | Deny-by-default; explicit allow-list only. |
| Input validation | Required fields, corporate-email rule, and length caps; all server-side. |
| CDN integrity | SignalR client pinned with SRI (`integrity` + `crossorigin`). |
| Injection / XSS | EF Core parameterization; dashboard HTML-escapes all lead fields. |
| Dependencies | `dotnet list package --vulnerable` clean; patched SQLite pinned (CVE-2025-6965). |

> **Not yet implemented:** authentication/authorization. The dashboard and read APIs are
> currently unauthenticated — add Entra ID (or equivalent) before exposing real lead PII.

## Architecture notes

- CRM sync is decoupled from the request via an in-process queue (`ILeadSyncQueue`) drained
  by a hosted `LeadSyncWorker` with bounded retries — each item runs in its own DI scope, so
  it never touches the request's `DbContext`. Swap the queue implementation for Azure Service
  Bus / Storage Queues to get at-least-once delivery across restarts.
- `GET /health` performs a real DB connectivity check for load-balancer probes.

## Project layout

```
Configuration/   strongly-typed options (HubSpot, LeadValidation, BudgetValuation, Seeding, Cors)
Contracts/       request/response DTOs (CreateLeadRequest, LeadDto, AnalyticsResponse)
Data/            AppDbContext + Seed
Endpoints/       HTTP API mapping (LeadApiEndpoints)
HealthChecks/    DatabaseHealthCheck
Hubs/            SignalR LeadsHub
Middleware/      SecurityHeadersMiddleware
Models/          Lead entity + enums
Services/        LeadService, IHubSpotClient, MockHubSpotClient, HubSpotClient (real stub),
                 ILeadSyncQueue, LeadSyncQueue, LeadSyncWorker (background sync)
wwwroot/         static SPA (index.html, dashboard.html)
```

## HubSpot integration — swapping Mock → Real

The integration is behind a single interface:

```csharp
// Services/IHubSpotClient.cs
public interface IHubSpotClient
{
    Task<HubSpotSyncResult> UpsertContactAsync(Lead lead);
}
```

**Current:** `MockHubSpotClient` — simulated latency and failure rate are configured under
the `HubSpot` section (defaults: 800–1500 ms latency, 15% failure rate).

**To wire up the real HubSpot CRM API:**

1. Create a Private App in your HubSpot Developer Sandbox and copy the access token.
2. Add the token to `appsettings.json` (or an environment variable):
   ```json
   "HubSpot": { "AccessToken": "pat-na1-xxxx" }
   ```
3. Open `Services/HubSpotClient.cs` and implement `RealHubSpotClient`:
   - `POST https://api.hubapi.com/crm/v3/objects/contacts` for new contacts.
   - `PATCH /crm/v3/objects/contacts/{id}?idProperty=email` to update by email (upsert).
   - Map the response `id` field to `HubSpotContactId`.
4. In `Program.cs`, swap the DI registration:
   ```diff
   - builder.Services.AddSingleton<IHubSpotClient, MockHubSpotClient>();
   + builder.Services.AddSingleton<IHubSpotClient, RealHubSpotClient>();
   ```

No other code changes needed — `LeadService` calls `IHubSpotClient` and handles both success and failure paths identically.
