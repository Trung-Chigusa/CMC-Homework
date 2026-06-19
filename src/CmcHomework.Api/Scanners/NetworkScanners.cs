using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CmcHomework.Api.Models;

namespace CmcHomework.Api.Scanners;

public sealed class DnsScanner : IScanner
{
    public string ScanType => ScanRules.Dns;

    public IReadOnlySet<string> SupportedAssetTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "domain"
    };

    public async Task<IReadOnlyList<JsonNode>> ScanAsync(Asset asset, CancellationToken cancellationToken)
    {
        var records = new JsonArray();
        var error = "";

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(asset.Name, cancellationToken);
            foreach (var address in addresses)
            {
                records.Add(new JsonObject
                {
                    ["type"] = address.AddressFamily == AddressFamily.InterNetwork ? "A" : "AAAA",
                    ["value"] = address.ToString()
                });
            }
        }
        catch (Exception exception) when (exception is SocketException or OperationCanceledException)
        {
            error = exception.Message;
        }

        return
        [
            new JsonObject
            {
                ["domain"] = asset.Name,
                ["records"] = records,
                ["error"] = error,
                ["created_at"] = DateTimeOffset.UtcNow.ToString("O")
            }
        ];
    }
}

public sealed class WhoisScanner : IScanner
{
    public string ScanType => ScanRules.Whois;

    public IReadOnlySet<string> SupportedAssetTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "domain"
    };

    public Task<IReadOnlyList<JsonNode>> ScanAsync(Asset asset, CancellationToken cancellationToken)
    {
        IReadOnlyList<JsonNode> results =
        [
            new JsonObject
            {
                ["domain"] = asset.Name,
                ["registrar"] = "lookup_not_configured",
                ["status"] = "collected_placeholder",
                ["note"] = "WHOIS server access depends on provider policy; this result keeps the API contract stable.",
                ["created_at"] = DateTimeOffset.UtcNow.ToString("O")
            }
        ];

        return Task.FromResult(results);
    }
}

public sealed class SubdomainScanner : IScanner
{
    private static readonly string[] CommonPrefixes = ["www", "api", "mail", "dev", "staging"];

    public string ScanType => ScanRules.Subdomain;

    public IReadOnlySet<string> SupportedAssetTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "domain"
    };

    public async Task<IReadOnlyList<JsonNode>> ScanAsync(Asset asset, CancellationToken cancellationToken)
    {
        var subdomains = new JsonArray();

        foreach (var prefix in CommonPrefixes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = $"{prefix}.{asset.Name}";

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(candidate, cancellationToken);
                if (addresses.Length > 0)
                {
                    subdomains.Add(new JsonObject
                    {
                        ["name"] = candidate,
                        ["source"] = "dns_probe",
                        ["addresses"] = new JsonArray(addresses.Select(address => JsonValue.Create(address.ToString())).ToArray())
                    });
                }
            }
            catch (SocketException)
            {
                // Không tìm thấy subdomain phổ biến thì bỏ qua, vì đây không phải lỗi của cả scan.
            }
        }

        return
        [
            new JsonObject
            {
                ["domain"] = asset.Name,
                ["subdomains"] = subdomains,
                ["created_at"] = DateTimeOffset.UtcNow.ToString("O")
            }
        ];
    }
}

public sealed class CertificateTransparencyScanner : IScanner
{
    public string ScanType => ScanRules.CertificateTransparency;

    public IReadOnlySet<string> SupportedAssetTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "domain"
    };

    public Task<IReadOnlyList<JsonNode>> ScanAsync(Asset asset, CancellationToken cancellationToken)
    {
        IReadOnlyList<JsonNode> results =
        [
            new JsonObject
            {
                ["domain"] = asset.Name,
                ["source"] = "certificate_transparency",
                ["query"] = $"%.{asset.Name}",
                ["note"] = "Ready for crt.sh/API integration; kept deterministic for local homework runs.",
                ["created_at"] = DateTimeOffset.UtcNow.ToString("O")
            }
        ];

        return Task.FromResult(results);
    }
}

public sealed class AsnScanner : IScanner
{
    public string ScanType => ScanRules.Asn;

    public IReadOnlySet<string> SupportedAssetTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ip"
    };

    public Task<IReadOnlyList<JsonNode>> ScanAsync(Asset asset, CancellationToken cancellationToken)
    {
        var local = ScanSafety.IsLocalOrPrivateTarget(asset.Name);
        IReadOnlyList<JsonNode> results =
        [
            new JsonObject
            {
                ["ip_address"] = asset.Name,
                ["asn"] = new JsonObject
                {
                    ["number"] = local ? 0 : null,
                    ["name"] = local ? "PRIVATE_NETWORK" : "UNKNOWN",
                    ["description"] = local ? "Private or loopback address; public ASN does not apply." : "External ASN lookup is not configured for offline-safe homework runs."
                },
                ["created_at"] = DateTimeOffset.UtcNow.ToString("O")
            }
        ];

        return Task.FromResult(results);
    }
}

