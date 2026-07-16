using LeadPortal.Configuration;
using LeadPortal.Contracts;
using LeadPortal.Services;
using Microsoft.Extensions.Options;

namespace LeadPortal.Endpoints;

/// <summary>
/// Maps the public HTTP API. Keeps routing separate from service and data concerns.
/// </summary>
public static class LeadApiEndpoints
{
    /// <summary>Rate-limit policy name for the public submission endpoint.</summary>
    public const string SubmitPolicy = "submit";

    public static IEndpointRouteBuilder MapLeadApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapPost("/leads", async (CreateLeadRequest req, LeadService svc, CancellationToken ct) =>
        {
            var (valid, errors) = svc.Validate(req);
            if (!valid) return Results.ValidationProblem(errors);

            var lead = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/leads/{lead.Id}", lead);
        }).RequireRateLimiting(SubmitPolicy);

        api.MapGet("/leads", async (LeadService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAllAsync(ct)));

        api.MapGet("/analytics", async (LeadService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAnalyticsAsync(ct)));

        api.MapGet("/integration/status", (IOptions<HubSpotOptions> hubSpot) =>
        {
            var opt = hubSpot.Value;
            return Results.Ok(new
            {
                connected = true,
                mode = opt.Mode.ToLowerInvariant(),
                label = opt.StatusLabel
            });
        });

        return app;
    }
}
