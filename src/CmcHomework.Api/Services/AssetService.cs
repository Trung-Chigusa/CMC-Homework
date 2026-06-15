using System.ComponentModel.DataAnnotations;
using CmcHomework.Api.Models;
using CmcHomework.Api.Storage;

namespace CmcHomework.Api.Services;

public sealed class AssetService : IAssetService
{
    private const int MaxBatchSize = 100;
    private const int DefaultPage = 1;
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;
    private const int MaxSearchResults = 100;

    private readonly IAssetStorage _storage;

    public AssetService(IAssetStorage storage)
    {
        _storage = storage;
    }

    public Asset Create(CreateAssetRequest request)
    {
        var asset = BuildAssetAfterValidation(request);
        return _storage.Create(asset);
    }

    public BatchCreateAssetsResponse BatchCreate(BatchCreateAssetsRequest request)
    {
        if (request.Assets is null || request.Assets.Count == 0)
        {
            throw new ValidationException("Danh sách assets không được để trống.");
        }

        if (request.Assets.Count > MaxBatchSize)
        {
            throw new ValidationException($"Mỗi request chỉ được tạo tối đa {MaxBatchSize} assets.");
        }

        // All-or-nothing: validate toàn bộ danh sách trước, chưa ghi gì vào storage.
        foreach (var input in request.Assets)
        {
            ValidateCreateInput(input);
        }

        var assets = request.Assets
            .Select(BuildAssetFromValidInput)
            .ToList();

        var created = _storage.BatchCreate(assets);
        return new BatchCreateAssetsResponse(created.Count, created.Select(asset => asset.Id).ToList());
    }

    public BatchDeleteAssetsResponse BatchDelete(string? idsParam)
    {
        if (string.IsNullOrWhiteSpace(idsParam))
        {
            throw new ValidationException("Query parameter 'ids' là bắt buộc.");
        }

        var ids = idsParam
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (ids.Count == 0)
        {
            throw new ValidationException("Cần truyền ít nhất 1 id hợp lệ trong query parameter 'ids'.");
        }

        return _storage.BatchDelete(ids);
    }

    public Asset? GetById(string id)
    {
        return _storage.GetById(id);
    }

    public PagedAssetsResponse List(int? page, int? limit, string? type, string? status)
    {
        var pageValue = page ?? DefaultPage;
        var limitValue = limit ?? DefaultLimit;

        if (pageValue < 1)
        {
            throw new ValidationException("page phải lớn hơn hoặc bằng 1.");
        }

        if (limitValue < 1 || limitValue > MaxLimit)
        {
            throw new ValidationException($"limit phải nằm trong khoảng 1 đến {MaxLimit}.");
        }

        ValidateOptionalFilters(type, status);

        var filtered = FilterAssets(_storage.GetAll(), type, status).ToList();
        var total = filtered.Count;
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)limitValue);

        var data = filtered
            .Skip((pageValue - 1) * limitValue)
            .Take(limitValue)
            .ToList();

        return new PagedAssetsResponse(
            data,
            new PaginationInfo(pageValue, limitValue, total, totalPages));
    }

    public AssetsStatsResponse GetStats()
    {
        var assets = _storage.GetAll();

        var byType = AssetRules.AllowedTypes.ToDictionary(type => type, _ => 0);
        var byStatus = AssetRules.AllowedStatuses.ToDictionary(status => status, _ => 0);

        foreach (var asset in assets)
        {
            byType[asset.Type]++;
            byStatus[asset.Status]++;
        }

        return new AssetsStatsResponse(assets.Count, byType, byStatus);
    }

    public CountAssetsResponse Count(string? type, string? status)
    {
        ValidateOptionalFilters(type, status);

        var count = FilterAssets(_storage.GetAll(), type, status).Count();
        var filters = new CountFilters(NormalizeOrNull(type), NormalizeOrNull(status));

        return new CountAssetsResponse(count, filters);
    }

    public IReadOnlyList<Asset> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ValidationException("Query parameter 'q' là bắt buộc.");
        }

        var trimmedQuery = query.Trim();

        return _storage.GetAll()
            .Where(asset => asset.Name.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase))
            .Take(MaxSearchResults)
            .ToList();
    }

    public int CountAll()
    {
        return _storage.Count();
    }

    private static IEnumerable<Asset> FilterAssets(IEnumerable<Asset> assets, string? type, string? status)
    {
        var normalizedType = NormalizeOrNull(type);
        var normalizedStatus = NormalizeOrNull(status);

        return assets.Where(asset =>
            (normalizedType is null || asset.Type == normalizedType) &&
            (normalizedStatus is null || asset.Status == normalizedStatus));
    }

    private static Asset BuildAssetAfterValidation(CreateAssetRequest request)
    {
        ValidateCreateInput(request);
        return BuildAssetFromValidInput(request);
    }

    private static Asset BuildAssetFromValidInput(CreateAssetRequest request)
    {
        return new Asset(
            Guid.NewGuid().ToString(),
            request.Name!.Trim(),
            request.Type!.Trim().ToLowerInvariant(),
            NormalizeOrNull(request.Status) ?? AssetRules.DefaultStatus,
            DateTimeOffset.UtcNow);
    }

    private static void ValidateCreateInput(CreateAssetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("name là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            throw new ValidationException("type là bắt buộc.");
        }

        if (!AssetRules.IsValidType(request.Type))
        {
            throw new ValidationException($"type chỉ nhận các giá trị: {AssetRules.TypeText}.");
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && !AssetRules.IsValidStatus(request.Status))
        {
            throw new ValidationException($"status chỉ nhận các giá trị: {AssetRules.StatusText}.");
        }
    }

    private static void ValidateOptionalFilters(string? type, string? status)
    {
        if (!string.IsNullOrWhiteSpace(type) && !AssetRules.IsValidType(type))
        {
            throw new ValidationException($"type filter chỉ nhận các giá trị: {AssetRules.TypeText}.");
        }

        if (!string.IsNullOrWhiteSpace(status) && !AssetRules.IsValidStatus(status))
        {
            throw new ValidationException($"status filter chỉ nhận các giá trị: {AssetRules.StatusText}.");
        }
    }

    private static string? NormalizeOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    }
}
