using CmcHomework.Api.Models;

namespace CmcHomework.Api.Storage;

// Interface lưu trữ cho scan jobs và scan results.
// Tách riêng khỏi IAssetStorage để phần asset CRUD không bị trộn với phần scan.
public interface IScanStorage
{
    ScanJob CreateJob(ScanJob job);

    ScanJob? GetJobById(string id);

    IReadOnlyList<ScanJob> GetJobsByAssetId(string assetId);

    void UpdateJob(ScanJob job);

    void ReplaceResults(string jobId, IReadOnlyList<StoredScanResult> results);

    IReadOnlyList<StoredScanResult> GetResultsByJobId(string jobId);

    IReadOnlyList<StoredScanResult> GetResultsByAssetId(string assetId);
}
