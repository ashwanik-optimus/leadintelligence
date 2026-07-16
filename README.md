# Lead Intelligence — Lead Distribution Portal

A lightweight **Lead Distribution Portal**: a public web form, a real-time internal
dashboard, and an ASP.NET Core backend that validates, persists, and routes leads to a
CRM. HubSpot is **mocked** behind a clean interface so the real OAuth + CRM integration
can drop in with no other code changes.

## Live demo

| | URL |
|---|---|
| Lead submission form | https://leadintel-portal-3d3ee34b.azurewebsites.net/ |
| Internal dashboard | https://leadintel-portal-3d3ee34b.azurewebsites.net/dashboard.html |
| Health probe | https://leadintel-portal-3d3ee34b.azurewebsites.net/health |

Hosted on Azure App Service (Linux). Free tier — first request after idle may cold-start.

## How it works

```
[Web form] ──POST──> [ASP.NET Core API] ──SignalR event──> [Live dashboard]
                            │
                            ├── validate + persist (EF Core / SQLite)
                            │
                            └── enqueue ──> [background sync worker] ──> [IHubSpotClient (mock)]
                                                    │
                                                    └── broadcast leadUpdated (Pending → Synced/Failed)
```

## Tech stack

| Layer | Choice |
|---|---|
| Backend | .NET 10 · ASP.NET Core Minimal API |
| Database | EF Core 10 + SQLite (file-based, zero setup) |
| Real-time | SignalR (`/hub/leads`) |
| Frontend | Plain HTML/CSS/JS — no build step |
| Hosting | Azure App Service (Linux) |

## Features

- Public form: first/last name, corporate email, company, budget band — client + server validation.
- Corporate-email-only rule (configurable free-domain block-list).
- Real-time dashboard: live lead feed, HubSpot router status, analytics badges — no manual refresh.
- Mocked CRM sync (`IHubSpotClient`) with simulated latency + failures, behind a background worker with retries.
- Config-driven: no hard-coded behavioural values; all in `appsettings.json` / env vars.
- Hardened: HTTPS-only, security headers, per-IP rate limiting, input length caps, SRI, patched dependencies.

## Quick start

```bash
cd LeadPortal
dotnet run
```

Then open **http://localhost:5100/** (form) and **http://localhost:5100/dashboard.html** (dashboard).
On first run EF Core creates `leads.db` and seeds sample leads.

## Documentation

Full details — API reference, configuration sections, security controls, architecture
notes, and how to swap the mock for the real HubSpot client — are in
**[`LeadPortal/README.md`](LeadPortal/README.md)**.

## Security

See the [Security section](LeadPortal/README.md#security) for the full list. Summary:
transport is HTTPS-only, responses carry a CSP and standard hardening headers, the public
submission endpoint is rate-limited per IP, input is length-capped and validated
server-side, and dependencies are scanned/patched.

> **Note:** authentication is intentionally **not** enabled in this demo — the dashboard
> and read APIs are open. Add Entra ID (or equivalent) before exposing real lead PII.

## Repository layout

```
LeadPortal/     ASP.NET Core application (see LeadPortal/README.md)
LICENSE
README.md       (this file)
```

## License

See [LICENSE](LICENSE).
