namespace CmcHomework.Api.Models;

public sealed record Asset(
    string Id,
    string Name,
    string Type,
    string Status,
    DateTimeOffset CreatedAt);
