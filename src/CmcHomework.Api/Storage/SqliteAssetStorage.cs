using CmcHomework.Api.Models;
using Microsoft.Data.Sqlite;

namespace CmcHomework.Api.Storage;

// Storage dùng SQLite để dữ liệu không mất khi restart server.
// Đây là phần đáp ứng yêu cầu "Migrate sang Database" của bài Day 3.
public sealed class SqliteAssetStorage : IAssetStorage
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteAssetStorage(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Asset Create(Asset asset)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO assets(id, name, type, status, created_at)
            VALUES ($id, $name, $type, $status, $createdAt);
            """;

        AddAssetParameters(command, asset);
        command.ExecuteNonQuery();

        return asset;
    }

    public IReadOnlyList<Asset> BatchCreate(IReadOnlyList<Asset> assets)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            foreach (var asset in assets)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    """
                    INSERT INTO assets(id, name, type, status, created_at)
                    VALUES ($id, $name, $type, $status, $createdAt);
                    """;

                AddAssetParameters(command, asset);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
            return assets.ToList();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public BatchDeleteAssetsResponse BatchDelete(IReadOnlyList<string> ids)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        var deleted = 0;
        var notFound = 0;

        try
        {
            foreach (var id in ids)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM assets WHERE id = $id;";
                command.Parameters.AddWithValue("$id", id);

                var affected = command.ExecuteNonQuery();
                if (affected > 0)
                {
                    deleted++;
                }
                else
                {
                    notFound++;
                }
            }

            transaction.Commit();
            return new BatchDeleteAssetsResponse(deleted, notFound);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Asset? GetById(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, name, type, status, created_at
            FROM assets
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadAsset(reader) : null;
    }

    public IReadOnlyList<Asset> GetAll()
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, name, type, status, created_at
            FROM assets
            ORDER BY created_at;
            """;

        using var reader = command.ExecuteReader();
        var assets = new List<Asset>();

        while (reader.Read())
        {
            assets.Add(ReadAsset(reader));
        }

        return assets;
    }

    public int Count()
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM assets;";

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void AddAssetParameters(SqliteCommand command, Asset asset)
    {
        command.Parameters.AddWithValue("$id", asset.Id);
        command.Parameters.AddWithValue("$name", asset.Name);
        command.Parameters.AddWithValue("$type", asset.Type);
        command.Parameters.AddWithValue("$status", asset.Status);
        command.Parameters.AddWithValue("$createdAt", asset.CreatedAt.ToString("O"));
    }

    private static Asset ReadAsset(SqliteDataReader reader)
    {
        return new Asset(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            DateTimeOffset.Parse(reader.GetString(4)));
    }
}
