namespace CmcHomework.Api.Models;

public static class AssetRules
{
    public const string DefaultStatus = "active";

    public static readonly string[] AllowedTypes = ["domain", "ip", "service"];
    public static readonly string[] AllowedStatuses = ["active", "inactive"];

    public static bool IsValidType(string? type)
    {
        return AllowedTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsValidStatus(string? status)
    {
        return AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }

    public static string TypeText => string.Join(", ", AllowedTypes);

    public static string StatusText => string.Join(", ", AllowedStatuses);
}
