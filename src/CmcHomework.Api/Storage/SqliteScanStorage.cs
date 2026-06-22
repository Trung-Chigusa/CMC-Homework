using CmcHomework.Api.Models;
using Microsoft.Data.Sqlite;

namespace CmcHomework.Api.Storage;

// SQLite implementation cho scan jobs/results.
// Mỗi job nằm trong bảng scan_jobs, còn từng result JSON nằm trong bảng scan_results.
public sealed class SqliteScanStorage : IScanStorage
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteScanStorage(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public ScanJob CreateJob(ScanJob job)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO scan_jobs(id, asset_id, scan_type, status, started_at, ended_at, error, results, created_at)
            VALUES ($id, $assetId, $scanType, $status, $startedAt, $endedAt, $error, $results, $createdAt);
            """;

        AddJobParameters(command, job);
        command.ExecuteNonQuery();

        return job;
    }

    public ScanJob? GetJobById(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, asset_id, scan_type, status, started_at, ended_at, error, results, created_at
            FROM scan_jobs
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadJob(reader) : null;
    }

    public IReadOnlyList<ScanJob> GetJobsByAssetId(string assetId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, asset_id, scan_type, status, started_at, ended_at, error, results, created_at
            FROM scan_jobs
            WHERE asset_id = $assetId
            ORDER BY created_at DESC;
            """;
        command.Parameters.AddWithValue("$assetId", assetId);

        using var reader = command.ExecuteReader();
        var jobs = new List<ScanJob>();
        while (reader.Read())
        {
            jobs.Add(ReadJob(reader));
        }

        return jobs;
    }

    public void UpdateJob(ScanJob job)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE scan_jobs
            SET status = $status,
                started_at = $startedAt,
                ended_at = $endedAt,
                error = $error,
                results = $results
            WHERE id = $id;
            """;

        AddJobParameters(command, job);
        command.ExecuteNonQuery();
    }

    public void ReplaceResults(string jobId, IReadOnlyList<StoredScanResult> results)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM scan_results WHERE job_id = $jobId;";
                delete.Parameters.AddWithValue("$jobId", jobId);
                delete.ExecuteNonQuery();
            }

            foreach (var result in results)
            {
                using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText =
                    """
                    INSERT INTO scan_results(id, job_id, asset_id, scan_type, data_json, created_at)
                    VALUES ($id, $jobId, $assetId, $scanType, $dataJson, $createdAt);
                    """;
                AddResultParameters(insert, result);
                insert.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public IReadOnlyList<StoredScanResult> GetResultsByJobId(string jobId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, job_id, asset_id, scan_type, data_json, created_at
            FROM scan_results
            WHERE job_id = $jobId
            ORDER BY created_at;
            """;
        command.Parameters.AddWithValue("$jobId", jobId);

        return ReadResults(command);
    }

    public IReadOnlyList<StoredScanResult> GetResultsByAssetId(string assetId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, job_id, asset_id, scan_type, data_json, created_at
            FROM scan_results
            WHERE asset_id = $assetId
            ORDER BY created_at DESC;
            """;
        command.Parameters.AddWithValue("$assetId", assetId);

        return ReadResults(command);
    }

    private static void AddJobParameters(SqliteCommand command, ScanJob job)
    {
        command.Parameters.AddWithValue("$id", job.Id);
        command.Parameters.AddWithValue("$assetId", job.AssetId);
        command.Parameters.AddWithValue("$scanType", job.ScanType);
        command.Parameters.AddWithValue("$status", job.Status);
        command.Parameters.AddWithValue("$startedAt", job.StartedAt.ToString("O"));
        command.Parameters.AddWithValue("$endedAt", job.EndedAt is null ? DBNull.Value : job.EndedAt.Value.ToString("O"));
        command.Parameters.AddWithValue("$error", job.Error);
        command.Parameters.AddWithValue("$results", job.Results);
        command.Parameters.AddWithValue("$createdAt", job.CreatedAt.ToString("O"));
    }

    private static void AddResultParameters(SqliteCommand command, StoredScanResult result)
    {
        command.Parameters.AddWithValue("$id", result.Id);
        command.Parameters.AddWithValue("$jobId", result.JobId);
        command.Parameters.AddWithValue("$assetId", result.AssetId);
        command.Parameters.AddWithValue("$scanType", result.ScanType);
        command.Parameters.AddWithValue("$dataJson", result.DataJson);
        command.Parameters.AddWithValue("$createdAt", result.CreatedAt.ToString("O"));
    }

    private static ScanJob ReadJob(SqliteDataReader reader)
    {
        DateTimeOffset? endedAt = reader.IsDBNull(5)
            ? null
            : DateTimeOffset.Parse(reader.GetString(5));

        return new ScanJob(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            DateTimeOffset.Parse(reader.GetString(4)),
            endedAt,
            reader.GetString(6),
            reader.GetInt32(7),
            DateTimeOffset.Parse(reader.GetString(8)));
    }

    private static IReadOnlyList<StoredScanResult> ReadResults(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var results = new List<StoredScanResult>();

        while (reader.Read())
        {
            results.Add(new StoredScanResult(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                DateTimeOffset.Parse(reader.GetString(5))));
        }

        return results;
    }
}
