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
    public static IEndpointRouteBuilder MapLeadApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapPost("/leads", async (CreateLeadRequest req, LeadService svc) =>
        {
            var (valid, errors) = svc.Validate(req);
            if (!valid) return Results.ValidationProblem(errors);

            var lead = await svc.CreateAsync(req);
            return Results.Created($"/api/leads/{lead.Id}", lead);
        });

        api.MapGet("/leads", async (LeadService svc) =>
            Results.Ok(await svc.GetAllAsync()));

        api.MapGet("/analytics", async (LeadService svc) =>
            Results.Ok(await svc.GetAnalyticsAsync()));

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
