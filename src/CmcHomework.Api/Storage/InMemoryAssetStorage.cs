using CmcHomework.Api.Models;

namespace CmcHomework.Api.Storage;

public sealed class InMemoryAssetStorage : IAssetStorage, IDisposable
{
    private readonly Dictionary<string, Asset> _assets = new(StringComparer.OrdinalIgnoreCase);
    private readonly ReaderWriterLockSlim _lock = new();

    public Asset Create(Asset asset)
    {
        _lock.EnterWriteLock();

        try
        {
            _assets.Add(asset.Id, asset);
            return asset;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IReadOnlyList<Asset> BatchCreate(IReadOnlyList<Asset> assets)
    {
        _lock.EnterWriteLock();

        try
        {
            // Ghi cả batch trong cùng 1 write lock để tránh request khác chen vào giữa.
            foreach (var asset in assets)
            {
                if (_assets.ContainsKey(asset.Id))
                {
                    throw new InvalidOperationException($"Asset id '{asset.Id}' đã tồn tại.");
                }
            }

            foreach (var asset in assets)
            {
                _assets.Add(asset.Id, asset);
            }

            return assets.ToList();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public BatchDeleteAssetsResponse BatchDelete(IReadOnlyList<string> ids)
    {
        _lock.EnterWriteLock();

        try
        {
            var deleted = 0;
            var notFound = 0;

            foreach (var id in ids)
            {
                if (_assets.Remove(id))
                {
                    deleted++;
                }
                else
                {
                    notFound++;
                }
            }

            return new BatchDeleteAssetsResponse(deleted, notFound);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Asset? GetById(string id)
    {
        _lock.EnterReadLock();

        try
        {
            return _assets.GetValueOrDefault(id);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IReadOnlyList<Asset> GetAll()
    {
        _lock.EnterReadLock();

        try
        {
            // Trả về snapshot để code bên ngoài không dùng trực tiếp Dictionary đang được lock.
            return _assets.Values
                .OrderBy(asset => asset.CreatedAt)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public int Count()
    {
        _lock.EnterReadLock();

        try
        {
            return _assets.Count;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
