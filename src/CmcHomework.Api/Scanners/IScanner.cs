using System.Text.Json.Nodes;
using CmcHomework.Api.Models;

namespace CmcHomework.Api.Scanners;

// Scanner là interface chung cho mọi loại scan.
// Mỗi scanner nhận một Asset và trả về danh sách result dạng JsonNode để mỗi scan_type có cấu trúc riêng.
public interface IScanner
{
    string ScanType { get; }

    IReadOnlySet<string> SupportedAssetTypes { get; }

    Task<IReadOnlyList<JsonNode>> ScanAsync(Asset asset, CancellationToken cancellationToken);
}
