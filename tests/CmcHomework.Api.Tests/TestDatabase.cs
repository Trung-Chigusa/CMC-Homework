using CmcHomework.Api.Storage;
using Microsoft.Data.Sqlite;

namespace CmcHomework.Api.Tests;

public sealed class TestDatabase : IDisposable
{
    private readonly string _databasePath;

    private TestDatabase(string databasePath, SqliteConnectionFactory connectionFactory)
    {
        _databasePath = databasePath;
        AssetStorage = new SqliteAssetStorage(connectionFactory);
        ScanStorage = new SqliteScanStorage(connectionFactory);
    }

    public SqliteAssetStorage AssetStorage { get; }

    public SqliteScanStorage ScanStorage { get; }

    public static TestDatabase Create()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"cmc-homework-tests-{Guid.NewGuid():N}.db");
        var factory = new SqliteConnectionFactory($"Data Source={databasePath};Foreign Keys=True");
        new DatabaseInitializer(factory).Initialize();

        return new TestDatabase(databasePath, factory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                }

                return;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
        }
    }
}
