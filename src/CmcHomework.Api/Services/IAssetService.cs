using CmcHomework.Api.Models;

namespace CmcHomework.Api.Services;

public interface IAssetService
{
    Asset Create(CreateAssetRequest request);

    BatchCreateAssetsResponse BatchCreate(BatchCreateAssetsRequest request);

    BatchDeleteAssetsResponse BatchDelete(string? idsParam);

    Asset? GetById(string id);

    PagedAssetsResponse List(int? page, int? limit, string? type, string? status);

    AssetsStatsResponse GetStats();

    CountAssetsResponse Count(string? type, string? status);

    IReadOnlyList<Asset> Search(string? query);

    int CountAll();
}
