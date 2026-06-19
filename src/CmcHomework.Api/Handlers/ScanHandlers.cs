using System.ComponentModel.DataAnnotations;
using CmcHomework.Api.Models;
using CmcHomework.Api.Services;

namespace CmcHomework.Api.Handlers;

// Handler cho các endpoint scan trong yêu cầu Day 3.
// File này chỉ lo HTTP status code; phần chạy scanner nằm trong ScanService.
public static class ScanHandlers
{
    public static IEndpointRouteBuilder MapScanEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/assets/{id}/scan", StartScan);
        app.MapGet("/scan-jobs/{id}", GetScanJob);
        app.MapGet("/scan-jobs/{id}/results", GetScanResults);
        app.MapGet("/assets/{id}/scans", GetAssetScans);
        app.MapGet("/assets/{id}/results", GetAssetResults);
        app.MapGet("/assets/{id}/dns", (string id, IScanService service) => GetTypedAssetResults(id, ScanRules.Dns, service));
        app.MapGet("/assets/{id}/whois", (string id, IScanService service) => GetTypedAssetResults(id, ScanRules.Whois, service));
        app.MapGet("/assets/{id}/subdomains", (string id, IScanService service) => GetTypedAssetResults(id, ScanRules.Subdomain, service));

        return app;
    }

    private static IResult StartScan(string id, StartScanRequest request, IScanService service)
    {
        try
        {
            var job = service.StartScan(id, request);
            return Results.Accepted($"/scan-jobs/{job.Id}", job);
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new ApiError(exception.Message));
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new ApiError(exception.Message));
        }
    }

    private static IResult GetScanJob(string id, IScanService service)
    {
        var job = service.GetJobById(id);
        return job is null ? Results.NotFound(new ApiError("Không tìm thấy scan job.")) : Results.Ok(job);
    }

    private static IResult GetScanResults(string id, IScanService service)
    {
        try
        {
            return Results.Ok(service.GetResultsByJobId(id));
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new ApiError(exception.Message));
        }
    }

    private static IResult GetAssetScans(string id, IScanService service)
    {
        try
        {
            return Results.Ok(service.GetJobsByAssetId(id));
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new ApiError(exception.Message));
        }
    }

    private static IResult GetAssetResults(string id, IScanService service)
    {
        try
        {
            return Results.Ok(service.GetResultsByAssetId(id));
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new ApiError(exception.Message));
        }
    }

    private static IResult GetTypedAssetResults(string id, string scanType, IScanService service)
    {
        try
        {
            return Results.Ok(service.GetTypedResultsByAssetId(id, scanType));
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new ApiError(exception.Message));
        }
    }
}
