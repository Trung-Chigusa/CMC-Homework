namespace CmcHomework.Api.Models;

// ScanRules gom các scan_type và status được phép.
// Việc gom rule vào một file giúp handler/service/scanner dùng chung cùng một nguồn sự thật.
public static class ScanRules
{
    public const string StatusPending = "pending";
    public const string StatusRunning = "running";
    public const string StatusCompleted = "completed";
    public const string StatusFailed = "failed";
    public const string StatusPartial = "partial";

    public const string Dns = "dns";
    public const string Whois = "whois";
    public const string Subdomain = "subdomain";
    public const string CertificateTransparency = "cert_trans";
    public const string Asn = "asn";
    public const string All = "all";
    public const string Ip = "ip";
    public const string Port = "port";
    public const string Ssl = "ssl";
    public const string Tech = "tech";

    public static readonly string[] AllowedScanTypes =
    [
        Dns,
        Whois,
        Subdomain,
        CertificateTransparency,
        Asn,
        All,
        Ip,
        Port,
        Ssl,
        Tech
    ];

    public static bool IsValidScanType(string? scanType)
    {
        return AllowedScanTypes.Contains(scanType, StringComparer.OrdinalIgnoreCase);
    }

    public static string ScanTypeText => string.Join(", ", AllowedScanTypes);
}
