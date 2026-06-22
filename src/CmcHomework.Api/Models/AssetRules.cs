namespace CmcHomework.Api.Models;

// AssetRules gom các quy định dùng chung cho asset.
// Nếu để các rule này rải rác trong service/handler, sau này sửa sẽ dễ sót.
public static class AssetRules
{
    // Nếu client tạo asset nhưng không gửi status, hệ thống tự dùng active.
    public const string DefaultStatus = "active";

    // Các loại asset được phép nhận.
    // Những giá trị khác, ví dụ "website" hoặc "server", sẽ bị trả 400 Bad Request.
    public static readonly string[] AllowedTypes = ["domain", "ip", "service"];

    // Các trạng thái asset được phép nhận.
    public static readonly string[] AllowedStatuses = ["active", "inactive"];

    // Kiểm tra type có nằm trong danh sách cho phép không.
    // StringComparer.OrdinalIgnoreCase giúp "DOMAIN", "Domain", "domain" đều được chấp nhận.
    public static bool IsValidType(string? type)
    {
        return AllowedTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
    }

    // Kiểm tra status có nằm trong danh sách cho phép không.
    public static bool IsValidStatus(string? status)
    {
        return AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }

    // Chuỗi dùng để in vào message lỗi, ví dụ: "domain, ip, service".
    public static string TypeText => string.Join(", ", AllowedTypes);

    // Chuỗi dùng để in vào message lỗi, ví dụ: "active, inactive".
    public static string StatusText => string.Join(", ", AllowedStatuses);
}
