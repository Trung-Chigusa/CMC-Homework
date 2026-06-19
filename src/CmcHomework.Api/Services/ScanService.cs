using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using CmcHomework.Api.Models;
using CmcHomework.Api.Scanners;
using CmcHomework.Api.Storage;

namespace CmcHomework.Api.Services;

// ScanService xử lý toàn bộ workflow scan:
// start job -> chạy scanner nền -> lưu result -> cập nhật status.
public sealed class ScanService : IScanService
{
    private readonly IAssetStorage _assetStorage;
    private readonly IScanStorage _scanStorage;
    private readonly IReadOnlyList<IScanner> _scanners;
    private readonly ILogger<ScanService> _logger;

    public ScanService(
        IAssetStorage assetStorage,
        IScanStorage scanStorage,
        IEnumerable<IScanner> scanners,
        ILogger<ScanService> logger)
    {
        _assetStorage = assetStorage;
        _scanStorage = scanStorage;
        _scanners = scanners.ToList();
        _logger = logger;
    }

    public ScanJob StartScan(string assetId, StartScanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ScanType))
        {
            throw new ValidationException("scan_type là bắt buộc.");
        }

        var scanType = request.ScanType.Trim().ToLowerInvariant();
        if (!ScanRules.IsValidScanType(scanType))
        {
            throw new ValidationException($"scan_type chỉ nhận các giá trị: {ScanRules.ScanTypeText}.");
        }

        var asset = _assetStorage.GetById(assetId)
            ?? throw new KeyNotFoundException("Không tìm thấy asset để scan.");

        var scanners = ResolveScanners(scanType, asset);
        if (scanners.Count == 0)
        {
            throw new ValidationException($"scan_type '{scanType}' không hỗ trợ asset type '{asset.Type}'.");
        }

        if (scanType == ScanRules.Port)
        {
            ScanSafety.EnsurePortScanAllowed(asset.Name);
        }

        var now = DateTimeOffset.UtcNow;
        var job = new ScanJob(
            Guid.NewGuid().ToString(),
            asset.Id,
            scanType,
            ScanRules.StatusPending,
            now,
            null,
            "",
            0,
            now);

        _scanStorage.CreateJob(job);

        // Chạy nền để endpoint POST trả 202 Accepted nhanh.
        // Nếu scanner lỗi, ExecuteJobAsync sẽ bắt lỗi và lưu status failed/partial.
        _ = Task.Run(() => ExecuteJobAsync(job.Id, asset, scanners, CancellationToken.None));

        return job;
    }

    public ScanJob? GetJobById(string id)
    {
        return _scanStorage.GetJobById(id);
    }

    public IReadOnlyList<ScanJob> GetJobsByAssetId(string assetId)
    {
        EnsureAssetExists(assetId);
        return _scanStorage.GetJobsByAssetId(assetId);
    }

    public ScanResultsResponse GetResultsByJobId(string jobId)
    {
        var job = _scanStorage.GetJobById(jobId)
            ?? throw new KeyNotFoundException("Không tìm thấy scan job.");

        var results = _scanStorage.GetResultsByJobId(jobId)
            .Select(ParseResultJson)
            .ToList();

        return new ScanResultsResponse(job.Id, job.ScanType, results);
    }

    public AssetResultsResponse GetResultsByAssetId(string assetId)
    {
        EnsureAssetExists(assetId);
        var jobs = _scanStorage.GetJobsByAssetId(assetId);
        var resultsByJob = _scanStorage.GetResultsByAssetId(assetId)
            .GroupBy(result => result.JobId)
            .ToDictionary(group => group.Key, group => group.Select(ParseResultJson).ToList());

        return new AssetResultsResponse(
            assetId,
            jobs.Select(job => new AssetScanResultsResponse(
                    job.Id,
                    job.ScanType,
                    job.Status,
                    resultsByJob.GetValueOrDefault(job.Id, [])))
                .ToList());
    }

    public AssetResultsResponse GetTypedResultsByAssetId(string assetId, string scanType)
    {
        EnsureAssetExists(assetId);
        var normalizedScanType = scanType.Trim().ToLowerInvariant();
        var jobs = _scanStorage.GetJobsByAssetId(assetId)
            .Where(job => job.ScanType == normalizedScanType || job.ScanType == ScanRules.All)
            .ToList();

        var resultsByJob = _scanStorage.GetResultsByAssetId(assetId)
            .Where(result => result.ScanType == normalizedScanType)
            .GroupBy(result => result.JobId)
            .ToDictionary(group => group.Key, group => group.Select(ParseResultJson).ToList());

        return new AssetResultsResponse(
            assetId,
            jobs.Select(job => new AssetScanResultsResponse(
                    job.Id,
                    normalizedScanType,
                    job.Status,
                    resultsByJob.GetValueOrDefault(job.Id, [])))
                .ToList());
    }

    private async Task ExecuteJobAsync(
        string jobId,
        Asset asset,
        IReadOnlyList<IScanner> scanners,
        CancellationToken cancellationToken)
    {
        var job = _scanStorage.GetJobById(jobId);
        if (job is null)
        {
            return;
        }

        var runningJob = job with
        {
            Status = ScanRules.StatusRunning,
            StartedAt = DateTimeOffset.UtcNow
        };
        _scanStorage.UpdateJob(runningJob);

        var storedResults = new List<StoredScanResult>();
        var errors = new List<string>();

        foreach (var scanner in scanners)
        {
            try
            {
                _logger.LogInformation("Starting {ScanType} scan for asset {AssetId}", scanner.ScanType, asset.Id);
                var scannerResults = await scanner.ScanAsync(asset, cancellationToken);

                foreach (var result in scannerResults)
                {
                    if (result is JsonObject jsonObject)
                    {
                        jsonObject["scan_type"] = scanner.ScanType;
                    }

                    storedResults.Add(new StoredScanResult(
                        Guid.NewGuid().ToString(),
                        jobId,
                        asset.Id,
                        scanner.ScanType,
                        result.ToJsonString(),
                        DateTimeOffset.UtcNow));
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "{ScanType} scan failed for asset {AssetId}", scanner.ScanType, asset.Id);
                errors.Add($"{scanner.ScanType}: {exception.Message}");
            }
        }

        _scanStorage.ReplaceResults(jobId, storedResults);

        var status = errors.Count == 0
            ? ScanRules.StatusCompleted
            : storedResults.Count == 0
                ? ScanRules.StatusFailed
                : ScanRules.StatusPartial;

        var completedJob = runningJob with
        {
            Status = status,
            EndedAt = DateTimeOffset.UtcNow,
            Error = string.Join("; ", errors),
            Results = storedResults.Count
        };

        _scanStorage.UpdateJob(completedJob);
    }

    private IReadOnlyList<IScanner> ResolveScanners(string scanType, Asset asset)
    {
        var candidates = scanType == ScanRules.All
            ? _scanners.Where(scanner =>
                scanner.ScanType is ScanRules.Dns or ScanRules.Whois or ScanRules.Subdomain or ScanRules.CertificateTransparency)
            : _scanners.Where(scanner => scanner.ScanType.Equals(scanType, StringComparison.OrdinalIgnoreCase));

        return candidates
            .Where(scanner => scanner.SupportedAssetTypes.Contains(asset.Type))
            .ToList();
    }

    private void EnsureAssetExists(string assetId)
    {
        if (_assetStorage.GetById(assetId) is null)
        {
            throw new KeyNotFoundException("Không tìm thấy asset.");
        }
    }

    private static JsonNode ParseResultJson(StoredScanResult result)
    {
        return JsonNode.Parse(result.DataJson) ?? new JsonObject();
    }
}
