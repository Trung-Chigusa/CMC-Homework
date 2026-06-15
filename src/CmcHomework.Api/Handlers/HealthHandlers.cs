using CmcHomework.Api.Models;
using CmcHomework.Api.Services;

namespace CmcHomework.Api.Handlers;

public static class HealthHandlers
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", GetHealth);
        return app;
    }

    private static IResult GetHealth(IAssetService service, AppLifetime lifetime)
    {
        var now = DateTimeOffset.UtcNow;

        var response = new HealthResponse(
            "ok",
            new HealthStorageInfo("in-memory", service.CountAll()),
            lifetime.GetUptimeSeconds(now),
            now);

        return Results.Ok(response);
    }
}
