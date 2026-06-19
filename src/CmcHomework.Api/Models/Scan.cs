using System.Text.Json.Nodes;

namespace CmcHomework.Api.Models;

// Request body của POST /assets/{id}/scan.
// Ví dụ JSON: { "scan_type": "dns" }
public sealed record StartScanRequest(string? ScanType);

// ScanJob là một job scan đang chờ chạy, đang chạy, đã xong hoặc lỗi.
// Client nhận id của job rồi dùng id đó để hỏi status/result sau.
public sealed record ScanJob(
    string Id,
    string AssetId,
    string ScanType,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string Error,
    int Results,
    DateTimeOffset CreatedAt);

// StoredScanResult là bản ghi thô lưu trong database.
// DataJson giữ JSON result nguyên dạng để mỗi loại scan có cấu trúc riêng.
public sealed record StoredScanResult(
    string Id,
    string JobId,
    string AssetId,
    string ScanType,
    string DataJson,
    DateTimeOffset CreatedAt);

// Response của GET /scan-jobs/{id}/results.
public sealed record ScanResultsResponse(
    string JobId,
    string ScanType,
    IReadOnlyList<JsonNode> Results);

// Một nhóm kết quả scan của một job thuộc một asset.
public sealed record AssetScanResultsResponse(
    string JobId,
    string ScanType,
    string Status,
    IReadOnlyList<JsonNode> Results);

// Response của GET /assets/{id}/results và các endpoint typed results.
public sealed record AssetResultsResponse(
    string AssetId,
    IReadOnlyList<AssetScanResultsResponse> Scans);
