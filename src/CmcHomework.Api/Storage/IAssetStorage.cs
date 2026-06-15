using CmcHomework.Api.Models;

namespace CmcHomework.Api.Storage;

public interface IAssetStorage
{
    Asset Create(Asset asset);

    IReadOnlyList<Asset> BatchCreate(IReadOnlyList<Asset> assets);

    BatchDeleteAssetsResponse BatchDelete(IReadOnlyList<string> ids);

    Asset? GetById(string id);

    IReadOnlyList<Asset> GetAll();

    int Count();
}