public sealed class IpScanner : IScanner
{
    public string ScanType => ScanRules.Ip;

    public IReadOnlySet<string> SupportedAssetTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ip"
    };

    public async Task<IReadOnlyList<JsonNode>> ScanAsync(Asset asset, CancellationToken cancellationToken)
    {
        var reverseDns = "";
        try
        {
            var host = await Dns.GetHostEntryAsync(asset.Name, cancellationToken);
            reverseDns = host.HostName;
        }
        catch (Exception exception) when (exception is SocketException or ArgumentException)
        {
            reverseDns = "";
        }

        var local = ScanSafety.IsLocalOrPrivateTarget(asset.Name);
        return
        [
            new JsonObject
            {
                ["ip_address"] = asset.Name,
                ["geolocation"] = new JsonObject
                {
                    ["country"] = local ? "Local/Private" : "Unknown",
                    ["country_code"] = local ? "LOCAL" : "",
                    ["city"] = local ? "Local Network" : "",
                    ["region"] = "",
                    ["latitude"] = 0,
                    ["longitude"] = 0,
                    ["isp"] = local ? "Private network" : "External lookup not configured",
                    ["org"] = local ? "Private network" : "Unknown"
                },
                ["asn"] = new JsonObject
                {
                    ["number"] = local ? 0 : null,
                    ["name"] = local ? "PRIVATE_NETWORK" : "UNKNOWN",
                    ["description"] = local ? "Private or loopback address." : "External lookup not configured."
                },
                ["reverse_dns"] = reverseDns,
                ["created_at"] = DateTimeOffset.UtcNow.ToString("O")
            }
        ];
    }
}

public sealed class PortScanner : IScanner
{
    private static readonly Dictionary<int, string> KnownServices = new()
    {
        [21] = "ftp",
        [22] = "ssh",
        [25] = "smtp",
        [53] = "dns",
        [80] = "http",
        [110] = "pop3",
        [143] = "imap",
        [443] = "https",
        [3306] = "mysql",
        [5432] = "postgresql",
        [8080] = "http-alt"
    };

    private static readonly int[] PortsToScan =
    [
        21, 22, 25, 53, 80, 110, 143, 443, 3306, 5432, 8080
    ];

    public string ScanType => ScanRules.Port;

    public IReadOnlySet<string> SupportedAssetTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ip"
    };

    public async Task<IReadOnlyList<JsonNode>> ScanAsync(Asset asset, CancellationToken cancellationToken)
    {
        ScanSafety.EnsurePortScanAllowed(asset.Name);

        var stopwatch = Stopwatch.StartNew();
        var openPorts = new JsonArray();

        foreach (var port in PortsToScan)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await IsPortOpenAsync(asset.Name, port, cancellationToken))
            {
                openPorts.Add(new JsonObject
                {
                    ["port"] = port,
                    ["protocol"] = "tcp",
                    ["state"] = "open",
                    ["service"] = KnownServices.GetValueOrDefault(port, "unknown"),
                    ["version"] = ""
                });
            }
        }

        stopwatch.Stop();
        return
        [
            new JsonObject
            {
                ["ip_address"] = asset.Name,
                ["open_ports"] = openPorts,
                ["closed_ports"] = PortsToScan.Length - openPorts.Count,
                ["total_scanned"] = PortsToScan.Length,
                ["scan_duration_ms"] = stopwatch.ElapsedMilliseconds,
                ["created_at"] = DateTimeOffset.UtcNow.ToString("O")
            }
        ];
    }

    private static async Task<bool> IsPortOpenAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(host, port, cancellationToken).AsTask();
        var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(120), cancellationToken);
        var completed = await Task.WhenAny(connectTask, timeoutTask);

        return completed == connectTask && client.Connected;
    }
}

public sealed class SslScanner : IScanner
{
    public string ScanType => ScanRules.Ssl;

