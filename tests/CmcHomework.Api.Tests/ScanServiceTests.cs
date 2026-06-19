using System.ComponentModel.DataAnnotations;
using CmcHomework.Api.Models;
using CmcHomework.Api.Scanners;
using CmcHomework.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CmcHomework.Api.Tests;

public sealed class ScanServiceTests
{
    [Fact]
    public async Task StartScan_IpScan_CompletesAndStoresResults()
    {
        using var database = TestDatabase.Create();
        var assetService = new AssetService(database.AssetStorage);
        var asset = assetService.Create(new CreateAssetRequest("127.0.0.1", "ip", null));
        var scanService = CreateScanService(database);

        var job = scanService.StartScan(asset.Id, new StartScanRequest("ip"));

        Assert.Equal(ScanRules.StatusPending, job.Status);

        var completed = await WaitForCompletionAsync(scanService, job.Id);
        Assert.Equal(ScanRules.StatusCompleted, completed.Status);
        Assert.True(completed.Results > 0);

        var results = scanService.GetResultsByJobId(job.Id);
        Assert.Equal(job.Id, results.JobId);
        Assert.NotEmpty(results.Results);
    }

    [Fact]
    public void StartScan_PortScanForPublicIp_IsRejected()
    {
        using var database = TestDatabase.Create();
        var assetService = new AssetService(database.AssetStorage);
        var asset = assetService.Create(new CreateAssetRequest("8.8.8.8", "ip", null));
        var scanService = CreateScanService(database);

        Assert.Throws<ValidationException>(() => scanService.StartScan(asset.Id, new StartScanRequest("port")));
    }

    [Fact]
    public async Task DnsScanner_InvalidDomain_ReturnsGracefulResult()
    {
        var scanner = new DnsScanner();
        var asset = new Asset(Guid.NewGuid().ToString(), "not-a-real-domain.invalid", "domain", "active", DateTimeOffset.UtcNow);

        var results = await scanner.ScanAsync(asset, CancellationToken.None);

        Assert.Single(results);
        Assert.Contains("domain", results[0].ToJsonString());
    }

    [Fact]
    public async Task IpScanner_Localhost_ReturnsGeolocationShape()
    {
        var scanner = new IpScanner();
        var asset = new Asset(Guid.NewGuid().ToString(), "127.0.0.1", "ip", "active", DateTimeOffset.UtcNow);

        var results = await scanner.ScanAsync(asset, CancellationToken.None);

        Assert.Single(results);
        Assert.Contains("geolocation", results[0].ToJsonString());
        Assert.Contains("reverse_dns", results[0].ToJsonString());
    }

    private static ScanService CreateScanService(TestDatabase database)
    {
        IScanner[] scanners =
        [
            new DnsScanner(),
            new WhoisScanner(),
            new SubdomainScanner(),
            new CertificateTransparencyScanner(),
            new AsnScanner(),
            new IpScanner(),
            new PortScanner(),
            new SslScanner(),
            new TechScanner()
        ];

        return new ScanService(
            database.AssetStorage,
            database.ScanStorage,
            scanners,
            NullLogger<ScanService>.Instance);
    }

    private static async Task<ScanJob> WaitForCompletionAsync(IScanService service, string jobId)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var job = service.GetJobById(jobId)!;
            if (job.Status is not (ScanRules.StatusPending or ScanRules.StatusRunning))
            {
                return job;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Scan job did not complete during test.");
    }
}
