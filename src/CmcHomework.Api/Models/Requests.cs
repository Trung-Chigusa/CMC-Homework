namespace CmcHomework.Api.Models;

public sealed record CreateAssetRequest(string? Name, string? Type, string? Status);

public sealed record BatchCreateAssetsRequest(IReadOnlyList<CreateAssetRequest>? Assets);
