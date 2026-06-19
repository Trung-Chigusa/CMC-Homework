using CmcHomework.Api.Models;

namespace CmcHomework.Api.Services;

// Interface cho nghiệp vụ scan.
// Handler dùng interface này để start job, xem status và lấy results.
public interface IScanService
{
    ScanJob StartScan(string assetId, StartScanRequest request);

    ScanJob? GetJobById(string id);

    IReadOnlyList<ScanJob> GetJobsByAssetId(string assetId);

    ScanResultsResponse GetResultsByJobId(string jobId);

    AssetResultsResponse GetResultsByAssetId(string assetId);

    AssetResultsResponse GetTypedResultsByAssetId(string assetId, string scanType);
}
