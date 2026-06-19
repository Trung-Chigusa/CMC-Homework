namespace CmcHomework.Api.Models;

// File này chứa các DTO trả về cho client.
// Tách response thành record riêng giúp API rõ cấu trúc và dễ đọc hơn so với trả anonymous object ở mọi nơi.

// Response của POST /assets/batch.
// Created là số asset đã tạo, Ids là danh sách id vừa sinh ra.
public sealed record BatchCreateAssetsResponse(int Created, IReadOnlyList<string> Ids);

// Response của DELETE /assets/batch.
// Deleted là số id xóa được, NotFound là số id không tồn tại trong storage.
public sealed record BatchDeleteAssetsResponse(int Deleted, int NotFound);

// Response của GET /assets/stats.
// ByType và ByStatus là dictionary: key là tên nhóm, value là số lượng.
public sealed record AssetsStatsResponse(
    int Total,
    IReadOnlyDictionary<string, int> ByType,
    IReadOnlyDictionary<string, int> ByStatus);

// Response của GET /assets/count.
// Count là số lượng asset khớp điều kiện, Filters cho biết server đã áp dụng filter nào.
public sealed record CountAssetsResponse(int Count, CountFilters Filters);

// Phần mô tả filter trong CountAssetsResponse.
public sealed record CountFilters(string? Type, string? Status);

// Response của GET /assets.
// Data là danh sách asset của trang hiện tại, Pagination là thông tin phân trang.
public sealed record PagedAssetsResponse(IReadOnlyList<Asset> Data, PaginationInfo Pagination);

// Thông tin phân trang.
// Page: trang hiện tại.
// Limit: số item tối đa mỗi trang.
// Total: tổng số item sau khi filter.
// TotalPages: tổng số trang.
public sealed record PaginationInfo(int Page, int Limit, int Total, int TotalPages);

// Response của GET /health.
// Endpoint này giúp kiểm tra app đang chạy và storage hiện có bao nhiêu asset.
public sealed record HealthResponse(
    string Status,
    HealthStorageInfo Storage,
    long UptimeSeconds,
    DateTimeOffset Timestamp);

// Một phần nhỏ bên trong HealthResponse để mô tả storage.
public sealed record HealthStorageInfo(string Type, int AssetCount);