    public IReadOnlySet<string> SupportedAssetTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "domain"
    };

    public async Task<IReadOnlyList<JsonNode>> ScanAsync(Asset asset, CancellationToken cancellationToken)
    {
        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(asset.Name, 443, cancellationToken);

            using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            await ssl.AuthenticateAsClientAsync(asset.Name);

            var certificate = new X509Certificate2(ssl.RemoteCertificate!);
            var daysUntilExpiry = (int)Math.Ceiling((certificate.NotAfter.ToUniversalTime() - DateTime.UtcNow).TotalDays);
            var selfSigned = certificate.Subject == certificate.Issuer;

            return
            [
                new JsonObject
                {
                    ["domain"] = asset.Name,
                    ["certificate"] = new JsonObject
                    {
                        ["subject"] = certificate.Subject,
                        ["issuer"] = certificate.Issuer,
                        ["serial_number"] = certificate.SerialNumber,
                        ["valid_from"] = certificate.NotBefore.ToUniversalTime().ToString("O"),
                        ["valid_until"] = certificate.NotAfter.ToUniversalTime().ToString("O"),
                        ["days_until_expiry"] = daysUntilExpiry,
                        ["is_expired"] = daysUntilExpiry < 0,
                        ["is_self_signed"] = selfSigned,
                        ["san"] = new JsonArray()
                    },
                    ["connection"] = new JsonObject
                    {
                        ["tls_version"] = ssl.SslProtocol.ToString(),
                        ["cipher_suite"] = ssl.NegotiatedCipherSuite.ToString(),
                        ["key_exchange"] = "negotiated"
                    },
                    ["grade"] = daysUntilExpiry < 0 || selfSigned ? "C" : "A",
                    ["issues"] = new JsonArray(),
                    ["created_at"] = DateTimeOffset.UtcNow.ToString("O")
                }
            ];
        }
        catch (Exception exception) when (exception is SocketException or AuthenticationException or IOException or OperationCanceledException)
        {
            return
            [
                new JsonObject
                {
                    ["domain"] = asset.Name,
                    ["certificate"] = null,
                    ["connection"] = null,
                    ["grade"] = "F",
                    ["issues"] = new JsonArray($"SSL scan failed: {exception.Message}"),
                    ["created_at"] = DateTimeOffset.UtcNow.ToString("O")
                }
            ];
        }
    }
}

public sealed partial class TechScanner : IScanner
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public string ScanType => ScanRules.Tech;

    public IReadOnlySet<string> SupportedAssetTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "domain",
        "service"
    };

    public async Task<IReadOnlyList<JsonNode>> ScanAsync(Asset asset, CancellationToken cancellationToken)
    {
        var headers = new JsonObject();
        var technologies = new JsonArray();
        var metaTags = new JsonObject();
        var issues = new JsonArray();

        foreach (var url in CandidateUrls(asset.Name))
        {
            try
            {
                using var response = await Client.GetAsync(url, cancellationToken);
                foreach (var header in response.Headers)
                {
                    headers[header.Key.ToLowerInvariant()] = string.Join(", ", header.Value);
                }

                foreach (var header in response.Content.Headers)
                {
                    headers[header.Key.ToLowerInvariant()] = string.Join(", ", header.Value);
                }

                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                DetectTechnologies(headers, html, technologies, metaTags);

                return
                [
                    new JsonObject
                    {
                        ["domain"] = asset.Name,
                        ["technologies"] = technologies,
                        ["headers"] = headers,
                        ["meta_tags"] = metaTags,
                        ["issues"] = issues,
                        ["created_at"] = DateTimeOffset.UtcNow.ToString("O")
                    }
                ];
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                issues.Add($"{url}: {exception.Message}");
            }
        }

        return
        [
            new JsonObject
            {
                ["domain"] = asset.Name,
                ["technologies"] = technologies,
                ["headers"] = headers,
                ["meta_tags"] = metaTags,
                ["issues"] = issues,
                ["created_at"] = DateTimeOffset.UtcNow.ToString("O")
            }
        ];
    }

    private static IEnumerable<string> CandidateUrls(string host)
    {
        if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            yield return host;
            yield break;
        }

        yield return $"https://{host}";
        yield return $"http://{host}";
    }

    private static void DetectTechnologies(JsonObject headers, string html, JsonArray technologies, JsonObject metaTags)
    {
        if (headers.TryGetPropertyValue("server", out var server) && server is not null)
        {
            technologies.Add(new JsonObject
            {
                ["name"] = server.ToString(),
                ["category"] = "Web Server",
                ["version"] = null,
                ["confidence"] = 90
            });
        }

        if (headers.TryGetPropertyValue("x-powered-by", out var poweredBy) && poweredBy is not null)
        {
            technologies.Add(new JsonObject
            {
                ["name"] = poweredBy.ToString(),
                ["category"] = "Backend",
                ["version"] = null,
                ["confidence"] = 85
            });
        }

        if (html.Contains("react", StringComparison.OrdinalIgnoreCase))
        {
            technologies.Add(new JsonObject
            {
                ["name"] = "React",
                ["category"] = "JavaScript Framework",
                ["version"] = null,
                ["confidence"] = 70
            });
        }

        foreach (Match match in MetaRegex().Matches(html))
        {
            var name = match.Groups["name"].Value;
            var content = WebUtility.HtmlDecode(match.Groups["content"].Value);
            if (!string.IsNullOrWhiteSpace(name))
            {
                metaTags[name.ToLowerInvariant()] = content;
            }
        }
    }

    [GeneratedRegex("<meta[^>]+name=[\"'](?<name>[^\"']+)[\"'][^>]+content=[\"'](?<content>[^\"']*)[\"'][^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex MetaRegex();
}
