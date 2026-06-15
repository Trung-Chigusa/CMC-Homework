namespace CmcHomework.Api.Models;

public sealed record BatchCreateAssetsResponse(int Created, IReadOnlyList<string> Ids);

public sealed record BatchDeleteAssetsResponse(int Deleted, int NotFound);

public sealed record AssetsStatsResponse(
    int Total,
    IReadOnlyDictionary<string, int> ByType,
    IReadOnlyDictionary<string, int> ByStatus);

public sealed record CountAssetsResponse(int Count, CountFilters Filters);

public sealed record CountFilters(string? Type, string? Status);

public sealed record PagedAssetsResponse(IReadOnlyList<Asset> Data, PaginationInfo Pagination);

public sealed record PaginationInfo(int Page, int Limit, int Total, int TotalPages);

public sealed record HealthResponse(
    string Status,
    HealthStorageInfo Storage,
    long UptimeSeconds,
    DateTimeOffset Timestamp);

public sealed record HealthStorageInfo(string Type, int AssetCount);
