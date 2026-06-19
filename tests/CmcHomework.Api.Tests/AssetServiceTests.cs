using System.ComponentModel.DataAnnotations;
using CmcHomework.Api.Models;
using CmcHomework.Api.Services;
using CmcHomework.Api.Storage;

namespace CmcHomework.Api.Tests;

public sealed class AssetServiceTests
{
    [Fact]
    public void Create_ValidAsset_PersistsToSqlite()
    {
        using var database = TestDatabase.Create();
        var service = new AssetService(database.AssetStorage);

        var asset = service.Create(new CreateAssetRequest("example.com", "DOMAIN", null));

        Assert.False(string.IsNullOrWhiteSpace(asset.Id));
        Assert.Equal("domain", asset.Type);
        Assert.Equal("active", asset.Status);
        Assert.Equal(1, service.CountAll());
        Assert.NotNull(database.AssetStorage.GetById(asset.Id));
    }

    [Fact]
    public void BatchCreate_InvalidItem_DoesNotPersistAnything()
    {
        using var database = TestDatabase.Create();
        var service = new AssetService(database.AssetStorage);

        Assert.Throws<ValidationException>(() => service.BatchCreate(new BatchCreateAssetsRequest(
        [
            new CreateAssetRequest("valid.com", "domain", null),
            new CreateAssetRequest("bad", "invalid", null)
        ])));

        Assert.Equal(0, service.CountAll());
    }

    [Fact]
    public void AssetRules_AcceptExpectedValues_RejectUnknownValues()
    {
        Assert.True(AssetRules.IsValidType("domain"));
        Assert.True(AssetRules.IsValidType("IP"));
        Assert.True(AssetRules.IsValidStatus("active"));
        Assert.False(AssetRules.IsValidType("server"));
        Assert.False(AssetRules.IsValidStatus("paused"));
    }
}
