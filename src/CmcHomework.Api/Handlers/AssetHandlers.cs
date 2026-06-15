using System.ComponentModel.DataAnnotations;
using CmcHomework.Api.Models;
using CmcHomework.Api.Services;

namespace CmcHomework.Api.Handlers;

public static class AssetHandlers
{
    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/assets");

        group.MapPost("", CreateAsset);
        group.MapPost("/batch", BatchCreateAssets);
        group.MapDelete("/batch", BatchDeleteAssets);
        group.MapGet("", ListAssets);
        group.MapGet("/stats", GetStats);
        group.MapGet("/count", CountAssets);
        group.MapGet("/search", SearchAssets);
        group.MapGet("/{id}", GetAssetById);

        return app;
    }

    private static IResult CreateAsset(CreateAssetRequest request, IAssetService service)
    {
        try
        {
            var asset = service.Create(request);
            return Results.Created($"/assets/{asset.Id}", asset);
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new ApiError(exception.Message));
        }
    }

    private static IResult BatchCreateAssets(BatchCreateAssetsRequest request, IAssetService service)
    {
        try
        {
            var response = service.BatchCreate(request);
            return Results.Created("/assets/batch", response);
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new ApiError(exception.Message));
        }
    }

    private static IResult BatchDeleteAssets(string? ids, IAssetService service)
    {
        try
        {
            return Results.Ok(service.BatchDelete(ids));
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new ApiError(exception.Message));
        }
    }

    private static IResult ListAssets(int? page, int? limit, string? type, string? status, IAssetService service)
    {
        try
        {
            return Results.Ok(service.List(page, limit, type, status));
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new ApiError(exception.Message));
        }
    }

    private static IResult GetStats(IAssetService service)
    {
        return Results.Ok(service.GetStats());
    }

    private static IResult CountAssets(string? type, string? status, IAssetService service)
    {
        try
        {
            return Results.Ok(service.Count(type, status));
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new ApiError(exception.Message));
        }
    }

    private static IResult SearchAssets(string? q, IAssetService service)
    {
        try
        {
            return Results.Ok(service.Search(q));
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new ApiError(exception.Message));
        }
    }

    private static IResult GetAssetById(string id, IAssetService service)
    {
        var asset = service.GetById(id);
        return asset is null ? Results.NotFound(new ApiError("Không tìm thấy asset.")) : Results.Ok(asset);
    }
}
