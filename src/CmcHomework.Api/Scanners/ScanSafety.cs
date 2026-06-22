using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;

namespace CmcHomework.Api.Scanners;

// Helper kiểm tra an toàn cho active scan.
// Port scan bị giới hạn localhost/private IP để tránh vô tình scan hệ thống bên ngoài.
public static class ScanSafety
{
    public static void EnsurePortScanAllowed(string target)
    {
        if (!IsLocalOrPrivateTarget(target))
        {
            throw new ValidationException("Port scan chỉ được phép chạy trên localhost hoặc private IP: 127.0.0.1, 10.x.x.x, 172.16-31.x.x, 192.168.x.x.");
        }
    }

    public static bool IsLocalOrPrivateTarget(string target)
    {
        if (target.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(target, out var address))
        {
            return false;
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 ||
            (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
            (bytes[0] == 192 && bytes[1] == 168);
    }
}
